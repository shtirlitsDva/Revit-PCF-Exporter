using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using MoreLinq;
using PCF_Functions;
using PCF_Output;
using Shared;
using Shared.BuildingCoder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using pd = PCF_Functions.ParameterData;
using pdef = PCF_Functions.ParameterDefinition;
using plst = PCF_Functions.ParameterList;

namespace PCF_Exporter
{
    public class PCFExport
    {
        internal Result ExecuteMyCommand(UIApplication uiApp, ref string msg)
        {
            // UIApplication uiApp = commandData.Application;
            //Test comment
            Document doc = uiApp.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            try
            {
                #region Declaration of variables
                // Instance a collector
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                //FilteredElementCollector pipeTypeCollector = new FilteredElementCollector(doc); //Obsolete???

                // Define a Filter instance to filter by System Abbreviation
                ElementParameterFilter sysAbbr = Shared.Filter.ParameterValueGenericFilter(doc, InputVars.SysAbbr, InputVars.SysAbbrParam);

                // Declare pipeline grouping object
                IEnumerable<IGrouping<string, Element>> pipelineGroups;

                //Declare an object to hold collected elements from collector
                HashSet<Element> colElements = new HashSet<Element>();

                // Instance a collecting stringbuilder
                StringBuilder sbCollect = new StringBuilder();
                #endregion

                #region Compose preamble
                //Compose preamble
                Composer composer = new Composer();

                StringBuilder sbPreamble = composer.PreambleComposer();

                //Append preamble
                sbCollect.Append(sbPreamble);
                #endregion

                #region Element collectors
                //If user chooses to export a single pipeline get only elements in that pipeline and create grouping.
                //Grouping is necessary even tho theres only one group to be able to process by the same code as the all pipelines case

                //If user chooses to export all pipelines get all elements and create grouping
                if (InputVars.ExportAllOneFile)
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

                else if (InputVars.ExportAllSepFiles || InputVars.ExportSpecificPipeLine)
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

                else if (InputVars.ExportSelection)
                {
                    ICollection<ElementId> selection = uiApp.ActiveUIDocument.Selection.GetElementIds();
                    colElements = selection.Select(s => doc.GetElement(s)).ToHashSet();
                }


                #region Sub: Filtering
                HashSet<Element> elements;
                try
                {
                    //DiameterLimit filter applied to ALL elements.
                    var filtering = from element in colElements where FilterDiameterLimit.FilterDL(element) select element;

                    //Filter out EXCLUDED elements -> 0 means no checkmark
                    filtering = from element in filtering
                                where element.get_Parameter(new plst().PCF_ELEM_EXCL.Guid).AsInteger() == 0
                                select element;

                    //Remove instrument pipes
                    filtering = filtering.ExceptWhere(x => x.get_Parameter(BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM)
                                                      .AsString() == "INSTR");

                    if (InputVars.PCF_ELEM_SPEC_FILTER.IsNullOrEmpty() == false)
                    {
                        filtering = filtering.ExceptWhere(x => x.get_Parameter(new plst().PCF_ELEM_SPEC.Guid).AsString() == InputVars.PCF_ELEM_SPEC_FILTER);
                    }

                    //If exporting to ISO, remove some not needed elements
                    if (InputVars.ExportToPlant3DIso)
                    {
                        //When exporting to Plant3D ISO creation, remove the group with the Piping System: Analysis Rigids (ARGD)
                        filtering = filtering
                            .Where(x => !(x.get_Parameter(BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM).AsString() == "ARGD"));

                        ////Also remove anchor symbols -> not needed for ISO
                        ////Currently not removed -> used for floor symbols
                        //filtering = filtering.ExceptWhere(x => x
                        //    .get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM)
                        //    .AsValueString() == "Support Symbolic: ANC");
                    }

                    //Create a grouping of elements based on the Pipeline identifier (System Abbreviation)
                    pipelineGroups = from e in filtering
                                     group e by e.get_Parameter(BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM).AsString();

                    elements = filtering.ToHashSet();
                }
                catch (Exception ex)
                {
                    throw new Exception("Filtering in Main threw an exception:\n" + ex.Message +
                        "\nTo fix:\n" +
                        "1. See if parameter PCF_ELEM_EXCL exists, if not, rerun parameter import.");
                }
                #endregion
                #endregion

                #region Initialize Material Data
                //Set the start number to count the COMPID instances and MAT groups.
                int elementIdentificationNumber = 0;
                int materialGroupIdentifier = 0;

                //Make sure that every element has PCF_MAT_DESCR filled out.
                foreach (Element e in elements)
                {
                    if (string.IsNullOrEmpty(e.get_Parameter(new plst().PCF_MAT_DESCR.Guid).AsString()))
                    {
                        uidoc.Selection.SetElementIds(new List<ElementId>(1) { e.Id });
                        BuildingCoderUtilities.ErrorMsg("PCF_MAT_DESCR is empty for element " + e.Id + "! Please, correct this issue before exporting again.");
                        throw new Exception("PCF_MAT_DESCR is empty for element " + e.Id + "! Please, correct this issue before exporting again.");
                    }
                }

                //Initialize material group numbers on the elements
                IEnumerable<IGrouping<string, Element>> materialGroups = from e in elements group e by e.get_Parameter(new plst().PCF_MAT_DESCR.Guid).AsString();

                using (Transaction trans = new Transaction(doc, "Set PCF_ELEM_COMPID and PCF_MAT_ID"))
                {

                    trans.Start();

                    //Access groups
                    foreach (IEnumerable<Element> group in materialGroups)
                    {
                        materialGroupIdentifier++;
                        //Access parameters
                        foreach (Element element in group)
                        {
                            elementIdentificationNumber++;
                            element.LookupParameter("PCF_ELEM_COMPID").Set(elementIdentificationNumber);
                            element.LookupParameter("PCF_MAT_ID").Set(materialGroupIdentifier);
                        }
                    }
                    trans.Commit();
                }

                //If turned on, write wall thickness of all components
                if (InputVars.WriteWallThickness)
                {
                    //Assign correct wall thickness to elements.
                    using (Transaction trans1 = new Transaction(doc))
                    {
                        trans1.Start("Set wall thickness for pipes!");
                        ParameterDataWriter.SetWallThicknessPipes(elements);
                        trans1.Commit();
                    }
                }

                #endregion

                using (TransactionGroup txGp = new TransactionGroup(doc))
                {
                    txGp.Start("Bogus transactionGroup for the break in hangers");
                    #region Pipeline management
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
                        #region BrokenPipes

                        //Here be code to handle break in accessories that act as supports
                        //Find the supports in current acessoryList and add to supportList
                        //Instantiate a brokenPipesGroup class

                        //Collect all Connectors from brokenPipesList and find the longest distance
                        //Create a temporary pipe from the Connectors with longest distance
                        //Copy PCF_ELEM parameter values to the temporary pipe
                        //Add the temporary pipe to the pipeList
                        //Roll back the TransactionGroup after the elements are sent to Export class' Export methods.

                        List<BrokenPipesGroup> bpgList = new List<BrokenPipesGroup>();

                        List<Element> supportsList = accessoryList.Where(x => x.ComponentClass1() == "Pipe Support").ToList();

                        while (supportsList.Count > 0)
                        {
                            //Get an element to start traversing
                            Element seedElement = supportsList.FirstOrDefault();
                            if (seedElement == null)
                                throw new Exception("BrokenPipes: Seed element returned null! supportsList.Count is " + supportsList.Count);

                            //Instantiate the BrokenPipesGroup
                            BrokenPipesGroup bpg = new BrokenPipesGroup(seedElement, gp.Key);

                            //Traverse system
                            bpg.Traverse(doc);

                            //Remove the support Elements from the collection to keep track of the while loop
                            foreach (Element support in bpg.SupportsOnPipe)
                            {
                                supportsList = supportsList.ExceptWhere(x => x.Id.IntegerValue == support.Id.IntegerValue).ToList();
                            }

                            bpgList.Add(bpg);
                        }

                        using (Transaction tx = new Transaction(doc))
                        {
                            tx.Start("Create healed pipes");
                            foreach (BrokenPipesGroup bpg in bpgList)
                            {
                                //Remove the broken pipes from the pipeList
                                //If there's only one broken pipe, then there's no need to do anything
                                //If there's no broken pipes, then there's no need to do anything either
                                if (bpg.BrokenPipes.Count != 0 && bpg.BrokenPipes.Count != 1)
                                {
                                    foreach (Element pipe in bpg.BrokenPipes)
                                    {
                                        pipeList = pipeList.ExceptWhere(x => x.Id.IntegerValue == pipe.Id.IntegerValue).ToHashSet();
                                    }

                                    //Using the new IEqualityComparer for Connectors to get distinct connectors in the collection
                                    var brokenCons = MepUtils.GetALLConnectorsFromElements(bpg.BrokenPipes.ToHashSet(), new ConnectorXyzComparer(2.0.MmToFt()));
                                    //Create distinct pair combinations with distance from all broken connectors
                                    //https://stackoverflow.com/a/47003122/6073998
                                    List<(Connector c1, Connector c2, double dist)> pairs = brokenCons
                                        .SelectMany
                                            (
                                                (fst, i) => brokenCons.Skip(i + 1).Select(snd => (fst, snd, fst.Origin.DistanceTo(snd.Origin)))
                                            )
                                        .ToList();
                                    var longest = pairs.MaxBy(x => x.dist).FirstOrDefault();
                                    Pipe dPipe = (Pipe)longest.c1.Owner;
                                    bpg.HealedPipe = Pipe.Create(doc, dPipe.MEPSystem.GetTypeId(), dPipe.GetTypeId(),
                                        dPipe.ReferenceLevel.Id, longest.c1.Origin, longest.c2.Origin);

                                    Pipe donorPipe = (Pipe)bpg.BrokenPipes.FirstOrDefault();
                                    bpg.HealedPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(donorPipe.Diameter);

                                    //Add the healed pipe to the pipeList for processing
                                    pipeList.Add(bpg.HealedPipe);
                                }
                            }
                            tx.Commit();
                        }

                        //Now the healed pipe must be populated by the parameters from a donorPipe
                        using (Transaction tx = new Transaction(doc))
                        {
                            //Gather all relevant parameter definitions
                            List<pdef> plist = new plst().LPAll.Where(x => x.Domain == "ELEM" && x.Usage == "U").ToList();
                            plist.Add(new plst().PCF_MAT_ID);

                            tx.Start("Populate the HealedPipe parameters!");
                            foreach (BrokenPipesGroup bpg in bpgList)
                            {
                                //Skip iteration if there's only 1 or no broken pipes
                                if (bpg.BrokenPipes.Count == 0 || bpg.BrokenPipes.Count == 1) continue;
                                Element donorPipe = bpg.BrokenPipes.FirstOrDefault();

                                foreach (pdef p in plist)
                                {
                                    Parameter donorParameter = donorPipe.get_Parameter(p.Guid);
                                    if (donorParameter == null) continue;
                                    switch (donorParameter.StorageType)
                                    {
                                        case StorageType.None:
                                            continue;
                                        case StorageType.Integer:
                                            int donorInt = donorParameter.AsInteger();
                                            if (donorInt == 0) continue;
                                            Parameter targetParInt = bpg.HealedPipe.get_Parameter(p.Guid);
                                            targetParInt.Set(donorInt);
                                            break;
                                        case StorageType.Double:
                                            continue;
                                        case StorageType.String:
                                            string donorStr = donorParameter.AsString();
                                            if (donorStr.IsNullOrEmpty()) continue;
                                            Parameter targetParStr = bpg.HealedPipe.get_Parameter(p.Guid);
                                            targetParStr.Set(donorStr);
                                            break;
                                        case StorageType.ElementId:
                                            continue;
                                        default:
                                            continue;
                                    }
                                }
                            }
                            tx.Commit();
                        }

                        #endregion


                        StringBuilder sbPipeline = new PCF_Pipeline.PCF_Pipeline_Export().Export(gp.Key, doc);
                        StringBuilder sbEndsAndConnections = PCF_Pipeline.EndsAndConnections
                            .DetectAndWriteEndsAndConnections(gp.Key, pipeList, fittingList, accessoryList, doc);
                        StringBuilder sbPipes = new PCF_Pipes.PCF_Pipes_Export().Export(gp.Key, pipeList, doc);
                        StringBuilder sbFittings = new PCF_Fittings.PCF_Fittings_Export().Export(gp.Key, fittingList, doc);
                        StringBuilder sbAccessories = new PCF_Accessories.PCF_Accessories_Export().Export(gp.Key, accessoryList, doc);

                        sbCollect.Append(sbPipeline); sbCollect.Append(sbEndsAndConnections);
                        sbCollect.Append(sbPipes); sbCollect.Append(sbFittings); sbCollect.Append(sbAccessories);
                    }
                    #endregion 

                    txGp.RollBack(); //RollBack the temporary created elements
                }

                #region Materials
                StringBuilder sbMaterials = composer.MaterialsSection(materialGroups);
                sbCollect.Append(sbMaterials);
                #endregion

                #region Output
                // Output the processed data
                PCF_Output.Output output = new PCF_Output.Output();
                output.OutputWriter(doc, sbCollect, InputVars.OutputDirectoryFilePath);
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
