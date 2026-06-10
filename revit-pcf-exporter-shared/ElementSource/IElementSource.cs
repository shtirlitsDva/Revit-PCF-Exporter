using System.Collections.Generic;

using Autodesk.Revit.DB;

using PcfExporter.Configuration;

namespace PcfExporter.ElementSource
{
    /// <summary>
    /// Owns "which Revit elements participate". Collects by export scope and applies
    /// the standard exclusion filters. The single home for the collection logic that
    /// used to be copy-pasted across PCF_Main and PCF_Parameters.
    /// </summary>
    public interface IElementSource
    {
        /// <summary>Raw scope collection: pipes, pipe fittings, pipe accessories per the configured scope.</summary>
        HashSet<Element> Collect(PcfConfiguration cfg);

        /// <summary>Scope collection with the standard export filters applied.</summary>
        HashSet<Element> CollectFiltered(PcfConfiguration cfg, FilterOptions options);
    }

    /// <summary>Which exclusion filters apply to a collection pass.</summary>
    public sealed class FilterOptions
    {
        public bool FilterByDiameter { get; set; }
        public bool FilterByPcfElemExcl { get; set; }
        public bool FilterByPcfPiplExcl { get; set; }
        public bool FilterOutInstrumentPipes { get; set; }
        public bool FilterOutSpecifiedPcfElemSpec { get; set; }
        public bool FilterForIsogen { get; set; }

        /// <summary>The standard filter set used by the PCF export itself.</summary>
        public static FilterOptions ForExport(PcfConfiguration cfg) => new FilterOptions
        {
            FilterByDiameter = true,
            FilterByPcfElemExcl = true,
            FilterByPcfPiplExcl = true,
            FilterOutInstrumentPipes = true,
            FilterOutSpecifiedPcfElemSpec = !string.IsNullOrEmpty(cfg.SpecFilter),
            FilterForIsogen = cfg.ExportToIsogen
        };
    }
}
