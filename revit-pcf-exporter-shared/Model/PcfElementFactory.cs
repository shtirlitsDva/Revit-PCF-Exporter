using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;

using Shared;

using et = PcfExporter.Model.PcfElementTypes;
using plst = PcfExporter.Model.Parameters;

namespace PcfExporter.Model
{
    /// <summary>
    /// Maps PCF_ELEM_TYPE values to element classes and derives virtual elements.
    /// Holds the export session and injects it into every created element.
    /// </summary>
    public sealed class PcfElementFactory
    {
        private readonly ExportSession _s;

        public PcfElementFactory(ExportSession session) => _s = session;

        public IPcfElement CreatePhysicalElement(Element e)
        {
            var type = plst.PCF_ELEM_TYPE.GetValue(e);

            if (type.IsNoE())
                throw new ArgumentException($"Element {e.Id} TYPE is null or empty!");

            switch (type)
            {
                case et.Pipe:
                    return new PCF_PIPE(e, _s);
                case et.Elbow:
                    return new PCF_ELBOW(e, _s);
                case et.Tee:
                    return new PCF_TEE(e, _s);
                case et.Filter:
                case et.Gasket:
                case et.ReducerConcentric:
                case et.Coupling:
                case et.Union:
                case et.PipeBlockFixed:
                    return new PCF_EP1_EP2(e, _s);
                case et.ReducerEccentric:
                    return new PCF_REDUCER_ECCENTRIC(e, _s);
                case et.FlangeBlind:
                case et.Cap:
                    return new PCF_FLANGE_BLIND(e, _s);
                case et.Flange:
                    return new PCF_FLANGE(e, _s);
                case et.TeeStub:
                    return new PCF_TEE_STUB(e, _s);
                case et.Olet:
                    return new PCF_OLET(e, _s);
                case et.Valve:
                case et.Instrument:
                case et.MiscComponent:
                    return new PCF_EP1_EP2_CPCONS(e, _s);
                case et.Valve3Way:
                case et.Instrument3Way:
                    return new PCF_EP1_EP2_EP3_CPCONS(e, _s);
                case et.ValveAngle:
                    return new PCF_VALVE_ANGLE(e, _s);
                case et.InstrumentDial:
                    return new PCF_INSTRUMENT_DIAL(e, _s);
                case et.Support:
                    return new PCF_SUPPORT(e, _s);
                case et.FloorSymbol:
                    return new PCF_FLOOR_SYMBOL(e, _s);
                case et.Tap:
                    return new PCF_TAP(e, _s);
                case et.Bolt:
                    return new PCF_BOLT(e, _s);
                default:
                    throw new NotImplementedException($"Element type {type} is not implemented!");
            }
        }

        public IEnumerable<IPcfElement> CreateDependentVirtualElements(IPcfElement e)
        {
            switch (e)
            {
                case PCF_FLANGE flange:
                    {
                        Parameter par = flange.Element.LookupParameter("Pakning");
                        if (par == null) yield break;
                        if (par.AsInteger() == 1)
                            yield return new PCF_VIRTUAL_NN_GASKET(flange.Element, _s);
                        else yield break;
                    }
                    break;
                default:
                    yield break;
            }
        }

        public HashSet<IPcfElement> CreateSpecialVirtualElements(HashSet<IPcfElement> oopElements)
        {
            Document doc = _s.Doc;

            var set = new HashSet<IPcfElement>();

            var specials = oopElements.Where(
                x => x.GetParameterValue(plst.PCF_ELEM_SPECIAL).IsNotNoE())
                .SelectMany(x => x.GetParameterValue(plst.PCF_ELEM_SPECIAL)
                    .Split(';')
                    .Select(value => new { Element = x, Value = value.Trim() }));

            var typegps = specials.GroupBy(x => x.Value);

            foreach (var group in typegps)
            {
                //Assumed: we are always looking for elements adjacent to each other,
                //so cluster their connectors by adjacency — except for start points,
                //which are always alone.
                var elementsInGroup = group.Select(x => x.Element).ToList();
                var type = group.Key;

                if (type == "START")
                {
                    foreach (var element in elementsInGroup)
                    {
                        XYZ sp = XYZ.Zero;

                        foreach (Connector con in element.AllConnectors)
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

                        set.Add(new PCF_VIRTUAL_STARTPOINT(doc.GetElement(element.ElementId), sp, _s));
                        break;
                    }

                    //Prevent fall through if we have a start point
                    continue;
                }

                #region FW and SP
                //Cluster adjacent connector pairs for the other virtual element types
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
                            set.Add(new PCF_VIRTUAL_FIELDWELD(cluster, _s));
                            break;
                        case "SP":
                            set.Add(new PCF_VIRTUAL_ISOSPLITPOINT(cluster, _s));
                            break;
                        default:
                            throw new Exception(
                                "CreateSpecialVirtualElements encountered a not-implemented value:\n" + type);
                    }
                }
                #endregion
            }

            return set;
        }
    }
}
