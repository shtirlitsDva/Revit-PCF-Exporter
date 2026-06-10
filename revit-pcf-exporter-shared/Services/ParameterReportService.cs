using System.Collections.Generic;
using System.Data;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;

using PcfExporter.Configuration;
using PcfExporter.Context;
using PcfExporter.ElementSource;

using pdef = PcfExporter.Model.ParameterDefinition;
using plst = PcfExporter.Model.Parameters;

namespace PcfExporter.Services
{
    /// <summary>
    /// Builds tabular reports for the setup workflows. Pure data — the UI decides
    /// how to present them (grid windows the user copies rows from into Excel;
    /// replaced the COM-automated live Excel, user decision 2026-06-10: the hosts
    /// must build with `dotnet build`, which cannot process COM references).
    /// </summary>
    public interface IParameterReportService
    {
        /// <summary>Current PIPL + ELEM parameter values, one table each ("Pipelines", "Elements").</summary>
        IReadOnlyList<DataTable> CurrentValues(IRevitContext ctx);
        /// <summary>Family+Type names in scope that have no row in the Elements workbook (empty = none).</summary>
        DataTable UndefinedElements(IRevitContext ctx, PcfConfiguration cfg, DataTable elementsTable);
        /// <summary>Pipelines in the model that have no row in the LDT workbook for this project (empty = none).</summary>
        DataTable UndefinedPipelines(IRevitContext ctx, PcfConfiguration cfg, DataTable pipelinesTable);
    }

    public sealed class ParameterReportService : IParameterReportService
    {
        public IReadOnlyList<DataTable> CurrentValues(IRevitContext ctx)
        {
            Document doc = ctx.Doc;

            #region Pipelines table
            var pipelineGroups = new FilteredElementCollector(doc)
                .OfClass(typeof(PipingSystem))
                .OrderBy(e => e.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString())
                .GroupBy(e => e.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString())
                .ToList();

            List<pdef> piplPars = plst.LPAll()
                .Where(p => p.Domain == Model.ParameterDomain.PIPL && p.Usage == Model.ParameterUsage.USER)
                .ToList();

            DataTable pipelines = NewTable("Pipelines", piplPars.Select(p => p.Name));
            foreach (var gp in pipelineGroups)
            {
                //SystemType parameters can only be read from type elements
                var systemType = (PipingSystemType)doc.GetElement(gp.First().GetTypeId());
                AddRow(pipelines, gp.Key,
                    piplPars.Select(p => systemType.get_Parameter(p.Guid)?.AsString()));
            }
            #endregion

            #region Elements table
            var elementGroups = Shared.Filter.GetElementsWithConnectors(doc)
                .OrderBy(e => e.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString())
                .GroupBy(e => e.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString())
                .ToList();

            List<pdef> elemPars = plst.LPAll()
                .Where(p => p.Domain == Model.ParameterDomain.ELEM && p.Usage == Model.ParameterUsage.USER)
                .ToList();

            DataTable elements = NewTable("Elements", elemPars.Select(p => p.Name));
            foreach (var gp in elementGroups)
                AddRow(elements, gp.Key,
                    elemPars.Select(p => gp.First().get_Parameter(p.Guid)?.AsString()));
            #endregion

            return new[] { pipelines, elements };
        }

        public DataTable UndefinedElements(IRevitContext ctx, PcfConfiguration cfg, DataTable elementsTable)
        {
            var source = new RevitElementSource(ctx);
            HashSet<Element> colElements = source.Collect(cfg);

            var inScope = colElements
                .Where(e => ElementFilters.PassesDiameterLimit(e, cfg))
                .GroupBy(e => e.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString())
                .Select(g => g.Key)
                .ToList();

            var defined = new HashSet<string>(
                elementsTable.AsEnumerable().Select(r => r.Field<string>(0)).Where(x => x != null));

            var result = new DataTable("Undefined elements");
            result.Columns.Add("Family and Type", typeof(string));
            foreach (string ft in inScope.Where(ft => !defined.Contains(ft)))
                result.Rows.Add(ft);
            return result;
        }

        public DataTable UndefinedPipelines(IRevitContext ctx, PcfConfiguration cfg, DataTable pipelinesTable)
        {
            Document doc = ctx.Doc;

            var modelAbbreviations = new FilteredElementCollector(doc)
                .OfClass(typeof(PipingSystemType))
                .Cast<PipingSystemType>()
                .Select(pst => pst.Abbreviation)
                .Where(a => !string.IsNullOrEmpty(a))
                .Distinct()
                .ToList();

            //The LDT workbook is keyed by (PCF_PROJID, LINE_NAME)
            var defined = new HashSet<string>(pipelinesTable.AsEnumerable()
                .Where(r => r.Field<string>(0) == cfg.ProjectIdentifier)
                .Select(r => r.Field<string>(1))
                .Where(x => x != null));

            var result = new DataTable("Undefined pipelines");
            result.Columns.Add("System abbreviation", typeof(string));
            foreach (string a in modelAbbreviations.Where(a => !defined.Contains(a)))
                result.Rows.Add(a);
            return result;
        }

        private static DataTable NewTable(string name, IEnumerable<string> parameterColumns)
        {
            var table = new DataTable(name);
            table.Columns.Add("Family and Type", typeof(string));
            foreach (string column in parameterColumns) table.Columns.Add(column, typeof(string));
            return table;
        }

        private static void AddRow(DataTable table, string key, IEnumerable<string> values)
        {
            DataRow row = table.NewRow();
            row[0] = key;
            int c = 1;
            foreach (string value in values) row[c++] = value;
            table.Rows.Add(row);
        }
    }
}
