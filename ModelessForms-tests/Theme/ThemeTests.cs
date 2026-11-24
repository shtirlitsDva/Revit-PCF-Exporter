using System;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using Xunit;

namespace ModelessForms.Tests.Theme
{
    /// <summary>
    /// Loads the shared theme the same way ThemeManager does in Revit (XamlReader,
    /// Colors merged into Theme) and builds every themed control's template, so a
    /// broken style fails here and not in Revit.
    /// </summary>
    public sealed class ThemeTests
    {
        internal static string ThemeDir => Path.Combine(AppContext.BaseDirectory, "Theme");

        internal static ResourceDictionary LoadTheme()
        {
            ResourceDictionary Read(string file)
            {
                using var stream = File.OpenRead(Path.Combine(ThemeDir, file));
                return (ResourceDictionary)XamlReader.Load(stream);
            }

            var theme = Read("Theme.xaml");
            theme.MergedDictionaries.Insert(0, Read("Colors.xaml"));
            return theme;
        }

        internal static void RunSta(Action action)
        {
            ExceptionDispatchInfo? error = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { error = ExceptionDispatchInfo.Capture(ex); }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            error?.Throw();
        }

        /// <summary>Every Template that <paramref name="style"/> (or a BasedOn style) sets, in setters or triggers.</summary>
        internal static System.Collections.Generic.List<ControlTemplate> ThemeTemplates(Style? style)
        {
            var result = new System.Collections.Generic.List<ControlTemplate>();
            void Collect(SetterBaseCollection setters)
            {
                foreach (var setterBase in setters)
                {
                    if (setterBase is Setter setter && setter.Property == Control.TemplateProperty)
                        result.Add((ControlTemplate)setter.Value);
                }
            }

            for (var s = style; s != null; s = s.BasedOn)
            {
                Collect(s.Setters);
                foreach (var trigger in s.Triggers)
                {
                    if (trigger is Trigger t) Collect(t.Setters);
                }
            }
            return result;
        }

        [Fact]
        public void Theme_loads_and_every_themed_control_builds_its_template()
        {
            RunSta(() =>
            {
                var theme = LoadTheme();
                var window = new Window();
                window.Resources.MergedDictionaries.Add(theme);
                window.Style = (Style)theme[typeof(Window)];

                var combo = new ComboBox { ItemsSource = new[] { "a", "b" }, SelectedIndex = 0 };
                var list = new ListBox { ItemsSource = new[] { "a", "b" }, SelectedIndex = 0 };
                var controls = new Control[]
                {
                    new TextBox(), new TextBox { Style = (Style)theme["TextBox.MultiLine"] },
                    new PasswordBox(), new Button(),
                    new Button { Style = (Style)theme["Button.Primary"] },
                    new Button { Style = (Style)theme["Button.Danger"] },
                    new Button { Style = (Style)theme["Button.Icon"] },
                    combo, list, new ScrollBar(), new ScrollBar { Orientation = Orientation.Horizontal },
                    new ScrollViewer(), new GridSplitter(), new GroupBox(),
                };

                var panel = new StackPanel();
                foreach (var c in controls) panel.Children.Add(c);
                window.Content = panel;

                panel.Measure(new Size(800, 2000));
                panel.Arrange(new Rect(0, 0, 800, 2000));

                foreach (var c in controls)
                {
                    c.ApplyTemplate();
                    var style = c.Style ?? (Style)theme[c.GetType()];
                    Assert.Contains(c.Template, ThemeTemplates(style));
                    Assert.True(System.Windows.Media.VisualTreeHelper.GetChildrenCount(c) > 0,
                        $"{c.GetType().Name}: the theme template did not build.");
                }

                // Item containers are generated during layout.
                Assert.NotNull(list.ItemContainerGenerator.ContainerFromIndex(0));

                foreach (var key in new[] { "Text.Secondary", "Text.Muted", "Text.Heading",
                                            "Surface.Panel", "Surface.Banner.Danger", "Surface.Recording" })
                    Assert.IsType<Style>(theme[key]);

                window.Close();
            });
        }

        [Fact]
        public void Text_boxes_get_the_themed_edit_menu_not_the_builtin_light_one()
        {
            RunSta(() =>
            {
                var theme = LoadTheme();
                var window = new Window();
                window.Resources.MergedDictionaries.Add(theme);
                var text = new TextBox();
                var password = new PasswordBox();
                window.Content = new StackPanel { Children = { text, password } };

                foreach (Control box in new Control[] { text, password })
                {
                    // The exact ContextMenu type: the implicit ContextMenu/MenuItem styles apply to it
                    // (WPF's built-in editor menu is a private subclass that they do not reach).
                    var menu = Assert.IsType<ContextMenu>(box.ContextMenu);
                    Assert.Same(theme["ContextMenu.TextEdit"], menu);
                }
                var commands = text.ContextMenu.Items.OfType<MenuItem>().Select(m => m.Command).ToList();
                Assert.Equal(new System.Windows.Input.ICommand[]
                {
                    System.Windows.Input.ApplicationCommands.Cut, System.Windows.Input.ApplicationCommands.Copy,
                    System.Windows.Input.ApplicationCommands.Paste, System.Windows.Input.ApplicationCommands.SelectAll,
                }, commands);
                window.Close();
            });
        }

        [Fact]
        public void Context_menu_items_and_separators_use_the_theme_templates()
        {
            RunSta(() =>
            {
                var theme = LoadTheme();
                var item = new MenuItem { Header = "Copy", InputGestureText = "Ctrl+C" };
                var separator = new Separator();
                var menu = new ContextMenu();
                menu.Resources.MergedDictionaries.Add(theme);
                menu.Items.Add(item);
                menu.Items.Add(separator);

                menu.ApplyTemplate();
                menu.Measure(new Size(400, 400));
                menu.Arrange(new Rect(0, 0, 400, 400));

                Assert.Contains(menu.Template, ThemeTemplates((Style)theme[typeof(ContextMenu)]));
                Assert.Contains(item.Template, ThemeTemplates((Style)theme[typeof(MenuItem)]));
                // Inside a menu a Separator uses MenuItem.SeparatorStyleKey, not the implicit style.
                Assert.Contains(separator.Template, ThemeTemplates((Style)theme[MenuItem.SeparatorStyleKey]));
            });
        }
    }
}
