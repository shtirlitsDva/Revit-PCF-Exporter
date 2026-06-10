using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PcfExporter.Writer
{
    /// <summary>
    /// Pipe-spec wall-thickness tables (DN → WTHK), loaded from the CSV files embedded
    /// under PipeSpecs\. The spec system is the single authority for wall thickness.
    /// </summary>
    public interface ISpecTables
    {
        /// <summary>
        /// The COMPONENT-ATTRIBUTE1 (wall thickness) line for the given spec and size,
        /// or empty string when the spec is unknown or has no entry for the size.
        /// An unknown spec is not an error: not every PCF_ELEM_SPEC value has a CSV table.
        /// </summary>
        string GetWallThicknessLine(string specName, string size);
    }

    public sealed class SpecTables : ISpecTables
    {
        private readonly Dictionary<string, Dictionary<string, string>> _specs;

        private SpecTables(Dictionary<string, Dictionary<string, string>> specs) => _specs = specs;

        /// <summary>Loads every *.csv embedded resource under a ".PipeSpecs." path in the given assembly.</summary>
        public static SpecTables LoadEmbedded() => LoadEmbedded(typeof(SpecTables).Assembly);

        public static SpecTables LoadEmbedded(Assembly assembly)
        {
            var specs = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            foreach (string resourceName in assembly.GetManifestResourceNames()
                .Where(n => n.Contains(".PipeSpecs.") && n.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)))
            {
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) continue;
                    using (var reader = new StreamReader(stream))
                    {
                        string name = SpecNameFromResource(resourceName);
                        specs[name] = ParseCsv(reader.ReadToEnd(), resourceName);
                    }
                }
            }

            if (specs.Count == 0)
                throw new InvalidOperationException(
                    "No PipeSpecs CSV tables were found as embedded resources. " +
                    "Wall thickness cannot be exported — check that PipeSpecs\\*.csv are embedded.");

            return new SpecTables(specs);
        }

        /// <summary>
        /// Parses the semicolon-separated spec table (header DN;OD;WTHK) into DN → WTHK.
        /// </summary>
        private static Dictionary<string, string> ParseCsv(string content, string resourceName)
        {
            var table = new Dictionary<string, string>();
            string[] lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
                throw new InvalidOperationException($"Spec table {resourceName} is empty.");

            string[] header = lines[0].Split(';');
            int dnIndex = Array.IndexOf(header, "DN");
            int wthkIndex = Array.IndexOf(header, "WTHK");
            if (dnIndex < 0 || wthkIndex < 0)
                throw new InvalidOperationException(
                    $"Spec table {resourceName} lacks DN/WTHK columns. Header was: {lines[0]}");

            foreach (string line in lines.Skip(1))
            {
                string[] fields = line.Split(';');
                if (fields.Length <= Math.Max(dnIndex, wthkIndex)) continue;
                table[fields[dnIndex].Trim()] = fields[wthkIndex].Trim();
            }
            return table;
        }

        private static string SpecNameFromResource(string resourceName)
        {
            var parts = resourceName.Split('.');
            return parts[parts.Length - 2]; // "...PipeSpecs.C02.csv" → "C02"
        }

        public string GetWallThicknessLine(string specName, string size)
        {
            if (string.IsNullOrEmpty(specName)) return "";
            if (!_specs.TryGetValue(specName, out var table)) return "";
            if (!table.TryGetValue(size, out string wthk)) return "";
            return $"    COMPONENT-ATTRIBUTE1 {wthk}\n";
        }
    }
}
