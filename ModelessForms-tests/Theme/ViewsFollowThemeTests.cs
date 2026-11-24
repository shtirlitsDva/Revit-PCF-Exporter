using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using Xunit;

namespace ModelessForms.Tests.Theme
{
    /// <summary>
    /// Keeps the Issue Tracker views in sync with the shared theme (spec M10 / D3):
    /// every control type used in a view has an implicit, retemplated style in the
    /// theme, and the views set no colours, fonts, templates or inline styles.
    /// </summary>
    public sealed class ViewsFollowThemeTests
    {
        private static readonly XNamespace Wpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        /// <summary>Control types that need no retemplate, with the reason.</summary>
        private static readonly Dictionary<string, string> NoTemplateNeeded = new Dictionary<string, string>
        {
            // The OS draws the frame; ThemeManager.Apply sets the background and the dark title bar.
            ["Window"] = "native frame",
            // The default template is an ItemsPresenter in a Border with no brushes: nothing to colour.
            ["ItemsControl"] = "no chrome",
        };

        /// <summary>Controls that WPF creates inside the controls in the views.</summary>
        /// <remarks>ContextMenu, MenuItem and Separator: the built-in Cut/Copy/Paste menu of TextBox and PasswordBox.</remarks>
        private static readonly string[] GeneratedControls =
            { "ListBoxItem", "ComboBoxItem", "ScrollBar", "ToolTip", "ContextMenu", "MenuItem", "Separator" };

        private static readonly string[] ForbiddenAttributes =
        {
            "Background", "Foreground", "BorderBrush", "BorderThickness", "Fill", "Stroke",
            "FontFamily", "FontSize", "FontWeight", "FontStyle", "Template", "Padding", "CornerRadius",
        };

        private static readonly Regex HexColour = new Regex("#[0-9A-Fa-f]{3,8}\\b");

        private static string ViewsDir => Path.Combine(AppContext.BaseDirectory, "Views");

        private static IEnumerable<(string File, XElement Element)> ViewElements() =>
            Directory.GetFiles(ViewsDir, "*.xaml")
                .SelectMany(f => XDocument.Load(f).Descendants().Select(e => (Path.GetFileName(f), e)));

        [Fact]
        public void The_views_are_found()
        {
            Assert.True(Directory.GetFiles(ViewsDir, "*.xaml").Length >= 5, "The view XAML files were not copied to the test output.");
        }

        [Fact]
        public void Every_control_in_the_views_has_a_retemplated_implicit_style()
        {
            var used = ViewElements()
                .Where(x => x.Element.Name.Namespace == Wpf && !x.Element.Name.LocalName.Contains('.'))
                .Select(x => x.Element.Name.LocalName)
                .Distinct()
                .Select(name => typeof(Control).Assembly.GetType("System.Windows.Controls." + name)
                             ?? typeof(Control).Assembly.GetType("System.Windows." + name))
                .Where(t => t != null && typeof(Control).IsAssignableFrom(t))
                .Select(t => t!.Name)
                .Concat(GeneratedControls)
                .Distinct()
                .Where(name => !NoTemplateNeeded.ContainsKey(name))
                .ToList();

            var missing = new List<string>();
            ThemeTests.RunSta(() =>
            {
                ResourceDictionary theme = ThemeTests.LoadTheme();
                foreach (string name in used)
                {
                    Type type = typeof(Control).Assembly.GetType("System.Windows.Controls." + name)
                                ?? typeof(Control).Assembly.GetType("System.Windows.Controls.Primitives." + name)!;
                    var style = theme[type] as Style;
                    if (style == null || ThemeTests.ThemeTemplates(style).Count == 0) missing.Add(name);
                }
            });

            Assert.True(missing.Count == 0, "No retemplated implicit style in the theme for: " + string.Join(", ", missing));
        }

        [Fact]
        public void The_views_set_no_colours_fonts_templates_or_inline_styles()
        {
            var problems = new List<string>();
            foreach (var (file, element) in ViewElements())
            {
                string name = element.Name.LocalName;
                if (name == "Style" || name.EndsWith(".Style") || name.EndsWith(".Resources") && element.Elements().Any(e => e.Name.LocalName == "Style"))
                    problems.Add($"{file}: inline style <{name}>");
                if (name.EndsWith(".Template") || name == "ControlTemplate")
                    problems.Add($"{file}: inline template <{name}>");

                foreach (XAttribute attribute in element.Attributes())
                {
                    string property = attribute.Name.LocalName;
                    if (ForbiddenAttributes.Contains(property))
                        problems.Add($"{file}: <{name} {property}=\"{attribute.Value}\">");
                    if (HexColour.IsMatch(attribute.Value))
                        problems.Add($"{file}: colour value in <{name} {property}=\"{attribute.Value}\">");
                    if (property == "Style" && !attribute.Value.StartsWith("{DynamicResource ", StringComparison.Ordinal))
                        problems.Add($"{file}: <{name} Style=\"{attribute.Value}\"> must use a DynamicResource theme key");
                }
            }

            Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
        }

        [Fact]
        public void Every_style_key_in_the_views_exists_in_the_theme()
        {
            var keys = ViewElements()
                .SelectMany(x => x.Element.Attributes())
                .Select(a => Regex.Match(a.Value, @"^\{DynamicResource ([^}]+)\}$"))
                .Where(m => m.Success)
                .Select(m => m.Groups[1].Value.Trim())
                .Distinct()
                .ToList();

            var missing = new List<string>();
            ThemeTests.RunSta(() =>
            {
                ResourceDictionary theme = ThemeTests.LoadTheme();
                missing.AddRange(keys.Where(k => !theme.Contains(k)));
            });

            Assert.True(missing.Count == 0, "Theme keys used in the views but not defined: " + string.Join(", ", missing));
        }
    }
}
