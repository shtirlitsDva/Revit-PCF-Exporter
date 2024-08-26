using Autodesk.Revit.DB;

using Shared;
using GroupByCluster;

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
            var set = new HashSet<IPcfElement>();

            var specials = oopElements.Where(
                x => x.GetParameterValue(plst.PCF_ELEM_SPECIAL).IsNotNoE());

            var typegps = specials.GroupBy(x => x.GetParameterValue(plst.PCF_ELEM_SPECIAL));

            foreach (var group in typegps)
            {
                //Assumed:
                //we are always looking for elements adjacent to each other
                //so cluster them now by adjacency

                var clusters = group.GroupByCluster(
                    (x, y) => MinDistBetweenCons(x, y), 1.25.MmToFt());

                foreach (var cluster in clusters)
                {
                    if (cluster.Count() > 2 || cluster.Count() < 2)
                        throw new Exception(
                            "Cluster count is not 2! (CreateSpecialVirtualElements)\n" +
                            string.Join("\n", cluster.Select(x => x.ElementId.ToString())));

                    var first = cluster.First();
                    var second = cluster.Last();

                    var cons1 = first.AllConnectors;
                    var cons2 = second.AllConnectors;

                    (Connector c1, Connector c2) adjacentCons = cons1
                        .SelectMany(c1 => cons2
                            .Select(c2 => (c1, c2)))
                        .OrderBy(x => x.c1.Origin.DistanceTo(x.c2.Origin))
                        .First();

                    var type = group.Key;

                    switch (type)
                    {
                        case "FW":
                            set.Add(new PCF_VIRTUAL_FIELDWELD(adjacentCons));
                            break;
                        case "SP":
                            set.Add(new PCF_VIRTUAL_ISOSPLITPOINT(adjacentCons));
                            break;
                        default:
                            throw new Exception("CreateSpecialVirtualElements encountered a not-implemented value:\n" +
                                type);
                    }
                }
            }

            return set;

            double MinDistBetweenCons(IPcfElement e1, IPcfElement e2)
            {
                var cons1 = e1.AllConnectors;
                var cons2 = e2.AllConnectors;

                return cons1
                    .Select(c1 => cons2.Min(
                        c2 => c1.Origin.DistanceTo(c2.Origin)))
                    .Min();
            }
        }
    }
}
