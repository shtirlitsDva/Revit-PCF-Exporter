using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using MoreLinq;
using NTR_Functions;
using NTR_Output;
using Shared.BuildingCoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iv = NTR_Functions.InputVars;

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
                    ICollection<ElementId> selection = cData.Application.ActiveUIDocument.Selection.GetElementIds();
                    colElements = selection.Select(s => doc.GetElement(s)).ToHashSet();
                }

                //DiameterLimit filter applied to ALL elements.
                //Filter out EXCLuded elements - 0 means no checkmark, GUID is for PCF_ELEM_EXCL
                HashSet<Element> elements = (from element in colElements
                                             where
                                             NTR_Filter.FilterDiameterLimit(element) &&
                                             element.get_Parameter(new Guid("CC8EC292-226C-4677-A32D-10B9736BFC1A")).AsInteger() == 0
                                             select element).ToHashSet();

                //Create a grouping of elements based on the Pipeline identifier (System Abbreviation)
                pipelineGroups = from e in elements
                                 group e by e.get_Parameter(BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM).AsString();
                #endregion

                outputBuilder.AppendLine("C Element definitions");

                #region Pipeline management

                //TransactionGroup to rollback the changes in creating the NonBreakInElements
                using (TransactionGroup txGp = new TransactionGroup(doc))
                {
                    txGp.Start("Olets non breakin elems.");

                    //List to store ALL created NonBreakInElements
                    List<NonBreakInElement> nbifAllList = new List<NonBreakInElement>();

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
                        IEnumerable<IGrouping<int, Element>> spudAdjQry = fittingList.Where(x => x.OfPartType(PartType.SpudAdjustable)).GroupBy(x => x.OletRefOwnerIdAsInt());

                        IList<NonBreakInElement> nbifList = new List<NonBreakInElement>();

                        foreach (IGrouping<int, Element> group in spudAdjQry) nbifList.Add(new NonBreakInElement(doc, group));
                        nbifAllList.AddRange(nbifList);

                        //Remove the HeadPipes from the PipeList
                        var pipesToRemoveIds = nbifList.Select(x => x.HeadPipe.Id.IntegerValue).ToHashSet();
                        pipeList = pipeList.Where(x => !pipesToRemoveIds.Contains(x.Id.IntegerValue)).ToHashSet();

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
                                        g.HeadPipe.ReferenceLevel.Id, g.AllCreationPoints[i], g.AllCreationPoints[j])); //Cast to Element needed?
                                }
                                //Add created pipes to pipeList
                                pipeList.UnionWith(g.CreatedElements);
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
                    #region Debug
                    //string ids = string.Empty;
                    //foreach (var g in nbifAllList) foreach (var e in g.CreatedElements) ids += e.Id.ToString() + "\n";
                    //BuildingCoderUtilities.InfoMsg(ids);
                    #endregion

                    txGp.RollBack(); //Rollback the extra elements created
                }
                #endregion

                #region Hangers
                //Temporary section to handle GenericModel hangers
                //Works only if all line in one file selected

                //outputBuilder.Append(NTR_GenericModels.ExportHangers(conf, doc));
                #endregion


                #region Output
                // Output the processed data
                Output output = new Output();
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


