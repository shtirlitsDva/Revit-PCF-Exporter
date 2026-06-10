using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;

using PcfExporter.Model;

using Shared;

using plst = PcfExporter.Model.Parameters;

namespace PcfExporter.Writer
{
    /// <summary>
    /// Detects where a pipeline's elements connect to the outside world and writes the
    /// continuation records:
    /// END-CONNECTION-EQUIPMENT (mechanical equipment, with tag reference),
    /// END-CONNECTION-PIPELINE (another pipeline, or EKSISTERENDE for existing plant).
    /// </summary>
    public static class EndsAndConnectionsWriter
    {
        public static StringBuilder Write(
            string key, HashSet<IPcfElement> pipelineElements, ExportSession s)
        {
            Document doc = s.Doc;
            var sb = new StringBuilder();

            //Virtual elements share their ends with the master physical element — skip them.
            foreach (IPcfElement elem in pipelineElements.OfType<PcfPhysicalElement>())
            {
                foreach (Connector con in elem.AllConnectors)
                {
                    if (!con.IsConnected) continue; //Free end -> no record (historical behavior)

                    var allRefs = MepUtils.GetAllConnectorsFromConnectorSet(con.AllRefs);
                    Connector correspondingCon = allRefs
                        .Where(x => x.Domain == Domain.DomainPiping)
                        .FirstOrDefault(x => x.Owner.Id != elem.ElementId);

                    //Also catches empty cons on multi-connector accessories
                    //(e.g. pressure take-outs on filters).
                    if (correspondingCon == null) continue;

                    if (s.Cfg.Scope == Configuration.ExportScope.Selection)
                    {
                        //When a selection is exported, anything outside the selection is a
                        //continuation — even within the same pipeline.
                        bool outsideExportSet = !pipelineElements.Any(
                            x => x.ElementId == correspondingCon.Owner.Id);

                        if (outsideExportSet)
                        {
                            if (IsMechanicalEquipment(correspondingCon))
                                sb.Append(EquipmentConnection(correspondingCon, s));
                            else
                                sb.Append(PipelineConnection(
                                    correspondingCon, correspondingCon.MEPSystemAbbreviation(doc), s));
                        }
                        continue;
                    }

                    if (IsMechanicalEquipment(correspondingCon))
                    {
                        sb.Append(EquipmentConnection(correspondingCon, s));
                        continue;
                    }

                    //Different pipeline -> unconditional continuation
                    if (correspondingCon.MEPSystemAbbreviation(doc) != key)
                    {
                        sb.Append(PipelineConnection(
                            correspondingCon, correspondingCon.MEPSystemAbbreviation(doc), s));
                        continue;
                    }

                    //Same pipeline, but the counterpart is an EXISTING component
                    Parameter specParameter = correspondingCon.Owner.get_Parameter(plst.PCF_ELEM_SPEC.Guid);
                    if (specParameter?.AsString() == "EXISTING")
                        sb.Append(PipelineConnection(correspondingCon, "EKSISTERENDE", s));
                }
            }

            return sb;
        }

        private static bool IsMechanicalEquipment(Connector con) =>
            con.Owner.Category.Id.IntegerValue == (int)BuiltInCategory.OST_MechanicalEquipment;

        private static StringBuilder EquipmentConnection(Connector con, ExportSession s)
        {
            var sb = new StringBuilder();
            sb.AppendLine("END-CONNECTION-EQUIPMENT");
            sb.Append(s.EW.WriteCO(con.Origin));
            sb.Append(TagWriter.Line(
                "CONNECTION-REFERENCE", new[] { "TAG 1", "TAG 2", "TAG 3", "TAG 4" }, con.Owner));
            return sb;
        }

        private static StringBuilder PipelineConnection(Connector con, string reference, ExportSession s)
        {
            var sb = new StringBuilder();
            sb.AppendLine("END-CONNECTION-PIPELINE");
            sb.Append(s.EW.WriteCO(con.Origin));
            sb.AppendLine("    PIPELINE-REFERENCE " + reference);
            return sb;
        }
    }
}
