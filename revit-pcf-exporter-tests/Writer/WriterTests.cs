using System;
using System.Linq;
using System.Reflection;

using PcfExporter.Configuration;
using PcfExporter.Writer;

using Xunit;

namespace PcfExporter.Tests.Writer
{
    public class PreambleTests
    {
        [Fact]
        public void Preamble_DefaultConfiguration_MatchesGolden()
        {
            string preamble = DocumentComposer.Preamble(new PcfConfiguration()).ToString();

            string expected =
                "ISOGEN-FILES ISOGEN.FLS" + Environment.NewLine +
                "UNITS-BORE MM" + Environment.NewLine +
                "UNITS-CO-ORDS MM" + Environment.NewLine +
                "UNITS-WEIGHT KGS" + Environment.NewLine +
                "UNITS-BOLT-DIA MM" + Environment.NewLine +
                "UNITS-BOLT-LENGTH MM" + Environment.NewLine +
                "UNITS-WEIGHT-LENGTH METER" + Environment.NewLine;

            Assert.Equal(expected, preamble);
        }

        [Fact]
        public void Preamble_ImperialConfiguration_UsesImperialKeywords()
        {
            var cfg = new PcfConfiguration
            {
                BoreUnits = BoreUnits.Inch,
                CoordsUnits = CoordsUnits.Inch,
                WeightUnits = WeightUnits.Lbs,
                WeightLengthUnits = WeightLengthUnits.Feet
            };
            string preamble = DocumentComposer.Preamble(cfg).ToString();

            Assert.Contains("UNITS-BORE INCH", preamble);
            Assert.Contains("UNITS-CO-ORDS INCH", preamble);
            Assert.Contains("UNITS-WEIGHT LBS", preamble);
            Assert.Contains("UNITS-WEIGHT-LENGTH FEET", preamble);
        }
    }

    public class FilenameBuilderTests
    {
        private static readonly DateTime Stamp = new DateTime(2026, 6, 10, 13, 45, 30);

        [Fact]
        public void Build_AllInOneFile_UsesAllLinesScope()
        {
            var (path, line) = FilenameBuilder.Build(
                "Project1", ExportScope.AllInOneFile, "FVF", @"C:\out", Stamp);

            Assert.Equal(@"C:\out\Project1_2026.06.10-13.45.30_All_Lines.pcf", path);
            Assert.Equal("    ATTRIBUTE59 Project1_2026.06.10-13.45.30_All_Lines.pcf" + Environment.NewLine, line);
        }

        [Theory]
        [InlineData(ExportScope.SpecificPipeline, "FVF")]
        [InlineData(ExportScope.AllInSeparateFiles, "FVR")]
        public void Build_PipelineScopes_UseSystemAbbreviation(ExportScope scope, string sysAbbr)
        {
            var (path, _) = FilenameBuilder.Build("P", scope, sysAbbr, @"C:\out", Stamp);
            Assert.EndsWith($"_{sysAbbr}.pcf", path);
        }

        [Fact]
        public void Build_Selection_UsesSelectionScope()
        {
            var (path, _) = FilenameBuilder.Build("P", ExportScope.Selection, "FVF", @"C:\out", Stamp);
            Assert.EndsWith("_Selection.pcf", path);
        }
    }

    public class SpecTablesTests
    {
        [Fact]
        public void LoadEmbedded_FindsAllFiveSpecTables()
        {
            //Regression for the original bug: File.Exists() on embedded-resource names
            //silently skipped every table, so wall thickness was never exported.
            var specs = SpecTables.LoadEmbedded(typeof(SpecTables).Assembly);
            Assert.NotNull(specs);

            foreach (string spec in new[] { "C02", "C03", "C08", "S02", "S03" })
            {
                //Every table must resolve at least one of its own sizes.
                string anyLine = Enumerable.Range(1, 1200)
                    .Select(dn => specs.GetWallThicknessLine(spec, dn.ToString()))
                    .FirstOrDefault(l => l != "");
                Assert.False(string.IsNullOrEmpty(anyLine),
                    $"Spec table {spec} loaded but resolves no sizes — is the CSV malformed?");
            }
        }

        [Fact]
        public void GetWallThicknessLine_UnknownSpec_ReturnsEmpty()
        {
            var specs = SpecTables.LoadEmbedded(typeof(SpecTables).Assembly);
            Assert.Equal("", specs.GetWallThicknessLine("NO-SUCH-SPEC", "100"));
            Assert.Equal("", specs.GetWallThicknessLine("", "100"));
            Assert.Equal("", specs.GetWallThicknessLine(null, "100"));
        }

        [Fact]
        public void GetWallThicknessLine_KnownSize_WritesComponentAttribute1()
        {
            var specs = SpecTables.LoadEmbedded(typeof(SpecTables).Assembly);
            //Find a size that exists and assert the exact line shape.
            string line = Enumerable.Range(1, 1200)
                .Select(dn => specs.GetWallThicknessLine("C02", dn.ToString()))
                .First(l => l != "");
            Assert.StartsWith("    COMPONENT-ATTRIBUTE1 ", line);
            Assert.EndsWith("\n", line);
        }
    }

    public class PcfFormatTests
    {
        [Fact]
        public void PointMm_FormatsFeetAsMillimetres_InvariantOfCulture()
        {
            //1 ft = 304.8 mm
            Assert.Equal("304.8 609.6 -152.4", PcfFormat.PointMm(1, 2, -0.5));
        }

        [Fact]
        public void PointMm_RoundsAwayFromZero_WithRequestedDecimals()
        {
            //0.0001 ft = 0.03048 mm
            Assert.Equal("0.030 0.000 0.000", PcfFormat.PointMm(0.0001, 0, 0, 3));
        }
    }
}
