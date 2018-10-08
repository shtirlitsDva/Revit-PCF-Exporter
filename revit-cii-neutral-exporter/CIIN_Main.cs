using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoreLinq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Shared.BuildingCoder;
using Shared;
using RevitMEPSystemTraversal;
using pd = CIINExporter.ParameterData;
using plst = CIINExporter.ParameterList;
using pdef = CIINExporter.ParameterDefinition;

namespace CIINExporter
{
    public class CIINExport
    {
        internal Result ExecuteMyCommand(UIApplication uiApp, ref string msg)
        {
            Document doc = uiApp.ActiveUIDocument.Document;

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

                //Declare an object to hold filtered elements
                HashSet<Element> filteredElements = new HashSet<Element>();

                // Instance a collecting stringbuilder
                StringBuilder sbCollect = new StringBuilder();
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

                try
                {
                    //DiameterLimit filter applied to ALL elements.
                    filteredElements = (from element in colElements
                                                 where
                                                 //Diameter limit filter
                                                 FilterDiameterLimit.FilterDL(element) &&
                                                 ////Filter out elements with empty PCF_ELEM_TYPE field (remember to !negate)
                                                 //!string.IsNullOrEmpty(element.get_Parameter(new plst().PCF_ELEM_TYPE.Guid).AsString()) &&
                                                 //Filter out EXCLUDED elements -> 0 means no checkmark
                                                 element.get_Parameter(new plst().CII_ELEM_EXCL.Guid).AsInteger() == 0 &&
                                                 //Filter out caps -- they are ignored... for now...
                                                 !MepUtils.IsTheElementACap(element)
                                                 select element).ToHashSet();

                    //Create a grouping of elements based on the Pipeline identifier (System Abbreviation)
                    pipelineGroups = from e in filteredElements
                                     group e by e.LookupParameter(InputVars.PipelineGroupParameterName).AsString();
                }
                catch (Exception ex)
                {
                    throw new Exception("Filtering in Main threw an exception:\n" + ex.Message +
                        "\nTo fix:\n" +
                        "1. See if parameter CII_ELEM_EXCL exists, if not, rerun parameter import.");
                }

                #endregion

                #region Analysis

                 cIIA = new CIIN_Analysis(doc, filteredElements);
                cIIA.AnalyzeSystem();
                cIIA.NumberNodes();
                //cIIA.PlaceTextNotesAtNodes();

                #endregion

                #region Data Processing
                ModelData Data = new ModelData(cIIA.Model);
                Data.ProcessData();

                #endregion

                #region Output
                // Output the processed data

                sbCollect.Append(Data._01_VERSION);
                sbCollect.Append(Data._02_CONTROL);
                sbCollect.Append(Data._03_ELEMENTS);
                sbCollect.Append(Data._04_AUXDATA);
                if (Data._05_NODENAME != null) sbCollect.Append(Data._05_NODENAME);
                sbCollect.Append(Data._06_BEND);
                sbCollect.Append(Data._07_RIGID);
                sbCollect.Append(Data._08_EXPJT);
                sbCollect.Append(Data._09_RESTRANT);
                sbCollect.Append(Data._10_DISPLMNT);
                sbCollect.Append(Data._11_FORCMNT);
                sbCollect.Append(Data._12_UNIFORM);
                sbCollect.Append(Data._13_WIND);
                sbCollect.Append(Data._14_OFFSETS);
                sbCollect.Append(Data._15_ALLOWBLS);
                sbCollect.Append(Data._16_SIFTEES);
                sbCollect.Append(Data._17_REDUCERS);
                sbCollect.Append(Data._18_FLANGES);
                sbCollect.Append(Data._19_EQUIPMNT);
                sbCollect.Append(Data._20_MISCEL_1);
                sbCollect.Append(Data._21_UNITS);
                sbCollect.Append(Data._22_COORDS);

                Output output = new Output();
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