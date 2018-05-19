using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using NTR_Functions;
using dw = NTR_Functions.DataWriter;
using PCF_Functions;

namespace NTR_Exporter
{
    class NTR_GenericModels
    {
        public static StringBuilder ExportHangers(ConfigurationData conf, Document doc)
        {
            StringBuilder sb = new StringBuilder();

            //Collect the hangers
            HashSet<FamilyInstance> stiffHangers = Filter.GetElements<FamilyInstance>(doc, "Rørophæng_stift", BuiltInParameter.ELEM_FAMILY_PARAM);
            HashSet<FamilyInstance> springHangers = Filter.GetElements<FamilyInstance>(doc, "Rørophæng_fjeder", BuiltInParameter.ELEM_FAMILY_PARAM);
            HashSet<Element> allHangers = stiffHangers.Union(springHangers).Cast<Element>().ToHashSet();

            foreach (Element element in allHangers)
            {
                //Read the family and type of the element
                string famAndType = element.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString();

                //Read element kind
                string kind = dw.ReadElementTypeFromDataTable(famAndType, conf.Supports, "KIND");
                if (string.IsNullOrEmpty(kind)) continue;

                //Write element kind
                sb.Append(kind);

                switch (kind)
                {
                    case "SH":
                    case "FH":
                        sb.Append(dw.PointCoordsHanger("PNAME", element));
                        sb.Append(dw.HangerLength("L", element));
                        sb.Append(dw.WriteElementId(element, "REF"));
                        sb.AppendLine();
                        continue;
                }
            }

            return sb;
        }
    }
}
