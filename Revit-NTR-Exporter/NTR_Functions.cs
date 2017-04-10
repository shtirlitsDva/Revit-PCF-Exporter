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
        public ConfigurationData(ExternalCommandData cData)
        {
            DataSet dataSet = DataHandler.ImportExcelToDataSet(iv.ExcelPath);

            DataTableCollection dataTableCollection = dataSet.Tables;

            var table = dataTableCollection.
        }
    }
}
