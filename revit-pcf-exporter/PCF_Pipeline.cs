using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Shared.BuildingCoder;
using pdef = PCF_Functions.ParameterDefinition;
using plst = PCF_Functions.ParameterList;

namespace PCF_Pipeline
{
    public class PCF_Pipeline_Export
    {
        public StringBuilder Export(string key, Document doc)
        {
            StringBuilder sbPipeline = new StringBuilder();

            try
            {
                //Instantiate collector
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                //Get the elements
                collector.OfClass(typeof (PipingSystemType));
                //Select correct systemType
                PipingSystemType sQuery = (from PipingSystemType st in collector
                    where string.Equals(st.Abbreviation, key)
                    select st).FirstOrDefault();
            
                IEnumerable<pdef> query = from p in new plst().LPAll
                    where string.Equals(p.Domain, "PIPL") && !string.Equals(p.ExportingTo, "CII")
                    select p;

                sbPipeline.Append("PIPELINE-REFERENCE ");
                sbPipeline.Append(key);
                sbPipeline.AppendLine();

                foreach (pdef p in query)
                {
                    if (string.IsNullOrEmpty(sQuery.get_Parameter(p.Guid).AsString())) continue;
                    sbPipeline.Append("    ");
                    sbPipeline.Append(p.Keyword);
                    sbPipeline.Append(" ");
                    sbPipeline.Append(sQuery.get_Parameter(p.Guid).AsString());
                    sbPipeline.AppendLine();
                }
            }
            catch (Exception e)
            {
                BuildingCoderUtilities.ErrorMsg(e.Message);
            }

            return sbPipeline;

            //Debug
            //// Clear the output file
            //System.IO.File.WriteAllBytes(InputVars.OutputDirectoryFilePath + "Pipes.pcf", new byte[0]);

            //// Write to output file
            //using (StreamWriter w = File.AppendText(InputVars.OutputDirectoryFilePath + "Pipes.pcf"))
            //{
            //    w.Write(sbPipes);
            //    w.Close();
            //}
        }
    }
}