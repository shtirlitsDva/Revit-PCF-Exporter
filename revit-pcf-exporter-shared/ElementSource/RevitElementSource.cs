using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;

using MoreLinq;

using PcfExporter.Configuration;
using PcfExporter.Context;

using Shared;

using plst = PcfExporter.Model.Parameters;

namespace PcfExporter.ElementSource
{
    public sealed class RevitElementSource : IElementSource
    {
        private readonly IRevitContext _ctx;

        public RevitElementSource(IRevitContext ctx) => _ctx = ctx;

        public HashSet<Element> Collect(PcfConfiguration cfg)
        {
            Document doc = _ctx.Doc;
            var collector = new FilteredElementCollector(doc);

            switch (cfg.Scope)
            {
                case ExportScope.AllInOneFile:
                    // (Fitting OR Accessory OR Pipe) AND (Pipe OR FamilyInstance):
                    // the second leg removes FamilySymbols, which would throw later on.
                    collector.WherePasses(new LogicalAndFilter(new List<ElementFilter>
                    {
                        new LogicalOrFilter(new List<ElementFilter>
                        {
                            new ElementCategoryFilter(BuiltInCategory.OST_PipeFitting),
                            new ElementCategoryFilter(BuiltInCategory.OST_PipeAccessory),
                            new ElementClassFilter(typeof(Pipe))
                        }),
                        new LogicalOrFilter(new List<ElementFilter>
                        {
                            new ElementClassFilter(typeof(Pipe)),
                            new ElementClassFilter(typeof(FamilyInstance))
                        })
                    }));
                    return collector.ToElements().ToHashSet();

                case ExportScope.AllInSeparateFiles:
                case ExportScope.SpecificPipeline:
                    // System Abbreviation filter also filters FamilySymbols out.
                    collector.WherePasses(
                        new LogicalOrFilter(new List<ElementFilter>
                        {
                            new ElementCategoryFilter(BuiltInCategory.OST_PipeFitting),
                            new ElementCategoryFilter(BuiltInCategory.OST_PipeAccessory),
                            new ElementClassFilter(typeof(Pipe))
                        }))
                        .WherePasses(Shared.Filter.ParameterValueGenericFilter(
                            doc, cfg.SelectedSystemAbbreviation,
                            BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM));
                    return collector.ToElements().ToHashSet();

                case ExportScope.Selection:
                default:
                    return _ctx.SelectedIds.Select(id => doc.GetElement(id)).ToHashSet();
            }
        }

        public HashSet<Element> CollectFiltered(PcfConfiguration cfg, FilterOptions options)
        {
            Document doc = _ctx.Doc;
            IEnumerable<Element> filtering = Collect(cfg);

            if (options.FilterByDiameter)
                filtering = filtering.Where(x => ElementFilters.PassesDiameterLimit(x, cfg));

            if (options.FilterByPcfElemExcl)
                filtering = from element in filtering
                            let par = element.get_Parameter(plst.PCF_ELEM_EXCL.Guid)
                            where par != null && par.AsInteger() == 0
                            select element;

            if (options.FilterByPcfPiplExcl)
                filtering = filtering.Where(x => x.PipingSystemAllowed(doc));

            if (options.FilterOutInstrumentPipes)
                filtering = filtering.ExceptWhere(x => x.get_Parameter(
                    BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM).AsString() == "INSTR");

            if (options.FilterOutSpecifiedPcfElemSpec)
                filtering = from element in filtering
                            let par = element.get_Parameter(plst.PCF_ELEM_SPEC.Guid)
                            where par != null && par.AsString() != cfg.SpecFilter
                            select element;

            if (options.FilterForIsogen)
                filtering = filtering.ExceptWhere(x => x.get_Parameter(
                    BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM).AsString() == "ARGD");

            return filtering.ToHashSet();
        }
    }
}
