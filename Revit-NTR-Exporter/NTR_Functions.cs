using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms.ComponentModel.Com2Interop;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using BuildingCoder;
using MoreLinq;
using PCF_Functions;
using iv = NTR_Functions.InputVars;

namespace NTR_Functions
{
    public static class InputVars
    {
        //Scope control
        public static bool ExportAllOneFile = false;
        public static bool ExportAllSepFiles = false;
        public static bool ExportSpecificPipeLine = false;
        public static bool ExportSelection = false;
        public static double DiameterLimit = 0;

        //File control
        public static string OutputDirectoryFilePath = @"C:\";
        public static string ExcelPath = @"C:\";

        //Current SystemAbbreviation
        public static string SysAbbr = null;
    }

    public class ConfigurationData
    {
        public StringBuilder GEN { get; }
        public StringBuilder AUFT { get; }
        public StringBuilder TEXT { get; }

        public ConfigurationData(ExternalCommandData cData)
        {
            DataSet dataSet = DataHandler.ImportExcelToDataSet(iv.ExcelPath, "NO");

            DataTableCollection dataTableCollection = dataSet.Tables;

            GEN = ReadConfigurationData(dataTableCollection, "GEN", "C General settings");
            AUFT = ReadConfigurationData(dataTableCollection, "AUFT", "C Project description");
            TEXT = ReadConfigurationData(dataTableCollection, "TEXT", "C User text");

            //http://stackoverflow.com/questions/10855/linq-query-on-a-datatable?rq=1
        }

        /// <summary>
        /// Selects a DataTable by name and creates a StringBuilder output to NTR format based on the data in table.
        /// </summary>
        /// <param name="dataTableCollection">A collection of datatables.</param>
        /// <param name="tableName">The name of the DataTable to process.</param>
        /// <returns>StringBuilder containing the output NTR data.</returns>
        private static StringBuilder ReadConfigurationData(DataTableCollection dataTableCollection, string tableName, string description)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(description);

            var table = (from DataTable dtbl in dataTableCollection where dtbl.TableName == tableName select dtbl).FirstOrDefault();
            if (table == null)
            {
                sb.AppendLine("C " + tableName + " does not exist!");
                return sb;
            }

            DataRow headerRow = table.Rows[0];
            DataRow dataRow = table.Rows[1];
            if (headerRow == null || dataRow == null) throw new NullReferenceException(tableName + " does not have two rows, check EXCEL configuration sheet!");

            sb.Append(tableName);

            for (int i = 0; i < table.Columns.Count; i++)
            {
                sb.Append(" ");
                sb.Append(headerRow.Field<string>(i));
                sb.Append("=");
                sb.Append(dataRow.Field<string>(i));
            }

            sb.AppendLine();

            return sb;
        }
    }
}
