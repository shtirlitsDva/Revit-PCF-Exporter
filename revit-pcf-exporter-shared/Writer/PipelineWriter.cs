using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;

using PcfExporter.Configuration;
using PcfExporter.Model;

using Shared;

using pdef = PcfExporter.Model.ParameterDefinition;
using plst = PcfExporter.Model.Parameters;

namespace PcfExporter.Writer
{
    /// <summary>
    /// Writes the PIPELINE-REFERENCE header of one pipeline: the PIPL-domain
    /// parameter attributes and, for Isogen export, the LDT title-block attributes.
    /// </summary>
    public static class PipelineHeaderWriter
    {
        public static StringBuilder Write(string sysAbbr, Document doc, PcfConfiguration cfg)
        {
            var sb = new StringBuilder();

            PipingSystemType pipingSystemType = new FilteredElementCollector(doc)
                .OfClass(typeof(PipingSystemType))
                .Cast<PipingSystemType>()
                .FirstOrDefault(st => string.Equals(st.Abbreviation, sysAbbr));

            if (pipingSystemType == null)
                throw new Exception($"PipingSystemType with abbreviation {sysAbbr} was not found!");

            sb.Append("PIPELINE-REFERENCE ").Append(sysAbbr).AppendLine();

            if (cfg.ExportToIsogen)
                sb.Append(LdtAttributes(pipingSystemType, cfg));

            IEnumerable<pdef> query = plst.LPAll().Where(p =>
                p.Domain == ParameterDomain.PIPL &&
                p.ExportingTo != ExportingTo.CII &&
                p.ExportingTo != ExportingTo.LDT);

            foreach (pdef p in query)
            {
                string value = pipingSystemType.get_Parameter(p.Guid)?.AsString();
                if (string.IsNullOrEmpty(value)) continue;
                sb.Append("    ").Append(p.Keyword).Append(' ').Append(value).AppendLine();
            }

            return sb;
        }

        /// <summary>ATTRIBUTE11..58 read from the LDT workbook row matching project + line.</summary>
        private static StringBuilder LdtAttributes(PipingSystemType pipingSystemType, PcfConfiguration cfg)
        {
            var sb = new StringBuilder();

            if (string.IsNullOrEmpty(cfg.LdtPath) || !File.Exists(cfg.LdtPath)) return sb;

            DataSet dataSet = DataHandler.ReadExcelToDataSet(cfg.LdtPath);
            DataTable data = DataHandler.ReadDataTable(dataSet, "Pipelines");

            string sysAbbr = pipingSystemType.get_Parameter(
                BuiltInParameter.RBS_SYSTEM_ABBREVIATION_PARAM).AsString();
            string projId = cfg.ProjectIdentifier;
            if (projId.IsNoE())
                throw new Exception("PROJECT-IDENTIFIER is empty!");

            DataRow[] matchingRows = data.Select(
                $"PCF_PROJID = '{projId}' AND LINE_NAME = '{sysAbbr}'");

            if (matchingRows.Length == 0)
                throw new Exception(
                    $"For project {projId} no pipeline with abbreviation {sysAbbr} found!\n" +
                    $"Is PROJECT-IDENTIFIER correctly filled out?");

            DataRow row = matchingRows[0];

            foreach (pdef par in plst.LPAll().Where(x => x.ExportingTo == ExportingTo.LDT))
            {
                string value = Convert.ToString(row[par.Name]);
                if (string.IsNullOrEmpty(value)) continue;
                sb.Append("    ").Append(par.Keyword).Append(' ').Append(value).AppendLine();
            }

            return sb;
        }
    }

    /// <summary>Writes the START-CO-ORDS line for a pipeline's start point, if one is defined.</summary>
    public static class StartPointWriter
    {
        public static StringBuilder Write(string sysAbbr, HashSet<IPcfElement> startPoints)
        {
            var sb = new StringBuilder();
            var sps = startPoints.Where(x => x.SystemAbbreviation == sysAbbr).ToList();
            if (sps.Count == 0) return sb;
            if (sps.Count > 1)
                throw new Exception(
                    $"Multiple start points ({sps.Count}) for the same system {sysAbbr}!\n" +
                    $"{string.Join("\n", sps.Select(x => x.ElementId))}");

            var startPoint = (PCF_VIRTUAL_STARTPOINT)sps[0];
            sb.AppendLine($"    START-CO-ORDS {PcfFormat.PointMm(startPoint.Location)}");
            return sb;
        }
    }
}
