using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using Autodesk.Revit.DB;

using PcfExporter.Writer;

using Shared;

using mp = Shared.MepUtils;
using pdef = PcfExporter.Model.ParameterDefinition;
using plst = PcfExporter.Model.Parameters;

namespace PcfExporter.Model
{
    public abstract class PcfPhysicalElement : IPcfElement
    {
        protected readonly ExportSession S;
        protected Document doc => S.Doc;
        protected EndpointWriter EW => S.EW;
        protected Cons Cons;

        public Element Element { get; }
        public ElementId ElementId => Element.Id;
        public HashSet<Connector> AllConnectors => mp.GetALLConnectorsFromElements(Element);
        public string SystemAbbreviation =>
            Element.get_Parameter(BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM).AsValueString();
        public bool ParticipateInMaterialTable => true;

        protected PcfPhysicalElement(Element element, ExportSession session)
        {
            Element = element;
            S = session;
            Cons = new Cons(element);
        }

        public string GetParameterValue(pdef pdef) => pdef.GetValue(Element);
        public void SetParameterValue(pdef pdef, string value) => pdef.SetValue(Element, value);

        #region Writing to string
        public StringBuilder ToPCFString()
        {
            var sb = new StringBuilder();
            try
            {
                sb.AppendLine(plst.PCF_ELEM_TYPE.GetValue(Element));

                var specification = plst.PCF_ELEM_SPEC.GetValue(Element);
                if (specification == "EXISTING-INCLUDE")
                {
                    sb.AppendLine("    STATUS DOTTED-UNDIMENSIONED");
                    sb.AppendLine("    MATERIAL-LIST EXCLUDE");
                }

                // ITEM-CODE entries enable Plant 3D PCF import; only relevant when the
                // file is not destined for Isogen.
                if (!S.Cfg.ExportToIsogen)
                    sb.Append(Plant3DItemCodeWriter.Write(Element, doc));

                sb.Append(WriteSpecificData());

                sb.Append(TagWriter.Line("TAG", new[] { "TAG 1", "TAG 2", "TAG 3" }, Element));

                sb.Append(WritePcfElemParameters());

                if (S.Cfg.ExportToCii && plst.PCF_ELEM_TYPE.GetValue(Element) != PcfElementTypes.Support)
                    sb.Append(CiiWriter.Write(doc, SystemAbbreviation));

                sb.Append(WriteSpindle());

                sb.Append(WriteTaps());

                sb.AppendLine($"    UCI {Element.UniqueId}");
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Error in {GetType().Name} for element {Element.Id}\n" + ex.ToString(), ex);
            }

            return sb;
        }

        protected abstract StringBuilder WriteSpecificData();
        #endregion

        private StringBuilder WritePcfElemParameters()
        {
            var pQuery = plst.LPAll()
                .Where(p => p.Keyword.IsNotNoE() && p.Domain == ParameterDomain.ELEM);

            var sb = new StringBuilder();

            foreach (pdef p in pQuery)
            {
                Parameter parameter = Element.get_Parameter(p.Guid);
                if (parameter == null) continue;
                StorageType storageType = parameter.StorageType;
                //We only use strings to store PCF data
                switch (storageType)
                {
                    case StorageType.String:
                        string value = parameter.AsString();
                        if (value.IsNoE()) continue;
                        sb.AppendLine($"    {p.Keyword} {value}");
                        break;
                    default:
                        throw new Exception($"Unsupported storage type!\n" +
                            $"Element {Element.Id} for parameter {p.Name} returned\n" +
                            $"unsupported storage type: {storageType}");
                }
            }

            return sb;
        }

        private StringBuilder WriteTaps()
        {
            var sb = new StringBuilder();

            if (plst.PCF_ELEM_TAP1.GetValue(Element).IsNotNoE())
                sb.Append(TapsWriter.WriteSpecificTap(Element, "PCF_ELEM_TAP1", S));
            if (plst.PCF_ELEM_TAP2.GetValue(Element).IsNotNoE())
                sb.Append(TapsWriter.WriteSpecificTap(Element, "PCF_ELEM_TAP2", S));
            if (plst.PCF_ELEM_TAP3.GetValue(Element).IsNotNoE())
                sb.Append(TapsWriter.WriteSpecificTap(Element, "PCF_ELEM_TAP3", S));

            //Write the TAP-element taps
            if (plst.PCF_ELEM_TAPS.GetValue(Element).IsNotNoE())
            {
                string raw = plst.PCF_ELEM_TAPS.GetValue(Element);
                foreach (string uci in raw.Split(';').Select(x => x.ToLower()))
                {
                    Element tap = doc.GetElement(uci);
                    if (tap == null) continue;
                    sb.Append(TapsWriter.WriteGenericTap(Element, tap, S));
                }
            }

            return sb;
        }

        private StringBuilder WriteSpindle()
        {
            var sb = new StringBuilder();
            if (S.Spindles.TryGetValue(Element.Id, out FamilyInstance sd))
            {
                XYZ direction = sd.GetTransform().BasisZ;
                sb.AppendLine($"    SPINDLE-DIRECTION {mp.MapToCardinalDirection(direction)}");
            }
            return sb;
        }
    }
}
