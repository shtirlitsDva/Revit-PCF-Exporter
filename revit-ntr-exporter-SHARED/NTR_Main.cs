using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
//using MoreLinq;
using NTR_Functions;
using NTR_Output;
using Shared.BuildingCoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iv = NTR_Functions.InputVars;
using Shared;

namespace NTR_Exporter
{
    class NTR_Exporter
    {
        StringBuilder outputBuilder = new StringBuilder();
        readonly ConfigurationData conf = new ConfigurationData();

        public NTR_Exporter()
        {
            //Clear data from previous runs
            //Test comment
            outputBuilder.Clear();

            outputBuilder.Append(conf._01_GEN);
            outputBuilder.Append(conf._02_AUFT);
            outputBuilder.Append(conf._03_TEXT);
            outputBuilder.Append(conf._04_LAST);
            outputBuilder.Append(conf._05_DN);
            outputBuilder.Append(conf._06_ISO);
        }

        public Result ExportNtr(ExternalCommandData cData)
        {
            // UIApplication uiApp = commandData.Application;
            Document doc = cData.Application.ActiveUIDocument.Document;

            try
            {
                #region Declaration of variables
                // Instance a collector
                FilteredElementCollector collector = new FilteredElementCollector(doc);

                // Define a Filter instance to filter by System Abbreviation
                ElementParameterFilter sysAbbr = Shared.Filter.ParameterValueGenericFilter(doc, InputVars.SysAbbr, InputVars.SysAbbrParam);

                // Declare pipeline grouping object
                IEnumerable<IGrouping<string, Element>> pipelineGroups;

                //Declare an object to hold collected elements from collector
                HashSet<Element> colElements = new HashSet<Element>();
                #endregion

                #region Element collectors
                //If user chooses to export a single pipeline get only elements in that pipeline and create grouping.
                //Grouping is necessary even tho theres only one group to be able to process by the same code as the all pipelines case

                //If user chooses to export all pipelines get all elements and create grouping
                if (iv.ExportAllOneFile)
                {
                    //Define a collector (Pipe OR FamInst) AND (Fitting OR Accessory OR Pipe).
                    //This is to eliminate FamilySymbols from collector which would throw an exception later on.
                    collector.WherePasses(new LogicalAndFilter(new List<ElementFilter>
                    {new LogicalOrFilter(new List<ElementFilter>
                        {
                            new ElementCategoryFilter(BuiltInCategory.OST_PipeFitting),
                            new ElementCategoryFilter(BuiltInCategory.OST_PipeAccessory),
                            new ElementClassFilter(typeof (Pipe))
                        }),
                        new LogicalOrFilter(new List<ElementFilter>
                        {
                            new ElementClassFilter(typeof(Pipe)),
                            new ElementClassFilter(typeof(FamilyInstance))
                        })
                    }));

                    colElements = collector.ToElements().ToHashSet();

                }

                else if (iv.ExportAllSepFiles || iv.ExportSpecificPipeLine)
                {
                    //Define a collector with multiple filters to collect PipeFittings OR PipeAccessories OR Pipes + filter by System Abbreviation
                    //System Abbreviation filter also filters FamilySymbols out.
                    collector.WherePasses(
                        new LogicalOrFilter(
                            new List<ElementFilter>
                            {
                                new ElementCategoryFilter(BuiltInCategory.OST_PipeFitting),
                                new ElementCategoryFilter(BuiltInCategory.OST_PipeAccessory),
                                new ElementClassFilter(typeof (Pipe))
                            })).WherePasses(sysAbbr);
                    colElements = collector.ToElements().ToHashSet();
                }

                else if (iv.ExportSelection)
                {
                    //TODO: If selection is exported -- validate selected elements:
                    //Only fittings, accessories and pipes allowed -> everything else must be filtered out
                    ICollection<ElementId> selection = cData.Application.ActiveUIDocument.Selection.GetElementIds();
                    colElements = selection.Select(s => doc.GetElement(s)).ToHashSet();
                }

                //DiameterLimit filter applied to ALL elements.
                //Filter out EXCLuded elements - 0 means no checkmark, GUID is for PCF_ELEM_EXCL
                //Filter by system PCF_PIPL_EXCL: c1c2c9fe-2634-42ba-89d0-5af699f54d4c
                //PROBLEM: If user exports selection which includes element in a PipingSystem which is not allowed
                //PROBLEM: no elements will be exported
                //SOLUTION: Turn off PipingSystemAllowed filter off for ExportSelection case.
                HashSet<Element> elements = (from element in colElements
                                             where
                                             NTR_Filter.FilterDiameterLimit(element) &&
                                             element.get_Parameter(new Guid("CC8EC292-226C-4677-A32D-10B9736BFC1A")).AsInteger() == 0 &&
                                             element.PipingSystemAllowed(doc)
                                             select element).ToHashSet();

                //Add the elements from ARGD (Rigids) piping system back to working set
                //Which were filtered out by DiameterLimit, but still respecting PCF_ELEM_EXCL
                HashSet<Element> argdElemsOutsideDiaLimit =
                    colElements.Where(x => !NTR_Filter.FilterDiameterLimit(x) &&
                                           x.get_Parameter(new Guid("CC8EC292-226C-4677-A32D-10B9736BFC1A")).AsInteger() == 0 &&
                                           x.MEPSystemAbbreviation() == "ARGD").ToHashSet();

                //Combine the newly found ARGD elements back to main collection
                elements.UnionWith(argdElemsOutsideDiaLimit);

                //Create a grouping of elements based on the Pipeline identifier (System Abbreviation)
                pipelineGroups = from e in elements
                                 group e by e.get_Parameter(BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM).AsString();
                #endregion

                #region Configuration validation

                //Validate if configuration has all pipelines defined
                //Else no LAST value will be written!
                //PROBLEM: If user has pipelines which have been marked as not allowed and do not wish to define them
                //PROBLEM: in the configuration file, this validation will throw an error
                //SOLUTION: Exclude the names of not allowed PipingSystems from the list.
                List<string> pipeSysAbbrs = Shared.MepUtils.GetDistinctPhysicalPipingSystemTypeNames(doc).ToList();
                foreach (string sa in pipeSysAbbrs)
                {
                    string returnValue = DataWriter.ReadPropertyFromDataTable(sa, conf.Pipelines, "LAST");
                    if (returnValue.IsNullOrEmpty())
                    {
                        throw new Exception($"Pipeline {sa} is not defined in the configuration!");
                    }
                }


                #endregion

                outputBuilder.AppendLine("C Element definitions");

                #region Pipeline management

                //TransactionGroup to rollback the changes in creating the NonBreakInElements
                using (TransactionGroup txGp = new TransactionGroup(doc))
                {
                    txGp.Start("Olets non breakin elems.");

                    //List to store ALL created NonBreakInElements
                    //List<NonBreakInElement> nbifAllList = new List<NonBreakInElement>();

                    foreach (IGrouping<string, Element> gp in pipelineGroups)
                    {
                        HashSet<Element> pipeList = (from element in gp
                                                     where element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeCurves
                                                     select element).ToHashSet();
                        HashSet<Element> fittingList = (from element in gp
                                                        where element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeFitting
                                                        select element).ToHashSet();
                        HashSet<Element> accessoryList = (from element in gp
                                                          where element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeAccessory
                                                          select element).ToHashSet();

                        #region Olets and other non-break in items
                        //Here be code to handle non-breaking in elements as olets by the following principle:
                        //Find the non-breaking elements (fx. olets) and affected pipes.
                        //Remove affected pipes from pipeList and create in a transaction new, broken up pieces of pipe coinciding
                        //with olet and ends. Then add those pieces to the pipeList, !!!copy parameter values also!!! <- not needed for NTR?.
                        //Process pipeList as usual and then delete those new dummy pipes from model.

                        //TODO: Implement multiple types of non-breaking items per pipe -> only if needed -> cannot think of others than olets

                        //SpudAdjustable -> Olets
                        //Find fittings of this type:
                        IEnumerable<IGrouping<int, Element>> spudAdjQry = 
                            fittingList.Where(x => x.OfPartType(
                                PartType.SpudAdjustable)).GroupBy(x => x.OletRefOwnerIdAsInt());

                        IList<NonBreakInElement> nbifList = new List<NonBreakInElement>();

                        foreach (IGrouping<int, Element> group in spudAdjQry) nbifList.Add(new NonBreakInElement(doc, group));
                        //nbifAllList.AddRange(nbifList);

                        //Remove the HeadPipes from the PipeList
                        List<int> pipesToRemoveIds = nbifList.Select(x => x.HeadPipe.Id.IntegerValue).ToList();
                        pipeList = pipeList.ExceptWhere(x => pipesToRemoveIds.Contains(x.Id.IntegerValue)).ToHashSet();

                        //Transaction to create all part pipes
                        using (Transaction tx1 = new Transaction(doc))
                        {
                            tx1.Start("Create broken pipes.");

                            foreach (var g in nbifList)
                            {
                                for (int i = 0; i < g.AllCreationPoints.Count - 1; i++)
                                {
                                    int j = i + 1;
                                    g.CreatedElements.Add(Pipe.Create(doc, g.HeadPipe.MEPSystem.GetTypeId(), g.HeadPipe.GetTypeId(),
                                        g.HeadPipe.ReferenceLevel.Id, g.AllCreationPoints[i], g.AllCreationPoints[j]));
                                }

                                foreach (Element el in g.CreatedElements)
                                {
                                    el.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(g.HeadPipe.Diameter);
                                }

                                //Add created pipes to pipeList
                                pipeList.UnionWith(g.CreatedElements);
                            }

                            //Additional nodes need to be created for internal supports
                            //Internal supports need a separate master node for each support
                            if (iv.IncludeSteelStructure)
                            {
                                Guid tag4guid = new Guid("f96a5688-8dbe-427d-aa62-f8744a6bc3ee");
                                var SteelSupports = accessoryList.Where(
                                            x => x.get_Parameter(tag4guid).AsString() == "FRAME");

                                //Also modify accessoryList to remove the same said supports
                                accessoryList = accessoryList.ExceptWhere(
                                    x => x.get_Parameter(tag4guid).AsString() == "FRAME").ToHashSet();

                                foreach (FamilyInstance fi in SteelSupports)
                                {
                                    string familyName = fi.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString();
                                    //Currently only the following family is implemented
                                    if (familyName == "VEKS bærering modplade")
                                    {
                                        Element elType = doc.GetElement(fi.GetTypeId());
                                        bool TopBool = elType.LookupParameter("Modpl_Top_Vis").AsInteger() == 1;
                                        bool BottomBool = elType.LookupParameter("Modpl_Bottom_Vis").AsInteger() == 1;
                                        bool LeftBool = elType.LookupParameter("Modpl_Left_Vis").AsInteger() == 1;
                                        bool RightBool = elType.LookupParameter("Modpl_Right_Vis").AsInteger() == 1;

                                        //Count how many extra nodes are needed
                                        int extraNodeCount = 0;
                                        if (TopBool) extraNodeCount++;
                                        if (BottomBool) extraNodeCount++;
                                        if (LeftBool) extraNodeCount++;
                                        if (RightBool) extraNodeCount++;

                                        if (extraNodeCount == 0) continue;
                                        else if (extraNodeCount == 1) continue;

                                        Cons supportCons = new Cons((Element)fi);

                                        //Getting the corresponding connectors with AllRefs method cannot be used
                                        //Because if two supports reside on same pipe
                                        //Subsequent iterations will get the original pipe, which will make overlapping segments
                                        //So connectors must be obtained by matching geometry
                                        //And it must be done separately at each iteration!

                                        var allConnectors = MepUtils.GetALLConnectorsFromElements(pipeList)
                                            .Where(c => c.ConnectorType == ConnectorType.End).ToHashSet();

                                        var matchedPipeConnectors = allConnectors
                                            .Where(x => supportCons.Primary.Equalz(x, 1.0.MmToFt()))
                                            .ExceptWhere(x => x.Owner.Id.IntegerValue == fi.Id.IntegerValue);

                                        //Should be a null check here -> to tired to figure it out
                                        Connector FirstSideConStart = matchedPipeConnectors.First();

                                        //Assume that supports will always be placed on pipes
                                        Connector FirstSideConEnd =
                                            (from Connector c in FirstSideConStart.ConnectorManager.Connectors
                                             where c.Id != FirstSideConStart.Id && (int)c.ConnectorType == 1
                                             select c).FirstOrDefault();

                                        Connector SecondSideConStart = matchedPipeConnectors.Last();

                                        //Assume that supports will always be placed on pipes
                                        Connector SecondSideConEnd =
                                            (from Connector c in SecondSideConStart.ConnectorManager.Connectors
                                             where c.Id != SecondSideConStart.Id && (int)c.ConnectorType == 1
                                             select c).FirstOrDefault();

                                        //Create help lines to help with the geometry analysis
                                        //The point is to get a point along the line at 5 mm distance from start
                                        Line FirstSideLine = Line.CreateBound(FirstSideConStart.Origin, FirstSideConEnd.Origin);
                                        Line SecondSideLine = Line.CreateBound(SecondSideConStart.Origin, SecondSideConEnd.Origin);

                                        List<XYZ> creationPoints = new List<XYZ>(extraNodeCount + 2);

                                        if (extraNodeCount == 2)
                                        {
                                            creationPoints.Add(FirstSideConEnd.Origin);
                                            creationPoints.Add(FirstSideLine.Evaluate(2.5.MmToFt(), false));
                                            creationPoints.Add(SecondSideLine.Evaluate(2.5.MmToFt(), false));
                                            creationPoints.Add(SecondSideConEnd.Origin);
                                        }

                                        else if (extraNodeCount == 3)
                                        {
                                            creationPoints.Add(FirstSideConEnd.Origin);
                                            creationPoints.Add(FirstSideLine.Evaluate(5.0.MmToFt(), false));
                                            creationPoints.Add(FirstSideConStart.Origin);
                                            creationPoints.Add(SecondSideLine.Evaluate(5.0.MmToFt(), false));
                                            creationPoints.Add(SecondSideConEnd.Origin);
                                            //Dbg.PlaceAdaptiveFamilyInstance(doc, "Marker Line: Red", FirstSideConStart.Origin, FirstSidePoint);
                                            //Dbg.PlaceAdaptiveFamilyInstance(doc, "Marker Line: Red", SecondSideConStart.Origin, SecondSidePoint);
                                        }

                                        else if (extraNodeCount == 4)
                                        {
                                            creationPoints.Add(FirstSideConEnd.Origin);
                                            creationPoints.Add(FirstSideLine.Evaluate(7.5.MmToFt(), false));
                                            creationPoints.Add(FirstSideLine.Evaluate(2.5.MmToFt(), false));
                                            creationPoints.Add(SecondSideLine.Evaluate(2.5.MmToFt(), false));
                                            creationPoints.Add(SecondSideLine.Evaluate(7.5.MmToFt(), false));
                                            creationPoints.Add(SecondSideConEnd.Origin);
                                        }

                                        //Remove the original pipes from pipeList
                                        Pipe fPipe = (Pipe)FirstSideConStart.Owner;
                                        Pipe sPipe = (Pipe)SecondSideConStart.Owner;
                                        pipeList = pipeList.ExceptWhere(x => x.Id.IntegerValue == fPipe.Id.IntegerValue)
                                                           .ExceptWhere(x => x.Id.IntegerValue == sPipe.Id.IntegerValue)
                                                           .ToHashSet();

                                        //Create extra pipes
                                        HashSet<Element> createdPipes = new HashSet<Element>();
                                        for (int i = 0; i < creationPoints.Count - 1; i++)
                                        {
                                            int j = i + 1;
                                            createdPipes.Add(Pipe.Create(doc, fPipe.MEPSystem.GetTypeId(), fPipe.GetTypeId(),
                                                fPipe.ReferenceLevel.Id, creationPoints[i], creationPoints[j]));
                                        }

                                        foreach (Element el in createdPipes)
                                        {
                                            el.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(fPipe.Diameter);
                                        }

                                        //Add created pipes to pipeList
                                        pipeList.UnionWith(createdPipes);
                                    }
                                    else { }//Implement other possibilities later

                                    //This, I think, is needed to be able to interact with temporary pipes
                                    doc.Regenerate();
                                }
                            }

                            tx1.Commit();
                        }

                        #endregion

                        //TODO: Change the splitting of elements to follow the worksheets
                        //There's now a mismatch of how many worksheets define the elements (ELEMENTS and SUPPORTS) and
                        //the division into fittings and accessories. Would be more concise to follow the excel configuration
                        //by worksheet

                        StringBuilder sbPipes = NTR_Pipes.Export(gp.Key, pipeList, conf, doc);
                        StringBuilder sbFittings = NTR_Fittings.Export(gp.Key, fittingList, conf, doc);
                        StringBuilder sbAccessories = NTR_Accessories.Export(gp.Key, accessoryList, conf, doc);

                        outputBuilder.Append(sbPipes);
                        outputBuilder.Append(sbFittings);
                        outputBuilder.Append(sbAccessories);
                    }

                    //Include steel structure here
                    //Requires that the Support Pipe Accessories which the structure support are filtered in the above section
                    if (iv.IncludeSteelStructure)
                    {
#if REVIT2024
                        StringBuilder sbSteel = new NTR_Steel(doc).ExportSteel();
                        outputBuilder.Append(sbSteel);

                        StringBuilder sbBoundaryConds = new NTR_Steel(doc).ExportBoundaryConditions();
                        outputBuilder.Append(sbBoundaryConds);
#endif
                    }

                    #region Debug
                    //string ids = string.Empty;
                    //foreach (var g in nbifAllList) foreach (var e in g.CreatedElements) ids += e.Id.ToString() + "\n";
                    //BuildingCoderUtilities.InfoMsg(ids);
                    #endregion

                    txGp.RollBack(); //Rollback the extra elements created
                    //txGp.Commit(); //For debug purposes can be uncommented
                }
                #endregion

                #region Hangers
                //Temporary section to handle GenericModel hangers
                //Works only if all line in one file selected

                //outputBuilder.Append(NTR_GenericModels.ExportHangers(conf, doc));
                #endregion


                #region Output
                // Output the processed data
                NTR_Output.Output output = new NTR_Output.Output();
                output.OutputWriter(doc, outputBuilder, iv.OutputDirectoryFilePath);
                #endregion


            }

            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }

            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return Result.Succeeded;
        }
    }
}


