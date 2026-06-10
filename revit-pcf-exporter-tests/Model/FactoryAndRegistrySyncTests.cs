using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using PcfExporter.Model;
using PcfExporter.Tests.Support;

using Xunit;

namespace PcfExporter.Tests.Model
{
    /// <summary>
    /// Source-level sync tests for the element-type vocabulary and the parameter
    /// registry. Source-based because Revit API objects (Element, Parameter) cannot
    /// be instantiated outside Revit.
    /// </summary>
    public class FactoryAndRegistrySyncTests
    {
        private static string SharedDir => RepoPaths.SharedProjectDir();

        /// <summary>Every PcfElementTypes constant must be handled by the factory switch.</summary>
        [Fact]
        public void Factory_HandlesEveryElementType()
        {
            string factorySource = File.ReadAllText(
                Path.Combine(SharedDir, "Model", "PcfElementFactory.cs"));

            //Constant-name → constant-value via the real class
            var constants = typeof(PcfElementTypes)
                .GetFields()
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .ToDictionary(f => f.Name, f => (string)f.GetValue(null));

            var missing = constants.Keys
                .Where(name => !factorySource.Contains($"case et.{name}:"))
                .ToList();

            Assert.True(missing.Count == 0,
                "PcfElementFactory does not handle these PcfElementTypes: " + string.Join(", ", missing));
        }

        [Fact]
        public void ElementTypes_AllList_ContainsEveryConstant()
        {
            var constants = typeof(PcfElementTypes)
                .GetFields()
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetValue(null))
                .ToHashSet();

            Assert.Equal(constants, PcfElementTypes.All.ToHashSet());
        }

        /// <summary>
        /// Parameter GUIDs must be defined exactly once — in the registry. Any other
        /// file hardcoding a registry GUID is a desync waiting to happen (this was a
        /// real bug: PCF_ELEM_SPEC's GUID was duplicated inline in two places).
        /// </summary>
        [Fact]
        public void RegistryGuids_AreNotDuplicatedElsewhereInSource()
        {
            string registryPath = Path.Combine(SharedDir, "Model", "ParameterRegistry.cs");
            var guidRegex = new Regex(
                "new Guid\\(\"(?<g>[0-9a-fA-F-]{36})\"\\)", RegexOptions.Compiled);

            var registryGuids = guidRegex.Matches(File.ReadAllText(registryPath))
                .Cast<Match>()
                .Select(m => m.Groups["g"].Value.ToLowerInvariant())
                .ToHashSet();
            Assert.NotEmpty(registryGuids);

            var offenders = new List<string>();
            foreach (string file in Directory.GetFiles(SharedDir, "*.cs", SearchOption.AllDirectories))
            {
                if (string.Equals(file, registryPath, StringComparison.OrdinalIgnoreCase)) continue;
                foreach (Match m in guidRegex.Matches(File.ReadAllText(file)))
                {
                    string g = m.Groups["g"].Value.ToLowerInvariant();
                    if (registryGuids.Contains(g))
                        offenders.Add($"{Path.GetFileName(file)}: {g}");
                }
            }

            Assert.True(offenders.Count == 0,
                "Registry GUIDs are hardcoded outside ParameterRegistry.cs:\n" + string.Join("\n", offenders));
        }

        /// <summary>Registry GUIDs must be unique (source-level — no Revit needed).</summary>
        [Fact]
        public void RegistryGuids_AreUnique()
        {
            string registrySource = File.ReadAllText(
                Path.Combine(SharedDir, "Model", "ParameterRegistry.cs"));

            //Only count ACTIVE definitions — commented-out parameters are kept on purpose.
            var active = registrySource
                .Split('\n')
                .Where(l => !l.TrimStart().StartsWith("//"))
                .ToArray();

            var guids = new Regex("new Guid\\(\"(?<g>[0-9a-fA-F-]{36})\"\\)")
                .Matches(string.Join("\n", active))
                .Cast<Match>()
                .Select(m => m.Groups["g"].Value.ToLowerInvariant())
                .ToList();

            var duplicates = guids.GroupBy(g => g).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            Assert.True(duplicates.Count == 0,
                "Duplicate parameter GUIDs in the registry: " + string.Join(", ", duplicates));
        }
    }
}
