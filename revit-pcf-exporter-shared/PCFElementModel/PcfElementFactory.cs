using Autodesk.Revit.DB;

using Shared;

using System;
using System.Collections.Generic;
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
    }
}
