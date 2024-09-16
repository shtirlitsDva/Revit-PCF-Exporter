using Autodesk.Revit.DB;

using PCF_Exporter;

using Shared;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using plst = PCF_Functions.Parameters;

namespace PCF_Model
{
    internal static class PcfElementFactory
    {
        public static IPcfElement CreatePhysicalElements(Element e)
        {
            var type = plst.PCF_ELEM_TYPE.GetValue(e);

            if (type.IsNoE())
                throw new ArgumentException($"Element {e.Id} TYPE is null or empty!");

            switch (type)
            {
                case "PIPE":
                    return new PCF_PIPE(e);
                case "ELBOW":
                    return new PCF_ELBOW(e);
                case "TEE":
                    return new PCF_TEE(e);
                case "FILTER":
                case "GASKET":
                case "REDUCER-CONCENTRIC":
                case "COUPLING":
                case "UNION":
                case "PIPE-BLOCK-FIXED":
                    return new PCF_EP1_EP2(e);
                case "REDUCER-ECCENTRIC":
                    return new PCF_REDUCER_ECCENTRIC(e);
                case "FLANGE-BLIND":
                case "CAP":
                    return new PCF_FLANGE_BLIND(e);
                case "FLANGE":
                    return new PCF_FLANGE(e);
                case "TEE-STUB":
                case "OLET":
                    return new PCF_TEE_STUB(e);
                case "VALVE":
                case "INSTRUMENT":
                case "MISC-COMPONENT":
                    return new PCF_EP1_EP2_CPCONS(e);
                case "VALVE-3WAY":
                case "INSTRUMENT-3WAY":
                    return new PCF_EP1_EP2_EP3_CPCONS(e);
                case "VALVE-ANGLE":
                    return new PCF_VALVE_ANGLE(e);
                case "INSTRUMENT-DIAL":
                    return new PCF_INSTRUMENT_DIAL(e);
                case "SUPPORT":
                    return new PCF_SUPPORT(e);
                case "FLOOR-SYMBOL":
                    return new PCF_FLOOR_SYMBOL(e);
                case "TAP":
                    return new PCF_TAP(e);
                default:
                    throw new NotImplementedException($"Element type {type} is not implemented!");
            }
        }
        public static IEnumerable<IPcfElement> CreateDependentVirtualElements(IPcfElement e)
        {
            switch (e)
            {
                case PCF_FLANGE flange:
                    {
                        Parameter par = flange.Element.LookupParameter("Pakning");
                        if (par == null) yield break;
                        if (par.AsInteger() == 1)
                            yield return new PCF_VIRTUAL_NN_GASKET(flange.Element);
                        else yield break;
                    }
                    break;
                default:
                    yield break;
            }
        }

        internal static HashSet<IPcfElement> CreateSpecialVirtualElements(HashSet<IPcfElement> oopElements)
        {
            Document doc = DocumentManager.Instance.Doc;

            var set = new HashSet<IPcfElement>();

            var specials = oopElements.Where(
                x => x.GetParameterValue(plst.PCF_ELEM_SPECIAL).IsNotNoE())
                .SelectMany(x => x.GetParameterValue(plst.PCF_ELEM_SPECIAL)
                    .Split(';')
                    .Select(
                        value => new { Element = x, Value = value.Trim() }
                        ));

            var typegps = specials.GroupBy(x => x.Value);

            foreach (var group in typegps)
            {
                //Assumed:
                //we are always looking for elements adjacent to each other
                //so cluster their connectors by adjacency
                //Now except for start points which are always alone
                
                // Extract elements from the group
                var elementsInGroup = group.Select(x => x.Element).ToList();
                var type = group.Key;

                if (type == "START")
                {
                    foreach (var element in elementsInGroup)
                    {
                        XYZ sp = XYZ.Zero;

                        var cons = element.AllConnectors;

                        foreach (Connector con in cons)
                        {
                            if (!con.IsConnected)
                            {
                                sp = con.Origin;
                                break;
                            }

                            var thisAbbr = con.MEPSystemAbbreviation(doc);

                            var refCon = con.GetRefConnnector(doc.GetElement(element.ElementId));
                            if (refCon == null) continue;

                            var refAbbr = refCon.MEPSystemAbbreviation(doc);

                            if (thisAbbr != refAbbr)
                            {
                                sp = con.Origin;
                                break;
                            }
                        }

                        if (sp == XYZ.Zero) continue;

                        set.Add(new PCF_VIRTUAL_STARTPOINT(doc.GetElement(element.ElementId), sp));
                        break;
                    }

                    //Prevent fall through if we have a start point
                    continue;
                }

                #region FW and SP
                //Fall through to other types of virtual elements
                var clusters = elementsInGroup
                    .SelectMany((e1, index) => elementsInGroup
                        .Skip(index + 1)
                        .SelectMany(e2 => e1.AllConnectors
                            .SelectMany(c1 => e2.AllConnectors
                                .Where(c2 => c1 != c2 && c1.Origin.DistanceTo(c2.Origin) < 1.25.MmToFt())
                                .Select(c2 => (c1, c2)))))
                        .Distinct();

                foreach (var cluster in clusters)
                {
                    switch (type)
                    {
                        case "FW":
                            set.Add(new PCF_VIRTUAL_FIELDWELD(cluster));
                            break;
                        case "SP":
                            set.Add(new PCF_VIRTUAL_ISOSPLITPOINT(cluster));
                            break;
                        default:
                            throw new Exception("CreateSpecialVirtualElements encountered a not-implemented value:\n" +
                                type);
                    }
                } 
                #endregion
            }

            return set;
        }
    }
}
