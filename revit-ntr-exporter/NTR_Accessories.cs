using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using NTR_Functions;
using dw = NTR_Functions.DataWriter;

namespace NTR_Exporter
{
    class NTR_Accessories
    {
        public static StringBuilder Export(string key, HashSet<Element> elements, ConfigurationData conf, Document doc)
        {
            var sbAccessories = new StringBuilder();

            foreach (Element element in elements)
            {
                //Read the family and type of the element
                string fat = element.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString();
                
                //Read element kind
                string kind = dw.ReadElementTypeFromDataTable(fat, conf.Elements, "KIND");
                if (string.IsNullOrEmpty(kind)) kind = dw.ReadElementTypeFromDataTable(fat, conf.Supports, "KIND");
                if (string.IsNullOrEmpty(kind)) continue;

                //Write element kind
                sbAccessories.Append(kind);

                //Get the connectors
                var cons = NTR_Utils.GetConnectors(element);

                switch (kind)
                {
                    case "ARM":
                        sbAccessories.Append(dw.PointCoords("P1", cons.Primary));
                        sbAccessories.Append(dw.PointCoords("P2", cons.Secondary));
                        sbAccessories.Append(dw.PointCoords("PM", element));
                        sbAccessories.Append(dw.DnWriter("DN1", cons.Primary));
                        sbAccessories.Append(dw.DnWriter("DN2", cons.Secondary));
                        sbAccessories.Append(dw.ReadParameterFromDataTable(fat, conf.Elements, "GEW"));
                        break;
                    // Old SymbolicSupport cases
                    //case "SH":
                    //case "FH":
                    //    sbAccessories.Append(dw.PointCoords("PNAME", element));
                    //    sbAccessories.Append(dw.ReadParameterFromDataTable(fat, conf.Supports, "L"));
                    //    sbAccessories.Append(dw.WriteElementId(element, "REF"));
                    //    sbAccessories.AppendLine();
                    //    continue;
                    case "SH":
                    case "FH":
                        sbAccessories.Append(dw.PointCoords("PNAME", element));
                        if (kind == "FH") sbAccessories.Append(dw.ReadParameterFromDataTable(fat, conf.Supports, "CW"));
                        sbAccessories.Append(dw.HangerLength("L", element));
                        sbAccessories.Append(dw.WriteElementId(element, "REF"));
                        sbAccessories.AppendLine();
                        continue;
                    case "FP":
                    case "AX":
                        sbAccessories.Append(dw.PointCoords("PNAME", element));
                        sbAccessories.Append(dw.WriteElementId(element, "REF"));
                        sbAccessories.AppendLine();
                        continue;
                    case "FL":
                    case "GL":
                        sbAccessories.Append(dw.PointCoords("PNAME", element));
                        sbAccessories.Append(dw.ReadParameterFromDataTable(fat, conf.Supports, "MALL"));
                        sbAccessories.AppendLine();
                        continue;
                    case "RO":
                        //Added for preinsulated district heating pipes in Pipe Accessory category
                        sbAccessories.Append(dw.PointCoords("P1", cons.Primary));
                        sbAccessories.Append(dw.PointCoords("P2", cons.Secondary));
                        sbAccessories.Append(dw.ReadParameterFromDataTable(fat, conf.Elements, "DN"));
                        break;
                }

                sbAccessories.Append(dw.ReadParameterFromDataTable(key, conf.Pipelines, "MAT")); //Is not required for FLABL?
                sbAccessories.Append(dw.ReadParameterFromDataTable(key, conf.Pipelines, "LAST")); //Is not required for FLABL?
                sbAccessories.Append(dw.WriteElementId(element, "REF"));
                sbAccessories.Append(" LTG=" + key);
                sbAccessories.AppendLine();

            }

            return sbAccessories;
        }
    }
}
