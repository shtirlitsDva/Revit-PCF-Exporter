using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using PcfExporter.Tests.Support;
using PcfExporter.UI.ViewModels;

using Xunit;

namespace PcfExporter.Tests.UI
{
    /// <summary>
    /// Source-level XAML checks: binding paths resolve against the ViewModel,
    /// every control type used has a retemplated implicit style in Theme.xaml,
    /// and views contain no inline styling (centralized Theme.xaml only).
    /// </summary>
    public class XamlSyncTests
    {
        private static string UiDir => Path.Combine(RepoPaths.SharedProjectDir(), "UI");
        private static string[] ViewFiles => Directory.GetFiles(
            Path.Combine(UiDir, "Views"), "*.xaml", SearchOption.AllDirectories);
        private static string ThemePath => Path.Combine(UiDir, "Theme.xaml");

        private static readonly Regex BindingRegex = new Regex(
            @"\{Binding\s+(?:Path=)?(?<path>[A-Za-z_][A-Za-z0-9_.]*)?", RegexOptions.Compiled);

        [Fact]
        public void AllBindingPaths_ExistOnMainViewModel()
        {
            //MessageWindow has no bindings (code-behind only); MainWindow binds MainViewModel.
            string xaml = File.ReadAllText(Path.Combine(UiDir, "Views", "MainWindow.xaml"));
            var unresolved = new List<string>();

            foreach (Match m in BindingRegex.Matches(xaml))
            {
                string path = m.Groups["path"].Value;
                if (string.IsNullOrEmpty(path)) continue; //{Binding} on DataContext itself
                string root = path.Split('.')[0];
                PropertyInfo prop = typeof(MainViewModel).GetProperty(
                    root, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null) unresolved.Add(path);
            }

            Assert.True(unresolved.Count == 0,
                "MainWindow.xaml binds paths that do not exist on MainViewModel: " +
                string.Join(", ", unresolved.Distinct()));
        }

        /// <summary>Controls used in views must have an implicit, retemplated style.</summary>
        [Fact]
        public void Theme_RetemplatesEveryControlTypeUsedInViews()
        {
            //Control types that require a Template override to be dark.
            var templated = new[]
            {
                "Button", "RadioButton", "CheckBox", "TextBox", "ComboBox",
                "TabControl", "TabItem", "GroupBox", "ScrollViewer", "ScrollBar",
                "ToolTip", "ProgressBar", "DataGrid", "DataGridColumnHeader",
                "DataGridRow", "DataGridCell"
            };

            XDocument theme = XDocument.Load(ThemePath);
            XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

            var implicitStyles = theme.Root.Elements()
                .Where(e => e.Name.LocalName == "Style" && e.Attribute(x + "Key") == null)
                .ToDictionary(
                    e => StripTypePrefix(e.Attribute("TargetType")?.Value),
                    e => e);

            var usedTypes = ViewFiles
                .SelectMany(f => XDocument.Load(f).Descendants().Select(e => e.Name.LocalName))
                .Distinct()
                .ToHashSet();

            foreach (string control in templated)
            {
                bool usedDirectly = usedTypes.Contains(control);
                //Not written in views, but rendered inside other controls' templates:
                var partOfOtherTemplates = new HashSet<string>
                {
                    "ScrollBar", "ToolTip", "TabItem", "ComboBox", "ProgressBar",
                    "DataGridColumnHeader", "DataGridRow", "DataGridCell"
                };
                if (!usedDirectly && !partOfOtherTemplates.Contains(control)) continue;

                Assert.True(implicitStyles.ContainsKey(control),
                    $"Theme.xaml has no implicit style for {control}.");

                bool hasTemplateSetter = implicitStyles[control]
                    .Descendants()
                    .Any(e => e.Name.LocalName == "Setter"
                              && e.Attribute("Property")?.Value == "Template");
                Assert.True(hasTemplateSetter,
                    $"Theme.xaml style for {control} does not override Template — " +
                    "recoloring only is not enough (default chrome leaks light theme).");
            }
        }

        /// <summary>Views must not style inline — Theme.xaml is the only styling source.</summary>
        [Fact]
        public void Views_HaveNoInlineStyling()
        {
            string[] forbidden =
            {
                "Background", "Foreground", "BorderBrush", "FontSize",
                "FontFamily", "FontWeight", "Padding", "Template"
            };

            var violations = new List<string>();
            foreach (string file in ViewFiles)
            {
                XDocument doc = XDocument.Load(file);
                foreach (XElement element in doc.Descendants())
                {
                    //Setters inside local ResourceDictionaries would also be styling:
                    if (element.Name.LocalName == "Setter")
                        violations.Add($"{Path.GetFileName(file)}: local <Setter> on {element.Attribute("Property")?.Value}");

                    foreach (XAttribute attr in element.Attributes())
                        if (forbidden.Contains(attr.Name.LocalName))
                            violations.Add($"{Path.GetFileName(file)}: <{element.Name.LocalName} {attr.Name.LocalName}=\"{attr.Value}\">");
                }
            }

            Assert.True(violations.Count == 0,
                "Inline styling found in views (move it to Theme.xaml):\n" + string.Join("\n", violations));
        }

        /// <summary>The palette colors must each be defined exactly once (in Theme.xaml).</summary>
        [Fact]
        public void Views_ContainNoColorLiterals()
        {
            var colorRegex = new Regex("#[0-9A-Fa-f]{6,8}");
            foreach (string file in ViewFiles)
            {
                string xaml = File.ReadAllText(file);
                Assert.False(colorRegex.IsMatch(xaml),
                    $"{Path.GetFileName(file)} contains a color literal — colors belong in Theme.xaml.");
            }
        }

        private static string StripTypePrefix(string targetType)
        {
            if (targetType == null) return "";
            //"{x:Type ComboBox}" or "ComboBox"
            Match m = Regex.Match(targetType, @"\{x:Type\s+(?<t>\w+)\}");
            return m.Success ? m.Groups["t"].Value : targetType;
        }
    }
}
