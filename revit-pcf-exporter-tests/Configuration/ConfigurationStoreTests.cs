using System;
using System.IO;
using System.Linq;
using System.Reflection;

using PcfExporter.Configuration;

using Xunit;

namespace PcfExporter.Tests.Configuration
{
    public class ConfigurationStoreTests : IDisposable
    {
        private readonly string _path = Path.Combine(
            Path.GetTempPath(), "pcf-tests-" + Guid.NewGuid().ToString("N") + ".cfg");

        public void Dispose()
        {
            if (File.Exists(_path)) File.Delete(_path);
        }

        /// <summary>
        /// Reflection round-trip: EVERY public writable property must survive
        /// save → load. Adding a property to PcfConfiguration automatically
        /// extends this test — forgetting persistence is impossible.
        /// </summary>
        [Fact]
        public void EveryProperty_RoundTrips()
        {
            var store = new FileConfigurationStore(_path);
            PcfConfiguration original = MakeNonDefault();

            store.Save(original);
            PcfConfiguration loaded = store.Load().Configuration;

            foreach (PropertyInfo p in WritableProperties())
            {
                object expected = p.GetValue(original);
                object actual = p.GetValue(loaded);
                Assert.True(Equals(expected, actual),
                    $"Property {p.Name} did not round-trip: saved '{expected}', loaded '{actual}'.");
            }
        }

        [Fact]
        public void Load_WithoutFile_ReturnsDefaults()
        {
            var store = new FileConfigurationStore(_path);
            PcfConfiguration loaded = store.Load().Configuration;
            var defaults = new PcfConfiguration();

            foreach (PropertyInfo p in WritableProperties())
                Assert.Equal(p.GetValue(defaults), p.GetValue(loaded));
        }

        /// <summary>
        /// User decision 2026-06-10: a malformed entry falls back to the default but is
        /// NEVER silent — the key must be reported so the UI can tell the user.
        /// </summary>
        [Fact]
        public void Load_WithMalformedLine_UsesDefaultAndReportsTheKey()
        {
            var store = new FileConfigurationStore(_path);
            store.Save(MakeNonDefault());

            //Corrupt one value
            string[] lines = File.ReadAllLines(_path);
            for (int i = 0; i < lines.Length; i++)
                if (lines[i].StartsWith("DiameterLimit=")) lines[i] = "DiameterLimit=not-a-number";
            File.WriteAllLines(_path, lines);

            ConfigurationLoadResult result = store.Load();
            Assert.Equal(new PcfConfiguration().DiameterLimit, result.Configuration.DiameterLimit);
            //The malformed key is reported by name:
            Assert.Contains("DiameterLimit", result.MalformedKeys);
            Assert.Single(result.MalformedKeys);
            //Other values still intact:
            Assert.Equal("ÆØÅ project", result.Configuration.ProjectIdentifier);
        }

        [Fact]
        public void Load_WithoutCorruption_ReportsNoMalformedKeys()
        {
            var store = new FileConfigurationStore(_path);
            store.Save(MakeNonDefault());
            Assert.Empty(store.Load().MalformedKeys);
        }

        [Fact]
        public void NonAsciiAndMultilineSafeStrings_RoundTrip()
        {
            var store = new FileConfigurationStore(_path);
            var cfg = new PcfConfiguration { ProjectIdentifier = "Æble\\Grød", SpecFilter = "S02" };
            store.Save(cfg);
            Assert.Equal("Æble\\Grød", store.Load().Configuration.ProjectIdentifier);
        }

        /// <summary>
        /// Regression: backslash followed by 'n'/'r' (most Windows paths) must survive.
        /// A sequential-Replace unescape turned "C:\norsyn" into "C:" + newline + "orsyn".
        /// </summary>
        [Theory]
        [InlineData(@"C:\norsyn\new\results")]
        [InlineData(@"X:\revit\nye projekter\rør")]
        [InlineData(@"\\server\share\norm")]
        [InlineData("literal\\nbackslash-n")]
        public void WindowsPaths_WithBackslashNandR_RoundTrip(string path)
        {
            var store = new FileConfigurationStore(_path);
            store.Save(new PcfConfiguration { OutputDirectory = path });
            Assert.Equal(path, store.Load().Configuration.OutputDirectory);
        }

        private static PropertyInfo[] WritableProperties() =>
            typeof(PcfConfiguration)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToArray();

        /// <summary>A configuration where every property differs from its default.</summary>
        private static PcfConfiguration MakeNonDefault()
        {
            var cfg = new PcfConfiguration();
            var defaults = new PcfConfiguration();
            foreach (PropertyInfo p in WritableProperties())
            {
                object newValue;
                if (p.PropertyType == typeof(string))
                    newValue = p.Name == "ProjectIdentifier" ? "ÆØÅ project" : "value-" + p.Name;
                else if (p.PropertyType == typeof(bool))
                    newValue = !(bool)p.GetValue(defaults);
                else if (p.PropertyType == typeof(double))
                    newValue = 32.5;
                else if (p.PropertyType.IsEnum)
                {
                    object[] values = Enum.GetValues(p.PropertyType).Cast<object>().ToArray();
                    newValue = values.First(v => !v.Equals(p.GetValue(defaults)));
                }
                else
                    throw new InvalidOperationException(
                        $"PcfConfiguration property {p.Name} has unsupported type {p.PropertyType}. " +
                        "Extend FileConfigurationStore AND this test.");
                p.SetValue(cfg, newValue);
            }
            return cfg;
        }
    }
}
