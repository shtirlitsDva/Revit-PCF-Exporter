using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
//using MoreLinq;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure.StructuralSections;
using static Autodesk.Revit.DB.UnitTypeId;
using Microsoft.WindowsAPICodePack.Dialogs;
using Shared;
using NLog;
using fi = Shared.Filter;
using op = Shared.Output;
using tr = Shared.Transformation;
using mp = Shared.MepUtils;
using System.Globalization;

namespace MEPUtils.SupportTools
{
    public class UpdateDataWithR2
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public static void Update(UIApplication uiApp, string pathToType, string pathToLoad)
        {
            Document doc = uiApp.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(@"X:\AutoCAD DRI - Revit\Addins\NLog\NLog.config");

            try
            {
                ElementParameterFilter pipeSupportEpf = Filter.ParameterValueGenericFilter(
                    doc, "Pipe Support", new Guid("a7f72797-135b-4a1c-8969-e2e3fc76ff14"));

                DataTable typeTable = DataHandler.ReadCsvToDataTable(pathToType, "TypeTable");
                DataTable loadTable = DataHandler.ReadCsvToDataTable(pathToLoad, "LoadTable");

                //Update load
                List<string> allLoadTags = loadTable.AsEnumerable().Select(
                                                    x => x["Description"].ToString()).ToList();

                using (Transaction tx = new Transaction(doc))
                {
                    tx.Start("Update Loads");
                    foreach (string tag in allLoadTags)
                    {
                        if (tag.IsNoE()) continue;
                        if (!tag.Contains("_")) continue;
                        string[] tagParts = tag.Split('_');

                        #region Collect elements
                        FilteredElementCollector col = new FilteredElementCollector(doc);
                        col = col.OfCategory(BuiltInCategory.OST_PipeAccessory)
                                 .WhereElementIsNotElementType()
                                 .WherePasses(pipeSupportEpf);

                        //Tag 1
                        ElementParameterFilter epf = fi.ParameterValueGenericFilter(
                            doc, tagParts[0], new Guid("a93679f7-ca9e-4a1e-bb44-0d890a5b4ba1"));
                        col = col.WherePasses(epf);
                        //Tag 2
                        epf = fi.ParameterValueGenericFilter(
                            doc, tagParts[1], new Guid("3b2afba4-447f-422a-8280-fd394718ad4e"));
                        col = col.WherePasses(epf);
                        #endregion

                        var ids = col.ToElementIds();
                        #region QA collector
                        if (ids.Count < 1)
                        {
                            log.Debug($"No element found for tag: {tag}. Skipping iteration.");
                            continue;
                        }
                        if (ids.Count > 1)
                        {
                            log.Debug($"{ids.Count} elements found for tag: {tag}. Skipping iteration.");
                            continue;
                        }
                        #endregion
                        Element el = doc.GetElement(ids.First());
                        //log.Debug(tag + " -> " + el.Id.ToString());

                        Parameter par = el.LookupParameter("Belastning");
                        if (par == null)
                        {
                            log.Debug($"Element id {el.Id} does not have 'Belastning' parameter!");
                            continue;
                        }
                        string rawValue = DataHandler
                            .ReadStringParameterFromDataTable(
                            tag, loadTable, "QZ [N]", loadTable.Columns["Description"].Ordinal);
                        if (rawValue.Contains(",")) rawValue = rawValue.Replace(",", ".");
                        //rawValue is in N
                        double parsedValue = Math.Abs(
                            double.Parse(rawValue, CultureInfo.InvariantCulture)) / 1000;
                        double convertedValue = UnitUtils.ConvertToInternalUnits(
                            parsedValue, UnitTypeId.Kilonewtons);
                        log.Debug($"{tag} -> {rawValue} : {parsedValue} : {convertedValue}");
                        par.Set(convertedValue);
                    }
                    tx.Commit();
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
                throw new Exception(ex.ToString());
                //return Result.Failed;
            }
        }
    }
}
