using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;

using PcfExporter.Configuration;
using PcfExporter.Model;

using pdef = PcfExporter.Model.ParameterDefinition;
using plst = PcfExporter.Model.Parameters;

namespace PcfExporter.Writer
{
    /// <summary>
    /// Composes the non-element sections of a PCF document: preamble (header) and
    /// the MATERIALS section. Pure functions of their inputs.
    /// </summary>
    public static class DocumentComposer
    {
        public static StringBuilder Preamble(PcfConfiguration cfg)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ISOGEN-FILES ISOGEN.FLS");
            sb.AppendLine("UNITS-BORE " + cfg.UnitsBoreKeyword);
            sb.AppendLine("UNITS-CO-ORDS " + cfg.UnitsCoOrdsKeyword);
            sb.AppendLine("UNITS-WEIGHT " + cfg.UnitsWeightKeyword);
            sb.AppendLine("UNITS-BOLT-DIA MM");
            sb.AppendLine("UNITS-BOLT-LENGTH MM");
            sb.AppendLine("UNITS-WEIGHT-LENGTH " + cfg.UnitsWeightLengthKeyword);
            return sb;
        }

        /// <summary>MATERIALS section from groups keyed by material description, in group order.</summary>
        public static StringBuilder MaterialsSection(IEnumerable<IGrouping<string, PcfExporter.Model.IPcfElement>> elementGroups)
        {
            var sb = new StringBuilder();
            int groupNumber = 0;
            sb.Append("MATERIALS");
            foreach (var group in elementGroups)
            {
                groupNumber++;
                sb.AppendLine();
                sb.Append("MATERIAL-IDENTIFIER " + groupNumber);
                sb.AppendLine();
                sb.Append("    DESCRIPTION " + group.Key);
            }
            return sb;
        }
    }

    /// <summary>
    /// Per-element ITEM-CODE / ITEM-DESCRIPTION entries enabling import of the PCF file
    /// into Plant 3D's PLANTPCFTOISO command. Written only when NOT exporting to Isogen.
    /// </summary>
    public static class Plant3DItemCodeWriter
    {
        public static StringBuilder Write(Element element, Document doc)
        {
            var sb = new StringBuilder();

            //GetValue throws with a "run Import PCF Parameters" message when the
            //parameter is missing — a half-written ITEM-CODE would be worse.
            string itemCode = plst.PCF_MAT_ID.GetValue(element);
            string itemDescr = plst.PCF_MAT_DESCR.GetValue(element);
            string key = Shared.MepUtils.GetElementPipingSystemType(element, doc).Abbreviation;

            sb.AppendLine("    ITEM-CODE " + key + "-" + itemCode);
            sb.AppendLine("    ITEM-DESCRIPTION " + itemDescr);
            return sb;
        }
    }

    /// <summary>
    /// CAESAR II component attributes, read from the pipeline's PipingSystemType.
    /// Historical note: written per element (as the old exporter did), not maintained.
    /// </summary>
    public static class CiiWriter
    {
        public static StringBuilder Write(Document doc, string systemAbbreviation)
        {
            var sb = new StringBuilder();

            PipingSystemType systemType = new FilteredElementCollector(doc)
                .OfClass(typeof(PipingSystemType))
                .WherePasses(Shared.Filter.ParameterValueGenericFilter(
                    doc, systemAbbreviation, BuiltInParameter.RBS_SYSTEM_ABBREVIATION_PARAM))
                .Cast<PipingSystemType>()
                .FirstOrDefault();

            if (systemType == null)
                throw new Exception(
                    $"CII export: no PipingSystemType with abbreviation {systemAbbreviation} was found.");

            var query = plst.LPAll().Where(p =>
                p.Domain == ParameterDomain.PIPL &&
                p.ExportingTo == ExportingTo.CII);

            foreach (pdef p in query)
            {
                string value = systemType.get_Parameter(p.Guid)?.AsString();
                if (string.IsNullOrEmpty(value)) continue;
                sb.AppendLine("    " + p.Keyword + " " + value);
            }
            return sb;
        }
    }

    /// <summary>
    /// Builds the output file name and the ATTRIBUTE59 (SOURCE) line referencing it.
    /// Pure: timestamp is an argument so tests can pin it.
    /// </summary>
    public static class FilenameBuilder
    {
        public static (string fullPath, string attribute59Line) Build(
            string documentName, ExportScope scope, string currentSystemAbbreviation,
            string outputDirectory, DateTime timestamp)
        {
            string dateAndTime = timestamp.ToString("yyyy.MM.dd-HH.mm.ss");

            string scopeText;
            switch (scope)
            {
                case ExportScope.AllInOneFile: scopeText = "All_Lines"; break;
                case ExportScope.Selection: scopeText = "Selection"; break;
                default: scopeText = currentSystemAbbreviation; break;
            }

            string fileName = $"{documentName}_{dateAndTime}_{scopeText}.pcf";
            string fullPath = System.IO.Path.Combine(outputDirectory, fileName);
            return (fullPath, $"    ATTRIBUTE59 {fileName}{Environment.NewLine}");
        }
    }
}
