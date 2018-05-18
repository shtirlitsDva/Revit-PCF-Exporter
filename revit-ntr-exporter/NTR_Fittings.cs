using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using NTR_Functions;
using PCF_Functions;

using dw = NTR_Functions.DataWriter;
using mu = PCF_Functions.MepUtils;

namespace NTR_Exporter
{
    public static class NTR_Fittings
    {
        public static StringBuilder Export(string key, HashSet<Element> elements, ConfigurationData conf, Document doc)
        {
            var sbFittings = new StringBuilder();

            foreach (Element element in elements)
            {
                //Read the family and type of the element
                string fat = element.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString();

                //Read element kind
                string kind = dw.ReadElementTypeFromDataTable(fat, conf.Elements, "KIND");
                if (string.IsNullOrEmpty(kind)) continue;
                
                //Write element kind
                sbFittings.Append(kind);

                //Get the connectors
                var cons = NTR_Utils.GetConnectors(element);

                switch (kind)
                {
                    case "TEE":
                        sbFittings.Append(dw.PointCoords("PH1", cons.Primary));
                        sbFittings.Append(dw.PointCoords("PH2", cons.Secondary));
                        sbFittings.Append(dw.PointCoords("PA1", element));
                        sbFittings.Append(dw.PointCoords("PA2", cons.Tertiary));
                        sbFittings.Append(dw.DnWriter("DNH", cons.Primary));
                        sbFittings.Append(dw.DnWriter("DNA", cons.Tertiary));
                        sbFittings.Append(dw.ReadParameterFromDataTable(fat, conf.Elements, "TYP"));
                        sbFittings.Append(dw.ReadParameterFromDataTable(fat, conf.Elements, "NORM"));
                        break;
                    case "RED":
                        sbFittings.Append(dw.PointCoords("P1", cons.Primary));
                        sbFittings.Append(dw.PointCoords("P2", cons.Secondary));
                        sbFittings.Append(dw.DnWriter("DN1", cons.Primary));
                        sbFittings.Append(dw.DnWriter("DN2", cons.Secondary));
                        sbFittings.Append(dw.ReadParameterFromDataTable(fat, conf.Elements, "NORM"));
                        break;
                    case "FLA":
                        sbFittings.Append(dw.PointCoords("P1", cons.Secondary));
                        sbFittings.Append(dw.PointCoords("P2", cons.Primary));
                        sbFittings.Append(dw.DnWriter("DN", cons.Primary));
                        sbFittings.Append(dw.ReadParameterFromDataTable(fat, conf.Elements, "NORM"));
                        //TODO: Implement flange weight GEW (not necessary, ROHR2 reads some default values if absent)
                        break;
                    case "FLABL":
                        sbFittings.Append(dw.PointCoords("PNAME", cons.Primary));
                        sbFittings.Append(dw.DnWriter("DN", cons.Primary));
                        sbFittings.Append(dw.ReadParameterFromDataTable(fat, conf.Elements, "NORM"));
                        //TODO: Implement flange weight GEW
                        break;
                    case "BOG":
                        sbFittings.Append(dw.PointCoords("P1", cons.Primary));
                        sbFittings.Append(dw.PointCoords("P2", cons.Secondary));
                        sbFittings.Append(dw.PointCoords("PT", element));
                        sbFittings.Append(dw.DnWriter("DN", cons.Primary));
                        sbFittings.Append(dw.ReadParameterFromDataTable(fat, conf.Elements, "NORM"));
                        break;
                }

                sbFittings.Append(dw.ReadParameterFromDataTable(key, conf.Pipelines, "MAT")); //Is not required for FLABL?
                sbFittings.Append(dw.ReadParameterFromDataTable(key, conf.Pipelines, "LAST")); //Is not required for FLABL?
                sbFittings.Append(dw.WriteElementId(element, "REF"));
                sbFittings.Append(" LTG=" + key);
                sbFittings.AppendLine();

            }

            return sbFittings;
        }
    }
}
