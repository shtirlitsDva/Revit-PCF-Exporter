using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
// These three type names exist in both WPF and the Revit API; pin them to WPF.
using Grid = System.Windows.Controls.Grid;
using TextBox = System.Windows.Controls.TextBox;
using Color = System.Windows.Media.Color;

namespace MEPUtils.Element3DRotation
{
    /// <summary>
    /// Modeless WPF window for rotation angle input. Built entirely in code (no
    /// XAML) because this shared project is compiled into both SDK-style (.NET 8)
    /// and legacy (.NET Framework 4.8) Revit projects, none of which run the XAML
    /// build pipeline. A faithful reproduction of pyRevitMEP's RotateOptions.xaml.
    /// </summary>
    public class RotateOptionsWindow : Window
    {
        // Logical resource names are pinned via <LogicalName> in the .projitems so
        // they are stable regardless of the consuming assembly's root namespace.
        private const string XyzImageResource = "MEPUtils.Element3DRotation.XYZ.png";
        private const string PlusMinusImageResource = "MEPUtils.Element3DRotation.PlusMinusRotation.png";

        private readonly ExternalEvent _externalEvent;
        private readonly RotateRequestHandler _handler;
        private readonly ForgeTypeId _angleUnit;

        private readonly TextBox _xAxis = MakeAngleBox();
        private readonly TextBox _yAxis = MakeAngleBox();
        private readonly TextBox _zAxis = MakeAngleBox();
        private readonly TextBox _rotationAngle = MakeAngleBox();
        private readonly TextBlock _warning = new TextBlock
        {
            Foreground = Brushes.Red,
            TextWrapping = TextWrapping.WrapWithOverflow,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        public RotateOptionsWindow(UIApplication uiapp, ExternalEvent externalEvent, RotateRequestHandler handler)
        {
            _externalEvent = externalEvent;
            _handler = handler;
            _angleUnit = uiapp.ActiveUIDocument.Document
                .GetUnits().GetFormatOptions(SpecTypeId.Angle).GetUnitTypeId();

            Title = "Set rotation angle:";
            Width = 420;
            Height = 415;
            ShowInTaskbar = false;
            Topmost = true;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = 0;
            Top = 180;
            ResizeMode = ResizeMode.NoResize;

            Content = BuildLayout();
        }

        private UIElement BuildLayout()
        {
            var root = new StackPanel { Margin = new Thickness(20) };

            var columns = new DockPanel();
            columns.Children.Add(BuildAroundItselfGroup());
            columns.Children.Add(BuildAroundAxisGroup());

            root.Children.Add(columns);
            root.Children.Add(_warning);
            return root;
        }

        private GroupBox BuildAroundItselfGroup()
        {
            var stack = new StackPanel { Margin = new Thickness(10) };

            stack.Children.Add(new Image
            {
                Source = LoadImage(XyzImageResource),
                Width = 100,
                Margin = new Thickness(0, 10, 0, 10)
            });

            var grid = new Grid();
            for (int i = 0; i < 3; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });

            AddAxisLabel(grid, "X Axis", 0, Color.FromRgb(0xFF, 0x00, 0x00));
            AddAxisLabel(grid, "Y Axis", 1, Color.FromRgb(0x00, 0xFF, 0x00));
            AddAxisLabel(grid, "Z Axis", 2, Color.FromRgb(0x00, 0x00, 0xFF));

            AddAxisBox(grid, _xAxis, 0);
            AddAxisBox(grid, _yAxis, 1);
            AddAxisBox(grid, _zAxis, 2);

            stack.Children.Add(grid);

            var go = new Button { Content = "Go", Height = 30, Margin = new Thickness(10, 10, 10, 10) };
            go.Click += AroundItselfClick;
            stack.Children.Add(go);

            return new GroupBox
            {
                Header = "Rotate on itself:",
                Margin = new Thickness(0, 0, 10, 0),
                Content = stack
            };
        }

        private GroupBox BuildAroundAxisGroup()
        {
            var stack = new StackPanel { Margin = new Thickness(10) };

            stack.Children.Add(new Image
            {
                Source = LoadImage(PlusMinusImageResource),
                Width = 100,
                Margin = new Thickness(0, 10, 0, 10)
            });

            stack.Children.Add(new TextBlock
            {
                Text = "Rotation angle",
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            _rotationAngle.Margin = new Thickness(0, 20, 0, 0);
            stack.Children.Add(_rotationAngle);

            var go = new Button
            {
                Content = "Select axis and rotate",
                Height = 30,
                Margin = new Thickness(10, 10, 10, 10)
            };
            go.Click += AroundAxisClick;
            stack.Children.Add(go);

            return new GroupBox
            {
                Header = "Rotate around selected axis:",
                Content = stack
            };
        }

        private void AroundItselfClick(object sender, RoutedEventArgs e)
        {
            if (!TryConvert(_xAxis.Text, out double x) ||
                !TryConvert(_yAxis.Text, out double y) ||
                !TryConvert(_zAxis.Text, out double z))
            {
                _warning.Text = "Incorrect angles, input format required '0.0'";
                return;
            }

            _warning.Text = "";
            _handler.Mode = RotateMode.AroundItself;
            _handler.AngleX = x;
            _handler.AngleY = y;
            _handler.AngleZ = z;
            _externalEvent.Raise();
        }

        private void AroundAxisClick(object sender, RoutedEventArgs e)
        {
            if (!TryConvert(_rotationAngle.Text, out double angle))
            {
                _warning.Text = "Incorrect angles, input format required '0.0'";
                return;
            }

            _warning.Text = "";
            _handler.Mode = RotateMode.AroundAxis;
            _handler.AngleAxis = angle;
            _externalEvent.Raise();
        }

        /// <summary>Parses an invariant-culture number and converts it to Revit internal units.</summary>
        private bool TryConvert(string text, out double internalUnits)
        {
            internalUnits = 0;
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                return false;
            internalUnits = UnitUtils.ConvertToInternalUnits(value, _angleUnit);
            return true;
        }

        private static void AddAxisLabel(Grid grid, string text, int column, Color color)
        {
            var label = new TextBlock
            {
                Text = text,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(color)
            };
            Grid.SetColumn(label, column);
            grid.Children.Add(label);
        }

        private static void AddAxisBox(Grid grid, TextBox box, int column)
        {
            box.Margin = new Thickness(0, 20, 0, 0);
            Grid.SetColumn(box, column);
            grid.Children.Add(box);
        }

        private static TextBox MakeAngleBox() => new TextBox
        {
            Text = "0",
            Width = 30,
            Height = 30,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };

        private static BitmapImage LoadImage(string logicalResourceName)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            using (Stream s = asm.GetManifestResourceStream(logicalResourceName))
            {
                if (s == null)
                    throw new InvalidOperationException(
                        $"Embedded image resource '{logicalResourceName}' not found in {asm.GetName().Name}.");

                var img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.StreamSource = s;
                img.EndInit();
                img.Freeze();
                return img;
            }
        }
    }
}
