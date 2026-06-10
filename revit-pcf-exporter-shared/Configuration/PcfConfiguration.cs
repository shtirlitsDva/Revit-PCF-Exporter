using System;

namespace PcfExporter.Configuration
{
    /// <summary>
    /// Which elements participate in an export run.
    /// </summary>
    public enum ExportScope
    {
        AllInOneFile,
        AllInSeparateFiles,
        SpecificPipeline,
        Selection
    }

    public enum BoreUnits { Mm, Inch }
    public enum CoordsUnits { Mm, Inch }
    public enum WeightUnits { Kgs, Lbs }
    public enum WeightLengthUnits { Meter, Feet }
    public enum OutputEncodingChoice { Ansi, Utf8Bom }

    /// <summary>
    /// Immutable snapshot of all user-facing export settings.
    /// The UI builds an instance; the engine receives it as an argument.
    /// Engine code must never read settings from any other source.
    /// </summary>
    public sealed class PcfConfiguration
    {
        public ExportScope Scope { get; set; } = ExportScope.AllInOneFile;
        /// <summary>System Abbreviation of the pipeline when Scope is SpecificPipeline.</summary>
        public string SelectedSystemAbbreviation { get; set; } = "";

        public BoreUnits BoreUnits { get; set; } = BoreUnits.Mm;
        public CoordsUnits CoordsUnits { get; set; } = CoordsUnits.Mm;
        public WeightUnits WeightUnits { get; set; } = WeightUnits.Kgs;
        public WeightLengthUnits WeightLengthUnits { get; set; } = WeightLengthUnits.Meter;
        public OutputEncodingChoice OutputEncoding { get; set; } = OutputEncodingChoice.Ansi;

        public string OutputDirectory { get; set; } = "";
        public string ElementsExcelPath { get; set; } = "";
        public string LdtPath { get; set; } = "";
        public string ProjectIdentifier { get; set; } = "";

        /// <summary>Elements with nominal size at or below this limit are excluded.</summary>
        public double DiameterLimit { get; set; } = 0;
        /// <summary>Elements whose PCF_ELEM_SPEC equals this value are excluded. Empty disables the filter.</summary>
        public string SpecFilter { get; set; } = "EXISTING";

        public bool ExportToIsogen { get; set; } = true;
        public bool ExportToCii { get; set; } = false;
        /// <summary>True: parameter population overwrites existing values. False: append (only fill empty).</summary>
        public bool Overwrite { get; set; } = true;

        public string UnitsBoreKeyword => BoreUnits == BoreUnits.Mm ? "MM" : "INCH";
        public string UnitsCoOrdsKeyword => CoordsUnits == CoordsUnits.Mm ? "MM" : "INCH";
        public string UnitsWeightKeyword => WeightUnits == WeightUnits.Kgs ? "KGS" : "LBS";
        public string UnitsWeightLengthKeyword => WeightLengthUnits == WeightLengthUnits.Meter ? "METER" : "FEET";

        public PcfConfiguration Clone() => (PcfConfiguration)MemberwiseClone();
    }
}
