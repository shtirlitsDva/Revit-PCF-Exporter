using System.Collections.Generic;
using System.Data;

using PcfExporter.Configuration;
using PcfExporter.Context;
using PcfExporter.Orchestration;
using PcfExporter.Services;

namespace PcfExporter.Tests.Support
{
    public sealed class FakeExportService : IPcfExportService
    {
        public List<PcfConfiguration> Calls { get; } = new List<PcfConfiguration>();
        public ExportResult Result { get; set; } = new ExportResult();
        public ExportResult Export(IRevitContext ctx, PcfConfiguration cfg)
        {
            Calls.Add(cfg);
            return Result;
        }
    }

    public sealed class FakeBindingService : IParameterBindingService
    {
        public string CreateAllBindings(IRevitContext ctx) => "created";
        public string DeleteAllBindings(IRevitContext ctx) => "deleted";
    }

    public sealed class FakePopulationService : IParameterPopulationService
    {
        public string PopulateElements(IRevitContext ctx, PcfConfiguration cfg, DataTable t) => "elements";
        public string PopulatePipelines(IRevitContext ctx, PcfConfiguration cfg, DataTable t) => "pipelines";
    }

    public sealed class FakeReportService : IParameterReportService
    {
        public DataTable UndefinedElementsResult { get; set; } = new DataTable("Undefined elements");
        public DataTable UndefinedPipelinesResult { get; set; } = new DataTable("Undefined pipelines");
        public IReadOnlyList<DataTable> CurrentValues(IRevitContext ctx) =>
            new[] { new DataTable("Pipelines"), new DataTable("Elements") };
        public DataTable UndefinedElements(IRevitContext ctx, PcfConfiguration cfg, DataTable t) => UndefinedElementsResult;
        public DataTable UndefinedPipelines(IRevitContext ctx, PcfConfiguration cfg, DataTable t) => UndefinedPipelinesResult;
    }

    public sealed class FakeScheduleService : IScheduleService
    {
        public void CreatePcfSchedules(IRevitContext ctx) { }
    }
}
