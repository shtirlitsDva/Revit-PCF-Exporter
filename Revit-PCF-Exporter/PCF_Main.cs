using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoreLinq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using BuildingCoder;
using PCF_Functions;
using PCF_Output;
using pd = PCF_Functions.ParameterData;
using plst = PCF_Functions.ParameterList;
using pdef = PCF_Functions.ParameterDefinition;

namespace PCF_Exporter
{
    public class PCFExport
    {
        internal Result ExecuteMyCommand(UIApplication uiApp, ref string msg)
        {
            // UIApplication uiApp = commandData.Application;
            Document doc = uiApp.ActiveUIDocument.Document;

            try
            {
                #region Declaration of variables
                // Instance a collector
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                //FilteredElementCollector pipeTypeCollector = new FilteredElementCollector(doc); //Obsolete???

                // Define a Filter instance to filter by System Abbreviation
                ElementParameterFilter sysAbbr = Filter.ParameterValueFilter(InputVars.SysAbbr, InputVars.SysAbbrParam);

                // Declare pipeline grouping object
                IEnumerable<IGrouping<string, Element>> pipelineGroups;

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
                switch (InputVars.ExportAllOneFile)
                {
                    case false:
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
                        break;

                    //If user chooses to export all pipelines get all elements and create grouping
                    case true:
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
                        break;
                }

                //DiameterLimit filter applied to ALL elements.
                HashSet<Element> elements = (from element in collector where new FilterDiameterLimit().FilterDL(element) select element).ToHashSet();

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

                //Create a grouping of elements based on the Pipeline identifier (System Abbreviation)
                pipelineGroups = from e in elements
                                 group e by e.LookupParameter(InputVars.PipelineGroupParameterName).AsString();
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
                        throw new Exception("PCF_MAT_DESCR is empty for element " + e.Id + "! Please, correct this issue before exporting again.");
                    }
                }

                //Initialize material group numbers on the elements
                IEnumerable<IGrouping<string, Element>> materialGroups = from e in elements group e by e.get_Parameter(new plst().PCF_MAT_DESCR.Guid).AsString();

                Transaction trans = new Transaction(doc, "Set PCF_ELEM_COMPID and PCF_MAT_ID");
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

                #endregion

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
                    
                    StringBuilder sbPipeline = new PCF_Pipeline.PCF_Pipeline_Export().Export(gp.Key, doc);
                    StringBuilder sbPipes = new PCF_Pipes.PCF_Pipes_Export().Export(gp.Key, pipeList, doc);
                    StringBuilder sbFittings = new PCF_Fittings.PCF_Fittings_Export().Export(gp.Key, fittingList, doc);
                    StringBuilder sbAccessories = new PCF_Accessories.PCF_Accessories_Export().Export(gp.Key, accessoryList, doc);

                    sbCollect.Append(sbPipeline); sbCollect.Append(sbPipes); sbCollect.Append(sbFittings); sbCollect.Append(sbAccessories);
                }
                #endregion

                #region Materials
                StringBuilder sbMaterials = composer.MaterialsSection(materialGroups);
                sbCollect.Append(sbMaterials);
                #endregion

                #region Output
                // Output the processed data
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
                msg = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
}