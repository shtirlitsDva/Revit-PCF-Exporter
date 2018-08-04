using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using System.Text;
using Autodesk.Revit.DB;
using NTR_Functions;
using dw = NTR_Functions.DataWriter;
using Shared;

namespace NTR_Exporter
{
    class NTR_GenericModels
    {
        public static StringBuilder ExportHangers(ConfigurationData conf, Document doc)
        {
            StringBuilder sb = new StringBuilder();

            //Collect the hangers
            //First are all instances collected of said hanger models
            //It is done in FamilyInstance, because Revit won't let collect all Elements (typeof(Element))
            //Then it is cast back to Elements
            HashSet<FamilyInstance> stiffHangers
                = Filter.GetElements<FamilyInstance, BuiltInParameter>
                (doc, BuiltInParameter.ELEM_FAMILY_PARAM, "Rørophæng_stift");
            HashSet<FamilyInstance> springHangers
                = Filter.GetElements<FamilyInstance, BuiltInParameter>
                (doc, BuiltInParameter.ELEM_FAMILY_PARAM, "Rørophæng_fjeder");

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
                        if (kind == "FH") sb.Append(dw.ReadParameterFromDataTable(famAndType, conf.Supports, "CW"));
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
