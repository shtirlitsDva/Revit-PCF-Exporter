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

namespace MEPUtils.SupportTools
{
    public class CompareDataWithR2
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public static void Compare(UIApplication uiApp, string pathToType, string pathToLoad)
        {
            Document doc = uiApp.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(@"X:\AutoCAD DRI - Revit\Addins\NLog\NLog.config");

            try
            {
                FilteredElementCollector col = new FilteredElementCollector(doc);
                var eList = col.OfCategory(BuiltInCategory.OST_PipeAccessory)
                    .WhereElementIsNotElementType().ToElements()
                    .Where(x => x.ComponentClass1(doc) == "Pipe Support").ToHashSet();

                DataTable compareSupports = new DataTable("Compare support data");
                #region DataTable definition
                DataColumn column;
                
                column = new DataColumn();
                column.DataType = typeof(string);
                column.ColumnName = "Family and type";
                compareSupports.Columns.Add(column);

                column = new DataColumn();
                column.DataType = typeof(string);
                column.ColumnName = "Tag";
                compareSupports.Columns.Add(column);

                column = new DataColumn();
                column.DataType = typeof(string);
                column.ColumnName = "Existing type";
                compareSupports.Columns.Add(column);

                column = new DataColumn();
                column.DataType = typeof(string);
                column.ColumnName = "R2 type";
                compareSupports.Columns.Add(column);

                column = new DataColumn();
                column.DataType = typeof(string);
                column.ColumnName = "Existing load";
                compareSupports.Columns.Add(column);

                column = new DataColumn();
                column.DataType = typeof(string);
                column.ColumnName = "R2 load";
                compareSupports.Columns.Add(column);

                #endregion

                DataTable typeTable = DataHandler.ReadCsvToDataTable(pathToType, "TypeTable");
                DataTable loadTable = DataHandler.ReadCsvToDataTable(pathToLoad, "LoadTable");

                foreach (Element el in eList)
                {
                    DataRow row = compareSupports.NewRow();

                    string tag = el.MultipleInstanceParameterValuesAsString(new[] { "TAG 1", "TAG 2" }, "_");

                    row[0] = el.FamilyAndTypeName();
                    row[1] = tag;
                    row[2] = el.MultipleTypeParameterValuesAsString(doc, new[] { "SpringHanger_Type" });
                    row[3] = DataHandler.ReadStringParameterFromDataTable(
                                         tag, typeTable, "Type",
                                         typeTable.Columns["Description"].Ordinal);
                    row[4] = el.MultipleInstanceParameterValuesAsString(new[] { "Belastning" });
                    row[5] = DataHandler.ReadStringParameterFromDataTable(
                                         tag, loadTable, "QZ [N]",
                                         loadTable.Columns["Description"].Ordinal);

                    compareSupports.Rows.Add(row);
                }
                compareSupports.AcceptChanges();

                DataGridViewWindow dgvw = new DataGridViewWindow(compareSupports);
                dgvw.ShowDialog();
                dgvw.Close();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
                //return Result.Failed;
            }
        }
    }
}
