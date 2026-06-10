using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;

using Shared;

using plst = PcfExporter.Model.Parameters;

namespace PcfExporter.Model
{
    /// <summary>
    /// DORMANT FEATURE — preserved on purpose, do not delete.
    ///
    /// Purpose: hanger (support) families split pipes in Revit, but hangers do not cut
    /// pipes in reality. This traversal finds the run of pipe pieces broken only by
    /// supports so they can be "healed" into one temporary pipe for export, after which
    /// the temporary pipe is rolled back.
    ///
    /// The healing orchestration (create healed pipe from the two farthest connectors,
    /// copy ELEM parameters from a donor pipe, swap the broken pipes out of the export
    /// set, roll everything back after composing) used to live as a commented region in
    /// PCF_Main and is summarized here:
    ///   1. Collect supports ("Pipe Support" component class) per pipeline; seed a
    ///      BrokenPipesGroup from each unvisited support and Traverse().
    ///   2. For each group with 2+ broken pipes: find the connector pair with the
    ///      longest distance, Pipe.Create between them, copy diameter + ELEM parameters
    ///      (and PCF_MAT_ID) from a donor broken pipe to the healed pipe.
    ///   3. Replace the broken pipes with the healed pipe in the export set.
    ///   4. Compose output, then roll back the TransactionGroup so the healed pipes
    ///      never persist in the model.
    /// </summary>
    public class BrokenPipesGroup
    {
        Element SeedElement;
        public List<Element> BrokenPipes = new List<Element>();
        public Element HealedPipe { get; set; } = null;
        public List<Element> SupportsOnPipe = new List<Element>();
        readonly string CurSysAbr;

        public BrokenPipesGroup(Element seedElement, string sysAbr)
        {
            SeedElement = seedElement;
            SupportsOnPipe.Add(seedElement);
            CurSysAbr = sysAbr;
        }

        //Choose one and traverse in both directions finding other supports on same pipe
        //Continue conditions:
        //  1. Element is Pipe -> add to brokenPipesList, continue
        //      a. AND PipingSystemAbbreviation remains unchanged
        //      b. AND PCF_ELEM_SPEC remains unchanged
        //  2. Element is PipeAccessory and is one of the Support family instances -> add to supports on pipe
        //Break conditions:
        //  1. Element is PipeFitting -> Break
        //  2. Element is PipeAccessory and NOT an instance of a Support family -> Break
        //  3. Element is Pipe AND PipingSystemAbbreviation changes -> Break
        //  4. Element is Pipe AND PCF_ELEM_SPEC changes -> Break
        //  5. Free end -> Break

        public void Traverse(Document doc)
        {
            //Get connectors from the Seed Element
            Cons cons = MepUtils.GetConnectors(SeedElement);
            //Assign the connectors from the support to the two directions
            Connector firstSideCon = cons.Primary; Connector secondSideCon = cons.Secondary;

            #region SpecFinder
            //The spec of the support can be different from the pipe's
            //It is decided that the spec of the pipe is decisive
            //This means spec must be determined before loop starts
            //ATTENTION: If pipes on both sides have different spec -> no traversal needed -> the support is placed at a natural boundary

            Connector refFirstCon = null; Connector refSecondCon = null;

            if (firstSideCon.IsConnected)
            {
                var refFirstCons = MepUtils.GetAllConnectorsFromConnectorSet(firstSideCon.AllRefs);
                refFirstCon = refFirstCons.Where(x => x.Owner.IsType<Pipe>()).FirstOrDefault();
            }
            else refFirstCon = DetectUnconnectedConnector(doc, firstSideCon);

            if (secondSideCon.IsConnected)
            {
                var refSecondCons = MepUtils.GetAllConnectorsFromConnectorSet(secondSideCon.AllRefs);
                refSecondCon = refSecondCons.Where(x => x.Owner.IsType<Pipe>()).FirstOrDefault();
            }
            else refSecondCon = DetectUnconnectedConnector(doc, secondSideCon);

            string firstSpec = ""; string secondSpec = ""; string spec = "";

            if (refFirstCon != null)
            {
                Element el = refFirstCon.Owner;
                firstSpec = el.get_Parameter(plst.PCF_ELEM_SPEC.Guid).AsString();
            }
            if (refSecondCon != null)
            {
                Element el = refSecondCon.Owner;
                secondSpec = el.get_Parameter(plst.PCF_ELEM_SPEC.Guid).AsString();
            }

            if (firstSpec.IsNullOrEmpty() && secondSpec.IsNullOrEmpty()) return; //<- Both empty
            if (!firstSpec.IsNullOrEmpty() && secondSpec.IsNullOrEmpty()) spec = firstSpec; //<- First not empty, but second
            else if (firstSpec.IsNullOrEmpty() && !secondSpec.IsNullOrEmpty()) spec = secondSpec; //<- Second not empty, but first
            else
            {
                if (firstSpec == secondSpec) spec = firstSpec;
                else return; //<- Different specs -> support is at natural boundary
            }
            #endregion

            //Loop controller
            bool Continue = true;
            //Side controller
            bool firstSideDone = false;
            //Loop variables
            Connector start = null;
            //Initialize first loop
            start = firstSideCon;
            //Loop guard
            int i = 0;

            while (Continue)
            {
                //Loop guard, if too many iterations something is wrong
                i++;
                if (i > 10000) throw new Exception("Traverse loop in BrokenPipes has reached 10000 iterations -> something is wrong! \n" +
                                                   "Do you really have 10000 pipe pieces?");

                //Using a seed connector, "start", get the next element
                //If "start" does not yield a connector to continue on -> stop this side
                //Determine if next element is eligible for continue
                //If yes, add element to collections, get next connector
                //If not, continue next side if side not already done

                Connector refCon;

                if (start.IsConnected)
                {
                    var refCons = MepUtils.GetAllConnectorsFromConnectorSet(start.AllRefs);
                    refCon = refCons.Where(x => x.Owner.IsType<Pipe>() || x.Owner.IsType<FamilyInstance>()).FirstOrDefault();
                }
                else refCon = DetectUnconnectedConnector(doc, start);

                //Break condition 5: Free end
                if (refCon == null) //Dead end
                {
                    if (firstSideDone == false)
                    {
                        //Dead end -> first side done -> continue second side
                        firstSideDone = true; start = secondSideCon; continue;
                    }
                    else { Continue = false; break; } //Dead end -> both sides done -> end traversal loop
                }

                Element elementToConsider = refCon.Owner;

                //Determine if the element is a support
                bool isSupport = elementToConsider.ComponentClass1(doc) == "Pipe Support";

                //Continuation 1a
                string elementSysAbr = elementToConsider.get_Parameter(BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM).AsString();
                if (CurSysAbr != elementSysAbr)
                {
                    if (firstSideDone == false)
                    {
                        firstSideDone = true; start = secondSideCon; continue;
                    }
                    else { Continue = false; break; }
                }

                //Continuation 1b
                string elementSpec = elementToConsider.get_Parameter(plst.PCF_ELEM_SPEC.Guid).AsString();
                if (spec != elementSpec && !isSupport) //The spec can be different for another support on the pipe, so it must accept those
                {
                    if (firstSideDone == false)
                    {
                        firstSideDone = true; start = secondSideCon; continue;
                    }
                    else { Continue = false; break; }
                }

                switch (elementToConsider)
                {
                    //Remove from pipeList, add to brokenPipesList, continue
                    case Pipe pipe:
                        BrokenPipes.Add(elementToConsider);
                        start = (from Connector c in pipe.ConnectorManager.Connectors //Find next seed connector
                                 where c.Id != refCon.Id && (int)c.ConnectorType == 1
                                 select c).FirstOrDefault();
                        break;
                    case FamilyInstance fi:
                        //Break condition 1: Element is a fitting
                        if (elementToConsider.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeFitting)
                        {
                            if (firstSideDone == false)
                            {
                                firstSideDone = true; start = secondSideCon; continue;
                            }
                            else { Continue = false; break; }
                        }
                        //Break condition 2: Element is a PipeAccessory and NOT a support
                        if (elementToConsider.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeAccessory
                            && !isSupport)
                        {
                            if (firstSideDone == false)
                            {
                                firstSideDone = true; start = secondSideCon; continue;
                            }
                            else { Continue = false; break; }
                        }
                        //If execution reaches this part, then the element is a support and is eligible for consideration
                        SupportsOnPipe.Add(elementToConsider);
                        //Find next seed connector
                        Cons supportCons = MepUtils.GetConnectors(elementToConsider);
                        if (refCon.GetMEPConnectorInfo().IsPrimary)
                        {
                            start = supportCons.Secondary;
                        }
                        else start = supportCons.Primary;

                        break;
                    default:
                        break;
                }
            }
        }

        private Connector DetectUnconnectedConnector(Document doc, Connector knownCon)
        {
            var allCons = MepUtils.GetALLConnectorsInDocument(doc);
            return allCons.Where(c => c.Equalz(knownCon, 0.00328) && c.Owner.Id.IntegerValue != knownCon.Owner.Id.IntegerValue).FirstOrDefault();
        }
    }
}
