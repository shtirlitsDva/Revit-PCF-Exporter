using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace NorsynApps
{
    // "All Commands": every IExternalCommand of every scanned addin —
    // including commands without a [DevReloadButton] attribute — grouped
    // per addin in a list, runnable from release installs. The window is
    // modal and the chosen command executes after it closes, still inside
    // THIS command's Execute, so the forwarded ExternalCommandData is live
    // and the API context is valid.
    [Transaction(TransactionMode.Manual)]
    public sealed class CommandBrowserCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var window = new CommandBrowserWindow(NorsynAppsApp.Addins);
            new System.Windows.Interop.WindowInteropHelper(window)
            {
                Owner = commandData.Application.MainWindowHandle,
            };
            if (window.ShowDialog() != true || window.Selected == null)
                return Result.Cancelled;

            (AddinAssembly addin, string className) = window.Selected.Value;
            Type type = addin.Assembly.GetType(className)
                ?? throw new InvalidOperationException(
                    $"type {className} not found in {addin.DisplayName}");
            if (Activator.CreateInstance(type) is not IExternalCommand command)
                throw new InvalidOperationException(
                    $"{className} does not implement IExternalCommand");

            return command.Execute(commandData, ref message, elements);
        }
    }

    // Code-only WPF (no XAML) so the window compiles identically from the
    // shared project across all per-year targets.
    internal sealed class CommandBrowserWindow : Window
    {
        private readonly ListView _list;

        public (AddinAssembly Addin, string FullClassName)? Selected { get; private set; }

        public CommandBrowserWindow(IReadOnlyList<AddinAssembly> addins)
        {
            Title = "Norsyn — All Commands";
            Width = 520;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var rows = addins
                .SelectMany(a => a.Commands.Select(c => new CommandRow(
                    a, c.FullClassName, c.DisplayName,
                    a.Buttons.Any(b => b.FullClassName == c.FullClassName))))
                .OrderBy(r => r.AddinName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _list = new ListView { ItemsSource = rows, Margin = new Thickness(8) };
            var view = new GridView();
            view.Columns.Add(Column("Addin", nameof(CommandRow.AddinName), 150));
            view.Columns.Add(Column("Command", nameof(CommandRow.DisplayName), 180));
            view.Columns.Add(Column("On ribbon", nameof(CommandRow.OnRibbon), 70));
            _list.View = view;
            // Row double-clicks only — a double-click on the header or
            // scrollbar must not run the selected command.
            _list.MouseDoubleClick += (_, e) =>
            {
                if (e.OriginalSource is FrameworkElement fe &&
                    fe.DataContext is CommandRow)
                    Accept();
            };

            var run = new Button
            {
                Content = "Run",
                Width = 90,
                Margin = new Thickness(8),
                IsDefault = true,
            };
            run.Click += (_, _) => Accept();

            var cancel = new Button
            {
                Content = "Cancel",
                Width = 90,
                Margin = new Thickness(0, 8, 8, 8),
                IsCancel = true,
            };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            buttons.Children.Add(run);
            buttons.Children.Add(cancel);

            var root = new DockPanel();
            DockPanel.SetDock(buttons, Dock.Bottom);
            root.Children.Add(buttons);
            if (NorsynAppsApp.Failures.Count > 0)
            {
                var failures = new TextBlock
                {
                    Text = "Load problems:\n" + string.Join("\n", NorsynAppsApp.Failures),
                    Margin = new Thickness(8, 0, 8, 0),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = System.Windows.Media.Brushes.DarkOrange,
                };
                DockPanel.SetDock(failures, Dock.Bottom);
                root.Children.Add(failures);
            }
            root.Children.Add(_list);
            Content = root;
        }

        private static GridViewColumn Column(string header, string property, double width)
            => new()
            {
                Header = header,
                Width = width,
                DisplayMemberBinding = new System.Windows.Data.Binding(property),
            };

        private void Accept()
        {
            if (_list.SelectedItem is not CommandRow row) return;
            Selected = (row.Addin, row.FullClassName);
            DialogResult = true;
        }

        private sealed class CommandRow
        {
            public CommandRow(AddinAssembly addin, string fullClassName,
                string displayName, bool onRibbon)
            {
                Addin = addin;
                FullClassName = fullClassName;
                DisplayName = displayName;
                OnRibbon = onRibbon ? "yes" : "";
            }

            public AddinAssembly Addin { get; }
            public string FullClassName { get; }
            public string DisplayName { get; }
            public string AddinName => Addin.DisplayName;
            public string OnRibbon { get; }
        }
    }
}
