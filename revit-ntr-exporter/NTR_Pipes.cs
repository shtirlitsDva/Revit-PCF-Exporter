using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using NTR_Functions;

using dw = NTR_Functions.DataWriter;

namespace NTR_Exporter
{
    public static class NTR_Pipes
    {
        public static StringBuilder Export(string pipeLineGroupingKey, HashSet<Element> elements, ConfigurationData conf, Document doc)
        {
            var pipeList = elements;
            var sbPipes = new StringBuilder();
            var key = pipeLineGroupingKey;

            foreach (Element element in pipeList)
            {
                //Process P1, P2, DN
                Pipe pipe = (Pipe)element;
                //Get connector set for the pipes
                ConnectorSet connectorSet = pipe.ConnectorManager.Connectors;
                //Filter out non-end types of connectors
                IList<Connector> connectorEnd = (from Connector connector in connectorSet
                                                 where connector.ConnectorType.ToString().Equals("End")
                                                 select connector).ToList();

                //Read the family and type of the element
                string fat = element.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString();
                //Read element kind
                string kind = dw.ReadElementTypeFromDataTable(fat, conf.Profiles, "KIND");

                switch (kind)
                {
                    case "PROF":
                        sbPipes.Append("PROF");
                        sbPipes.Append(dw.PointCoords("P1", connectorEnd.First()));
                        sbPipes.Append(dw.PointCoords("P2", connectorEnd.Last()));
                        sbPipes.Append(dw.ReadWritePropertyFromDataTable(fat, conf.Profiles, "MAT"));
                        sbPipes.Append(dw.ReadWritePropertyFromDataTable(fat, conf.Profiles, "TYP"));
                        sbPipes.Append(dw.ReadWritePropertyFromDataTable(fat, conf.Profiles, "ACHSE"));
                        //sbPipes.Append(dw.ReadParameterFromDataTable(fat, conf.Profiles, "RI"));
                        sbPipes.Append(dw.ReadWritePropertyFromDataTable(fat, conf.Profiles, "LAST"));
                        sbPipes.Append(dw.WriteElementId(element, "REF"));
                        sbPipes.AppendLine();
                        break;
                    default:
                        //Process RO
                        sbPipes.Append("RO");
                        sbPipes.Append(dw.PointCoords("P1", connectorEnd.First()));
                        sbPipes.Append(dw.PointCoords("P2", connectorEnd.Last()));
                        sbPipes.Append(dw.DnWriter(element));
                        sbPipes.Append(dw.ReadWritePropertyFromDataTable(key, conf.Pipelines, "MAT"));
                        sbPipes.Append(dw.ReadWritePropertyFromDataTable(key, conf.Pipelines, "LAST"));
                        sbPipes.Append(dw.WriteElementId(element, "REF"));
                        sbPipes.Append(" LTG=" + key);
                        sbPipes.AppendLine();
                        break;
                }
            }

            return sbPipes;

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