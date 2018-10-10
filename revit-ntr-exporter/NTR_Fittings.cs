using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using NTR_Functions;

using dw = NTR_Functions.DataWriter;
using mu = Shared.MepUtils;
using Shared;

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
                var cons = Shared.MepUtils.GetConnectors(element);

                switch (kind)
                {
                    case "TEE":
                        sbFittings.Append(dw.PointCoords("PH1", cons.Primary));
                        sbFittings.Append(dw.PointCoords("PH2", cons.Secondary));
                        sbFittings.Append(dw.PointCoords("PA1", element));
                        sbFittings.Append(dw.PointCoords("PA2", cons.Tertiary));
                        sbFittings.Append(dw.DnWriter("DNH", cons.Primary));
                        sbFittings.Append(dw.DnWriter("DNA", cons.Tertiary));
                        sbFittings.Append(dw.ReadWritePropertyFromDataTable(fat, conf.Elements, "TYP"));
                        sbFittings.Append(dw.ReadWritePropertyFromDataTable(fat, conf.Elements, "NORM"));
                        break;
                    case "RED":
                        sbFittings.Append(dw.PointCoords("P1", cons.Primary));
                        sbFittings.Append(dw.PointCoords("P2", cons.Secondary));
                        sbFittings.Append(dw.DnWriter("DN1", cons.Primary));
                        sbFittings.Append(dw.DnWriter("DN2", cons.Secondary));
                        string typ = dw.ReadWritePropertyFromDataTable(fat, conf.Elements, "TYP"); //Handle EXcentric reducers
                        if (!string.IsNullOrEmpty(typ)) sbFittings.Append(typ);
                        sbFittings.Append(dw.ReadWritePropertyFromDataTable(fat, conf.Elements, "NORM"));
                        break;
                    case "FLA":
                        sbFittings.Append(dw.PointCoords("P1", cons.Secondary));
                        sbFittings.Append(dw.PointCoords("P2", cons.Primary));
                        sbFittings.Append(dw.DnWriter("DN", cons.Primary));
                        sbFittings.Append(dw.ReadWritePropertyFromDataTable(fat, conf.Elements, "NORM"));
                        //TODO: Implement flange weight GEW (not necessary, ROHR2 reads some default values if absent)
                        break;
                    case "FLABL":
                        sbFittings.Append(dw.PointCoords("PNAME", cons.Primary));
                        sbFittings.Append(dw.DnWriter("DN", cons.Primary));
                        sbFittings.Append(dw.ReadWritePropertyFromDataTable(fat, conf.Elements, "NORM"));
                        //TODO: Implement flange weight GEW
                        break;
                    case "BOG":
                        sbFittings.Append(dw.PointCoords("P1", cons.Primary));
                        sbFittings.Append(dw.PointCoords("P2", cons.Secondary));
                        sbFittings.Append(dw.PointCoords("PT", element));
                        sbFittings.Append(dw.DnWriter("DN", cons.Primary));
                        sbFittings.Append(dw.ReadWritePropertyFromDataTable(fat, conf.Elements, "NORM"));
                        break;
                    case "TEW":
                        //sbFittings.Replace("TEW", "RO"); //Workaround for olets
                        sbFittings.Length = sbFittings.Length - 3; //Workaround for olets, moves the pointer position back to overwrite TEW
                        sbFittings.Append("RO");
                        sbFittings.Append(dw.PointCoords("P1", dw.OletP1Point(cons)));
                        sbFittings.Append(dw.PointCoords("P2", cons.Secondary));
                        sbFittings.Append(dw.DnWriter("DN", cons.Secondary));
                        break;
                }

                sbFittings.Append(dw.ReadWritePropertyFromDataTable(key, conf.Pipelines, "MAT")); //Is not required for FLABL?
                sbFittings.Append(dw.ReadWritePropertyFromDataTable(key, conf.Pipelines, "LAST")); //Is not required for FLABL?
                sbFittings.Append(dw.WriteElementId(element, "REF"));
                sbFittings.Append(" LTG=" + key);
                sbFittings.AppendLine();

                //Detect and write NOZZLES
                switch (kind)
                {
                    case "FLA":
                        string typ = dw.ReadPropertyValueFromDataTable(fat, conf.Elements, "TYP");
                        switch (typ)
                        {
                            case "NOZ":
                                sbFittings.Append("NOZZLE");
                                sbFittings.Append(dw.PointCoords("PNAME", cons.Primary));
                                sbFittings.Append(dw.ReadWritePropertyFromDataTable(fat, conf.Elements, "LQX"));
                                sbFittings.Append(dw.ReadWritePropertyFromDataTable(fat, conf.Elements, "LQY"));
                                sbFittings.Append(dw.ReadWritePropertyFromDataTable(fat, conf.Elements, "LQZ"));
                                sbFittings.Append(dw.ReadWritePropertyFromDataTable(fat, conf.Elements, "LMX"));
                                sbFittings.Append(dw.ReadWritePropertyFromDataTable(fat, conf.Elements, "LMY"));
                                sbFittings.Append(dw.ReadWritePropertyFromDataTable(fat, conf.Elements, "LMZ"));
                                sbFittings.AppendLine();
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }

            }

            return sbFittings;
        }
    }

    class NonBreakInFittings
    {
        public Pipe HeadPipe;
        public List<XYZ> AllCreationPoints = new List<XYZ>();
        public List<Element> CreatedElements = new List<Element>();

        public NonBreakInFittings(Document doc, IGrouping<int, Element> group)
        {
            //Retreive and store the pipe the said elements are connected to:
            ElementId headPipeId = new ElementId(group.Key);
            Element refElement = doc.GetElement(headPipeId);
            HeadPipe = refElement as Pipe;

            //Populate a list with all connector locations and sort from one end to other
            Cons pipeCons = mu.GetConnectors(refElement);
            XYZ referencePoint = pipeCons.Primary.Origin;
            AllCreationPoints.Add(referencePoint);
            AllCreationPoints.Add(pipeCons.Secondary.Origin);
            var allCons = mu.GetALLConnectorsFromElements(refElement);
            var curvePts = allCons.Where(x => x.OfConnectorType(ConnectorType.Curve)).Select(x => x.Origin).ToList();
            AllCreationPoints.AddRange(curvePts);
            AllCreationPoints.OrderBy(x => x.DistanceTo(referencePoint));
        }
    }
}
