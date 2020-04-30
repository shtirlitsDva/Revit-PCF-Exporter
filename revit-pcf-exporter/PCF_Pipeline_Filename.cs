using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using MoreLinq;
using Shared;
using Shared.BuildingCoder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using iv = PCF_Functions.InputVars;
using pdef = PCF_Functions.ParameterDefinition;
using plst = PCF_Functions.ParameterList;

namespace PCF_Pipeline
{
    public static class Filename
    {
        public static StringBuilder BuildAndWriteFilename(Document doc)
        {
            StringBuilder sb = new StringBuilder();

            string docName = doc.ProjectInformation.Name;
            string dateAndTime = DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss");
            dateAndTime = dateAndTime.Replace(" ", "_");
            dateAndTime = dateAndTime.Replace(":", "-");

            string scope = string.Empty;

            if (iv.ExportAllOneFile)
            {
                scope = "_All_Lines";
            }
            else if (iv.ExportAllSepFiles || iv.ExportSpecificPipeLine)
            {
                scope = "_" + iv.SysAbbr;
            }
            else if (iv.ExportSelection)
            {
                scope = "_Selection";
            }

            string _outputDir = iv.OutputDirectoryFilePath;

            iv.FullFileName = _outputDir + "\\" + docName + "_" + dateAndTime + scope + ".pcf";
            //string filename = _outputDir+"\\" + docName + ".pcf";

            sb.AppendLine("    ATTRIBUTE59 " + iv.FullFileName);

            return sb;
        }
    }
}