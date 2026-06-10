using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;

using PcfExporter.Configuration;
using PcfExporter.Context;
using PcfExporter.ElementSource;
using PcfExporter.Model;
using PcfExporter.Output;
using PcfExporter.Writer;

using Shared;

using plst = PcfExporter.Model.Parameters;

namespace PcfExporter.Orchestration
{
    public sealed class ExportResult
    {
        public List<string> WrittenFiles { get; } = new List<string>();
        public int ElementCount { get; set; }
    }

    /// <summary>
    /// Runs the PCF export: collect → filter → build element model → compose → write.
    /// The whole workflow lives here — including the all-pipelines-in-separate-files
    /// loop that used to live in a button handler.
    /// </summary>
    public interface IPcfExportService
    {
        ExportResult Export(IRevitContext ctx, PcfConfiguration cfg);
    }

    public sealed class PcfExportService : IPcfExportService
    {
        private readonly IOutputWriter _output;

        public PcfExportService() : this(new FileOutputWriter()) { }
        public PcfExportService(IOutputWriter output) => _output = output;

        public ExportResult Export(IRevitContext ctx, PcfConfiguration cfg)
        {
            var result = new ExportResult();

            if (cfg.Scope == ExportScope.AllInSeparateFiles)
            {
                //One full run per pipeline, each with its own file.
                foreach (string sysAbbr in MepUtils.GetDistinctPhysicalPipingSystemTypeNames(ctx.Doc, true))
                {
                    PcfConfiguration runCfg = cfg.Clone();
                    runCfg.SelectedSystemAbbreviation = sysAbbr;
                    ExportSingle(ctx, runCfg, result);
                }
            }
            else
            {
                ExportSingle(ctx, cfg, result);
            }

            return result;
        }

        private void ExportSingle(IRevitContext ctx, PcfConfiguration cfg, ExportResult result)
        {
            Document doc = ctx.Doc;
            var session = new ExportSession(ctx, cfg, SpecTables.LoadEmbedded());
            var factory = new PcfElementFactory(session);
            var source = new RevitElementSource(ctx);

            var sbCollect = new StringBuilder();
            sbCollect.Append(DocumentComposer.Preamble(cfg));

            #region Collect and filter
            HashSet<Element> elements;
            try
            {
                elements = source.CollectFiltered(cfg, FilterOptions.ForExport(cfg));
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Filtering the export set threw an exception:\n" + ex.Message +
                    "\nTo fix:\n1. See if parameter PCF_ELEM_EXCL exists; if not, rerun parameter import.",
                    ex);
            }
            #endregion

            #region Validate PCF_MAT_DESCR
            //Make sure that every element has PCF_MAT_DESCR filled out.
            foreach (Element e in elements)
            {
                if (string.IsNullOrEmpty(e.get_Parameter(plst.PCF_MAT_DESCR.Guid).AsString()))
                {
                    ctx.SetSelection(new[] { e.Id });
                    throw new Exception(
                        $"PCF_MAT_DESCR is empty for element {e.Id}! " +
                        "Please correct this issue before exporting again. " +
                        "The element has been selected in the model.");
                }
            }
            #endregion

            #region Build element model
            var oopElements = elements.Select(factory.CreatePhysicalElement).ToHashSet();

            var virtuals = oopElements
                .SelectMany(factory.CreateDependentVirtualElements)
                .ToHashSet();

            var specials = factory.CreateSpecialVirtualElements(oopElements);

            var startPoints = new HashSet<IPcfElement>();
            specials.RemoveWhere(e =>
            {
                if (e is PCF_VIRTUAL_STARTPOINT sp)
                {
                    startPoints.Add(sp);
                    return true;
                }
                return false;
            });

            oopElements.UnionWith(specials);
            oopElements.UnionWith(virtuals);

            //Extract TAP elements so they do not disturb the material data
            var taps = oopElements.ExtractBy(x => x is PCF_TAP);
            #endregion

            #region Material data
            //Material groups by PCF_MAT_DESCR; sequential COMPID per element, MAT_ID per group.
            int elementIdentificationNumber = 0;
            int materialGroupIdentifier = 0;

            IEnumerable<IGrouping<string, IPcfElement>> materialGroups = oopElements
                .Where(x => x.ParticipateInMaterialTable)
                .GroupBy(x => x.GetParameterValue(plst.PCF_MAT_DESCR));

            ctx.RunInTransaction("Set PCF_ELEM_COMPID and PCF_MAT_ID", () =>
            {
                foreach (var group in materialGroups)
                {
                    materialGroupIdentifier++;
                    foreach (var element in group)
                    {
                        elementIdentificationNumber++;
                        element.SetParameterValue(plst.PCF_ELEM_COMPID, elementIdentificationNumber.ToString());
                        element.SetParameterValue(plst.PCF_MAT_ID, materialGroupIdentifier.ToString());
                    }
                }
            });
            #endregion

            var pipelineGroups = oopElements
                .GroupBy(x => x.SystemAbbreviation)
                .ToDictionary(x => x.Key, x => x.ToHashSet());

            (string fullPath, string attribute59Line) = FilenameBuilder.Build(
                doc.ProjectInformation.Name, cfg.Scope, cfg.SelectedSystemAbbreviation,
                cfg.OutputDirectory, DateTime.Now);

            //The TransactionGroup is rolled back at the end on purpose: tap processing
            //writes PCF_ELEM_TAPS values that are only needed while composing the
            //output text and must not persist in the model.
            using (var txGp = new TransactionGroup(doc))
            {
                txGp.Start("Temporary data for PCF export");

                foreach (KeyValuePair<string, HashSet<IPcfElement>> gp in pipelineGroups)
                {
                    sbCollect.Append(PipelineHeaderWriter.Write(gp.Key, doc, cfg));
                    sbCollect.Append(attribute59Line);
                    sbCollect.Append(StartPointWriter.Write(gp.Key, startPoints));
                    sbCollect.Append(EndsAndConnectionsWriter.Write(gp.Key, gp.Value, session));

                    #region Process TAPS
                    //Write TAP-CONNECTION data to the affected elements.
                    //ASSUMPTIONS:
                    //1. TAPS are always set on pipes -> cannot be connected to fittings/accessories
                    //   (circumvent by using the old TAP method)
                    //2. TAPS are always part of the same pipeline
                    ctx.RunInTransaction("Process TAP-CONNECTION elements", () =>
                    {
                        foreach (PCF_TAP tap in taps.Where(x => x.SystemAbbreviation == gp.Key))
                            tap.ProcessTaps();
                    });
                    #endregion

                    //Write the elements
                    sbCollect.Append(gp.Value
                        .OrderBy(x => x.GetParameterValue(plst.PCF_ELEM_TYPE))
                        .Select(x => x.ToPCFString())
                        .Aggregate((x, y) => x.Append(y)));
                }

                txGp.RollBack();
            }

            sbCollect.Append(DocumentComposer.MaterialsSection(materialGroups));

            _output.Write(fullPath, sbCollect, cfg.OutputEncoding);
            result.WrittenFiles.Add(fullPath);
            result.ElementCount += elementIdentificationNumber;
        }
    }
}
