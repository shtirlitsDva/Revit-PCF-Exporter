﻿using System;
using System.IO;
using System.Text;
using Autodesk.Revit.DB;
using iv = CIINExporter.InputVars;

namespace CIINExporter
{
    public class Output
    {
        public void OutputWriter(Document _document, StringBuilder sb, string _outputDir)
        {
            string docName = _document.ProjectInformation.Name;
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

            //string filename = _outputDir + "\\" + docName + "_" + dateAndTime + scope + ".CII";
            string filename = _outputDir+"\\" + docName + ".CII";

            //Clear the output file
            System.IO.File.WriteAllBytes(filename, new byte[0]);

            // Write to output file
            using (StreamWriter w = File.AppendText(filename))
            {
                w.Write(sb);
                w.Close();
            }
        }
    }
}