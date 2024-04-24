using System;
using System.IO;
using System.Text;
using Autodesk.Revit.DB;
using iv = PCF_Functions.InputVars;
using mySettings = PCF_Exporter.Properties.Settings;

namespace PCF_Output
{
    public class Output
    {
        public void OutputWriter(StringBuilder _collect)
        {
            ////Clear the output file
            //System.IO.File.WriteAllBytes(filename, new byte[0]);
            ;
            // Write to output file

            if (mySettings.Default.radioButton17UTF8_BOM)
            {
                using (StreamWriter w = new StreamWriter(iv.FullFileName, false, Encoding.UTF8))
                {
                    w.Write(_collect);
                    w.Close();
                }
            }
            else
            {
                using (StreamWriter w = new StreamWriter(iv.FullFileName, false, Encoding.Default))
                {
                    w.Write(_collect);
                    w.Close();
                }
            }
        }
    }
}