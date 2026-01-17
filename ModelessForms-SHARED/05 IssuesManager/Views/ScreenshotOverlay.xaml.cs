using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ModelessForms.IssuesManager.Services;

namespace ModelessForms.IssuesManager.Views
{
    public class ScreenshotCapturedEventArgs : EventArgs
    {
        public byte[] ImageData { get; set; }
    }

    public partial class ScreenshotOverlay : Window
    {
        private Point _startPoint;
        private bool _isSelecting;
        private ScreenshotService _screenshotService;

        private Canvas OverlayCanvas;
        private Rectangle SelectionRectangle;
        private Border DimensionsLabel;
        private TextBlock DimensionsText;
        private TextBlock HintText;

        public event EventHandler<ScreenshotCapturedEventArgs> ScreenshotCaptured;

        public ScreenshotOverlay()
        {
            _screenshotService = new ScreenshotService();
            BuildUI();
        }

        private void BuildUI()
        {
            Title = "Screenshot";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
            Topmost = true;
            WindowState = WindowState.Maximized;
            Cursor = Cursors.Cross;

            KeyDown += Window_KeyDown;
            MouseLeftButtonDown += Window_MouseLeftButtonDown;
            MouseMove += Window_MouseMove;
            MouseLeftButtonUp += Window_MouseLeftButtonUp;

            var mainGrid = new Grid();

            OverlayCanvas = new Canvas
            {
                Background = new SolidColorBrush(Color.FromArgb(0x80, 0, 0, 0))
            };

            SelectionRectangle = new Rectangle
            {
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC")),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(0x20, 0, 0x7A, 0xCC)),
                Visibility = Visibility.Collapsed
            };
            OverlayCanvas.Children.Add(SelectionRectangle);

            DimensionsLabel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x1E, 0x1E, 0x1E)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 3, 6, 3),
                CornerRadius = new CornerRadius(2),
                Visibility = Visibility.Collapsed
            };
            DimensionsText = new TextBlock
            {
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12
            };
            DimensionsLabel.Child = DimensionsText;
            OverlayCanvas.Children.Add(DimensionsLabel);

            mainGrid.Children.Add(OverlayCanvas);

            HintText = new TextBlock
            {
                Text = "Click and drag to select region. Press ESC to cancel.",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 50, 0, 0),
                Foreground = Brushes.White,
                FontSize = 16
            };
            HintText.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                ShadowDepth = 2,
                BlurRadius = 4
            };
            mainGrid.Children.Add(HintText);

            Content = mainGrid;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                RaiseScreenshotCaptured(null);
                Close();
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(OverlayCanvas);
            _isSelecting = true;

            Canvas.SetLeft(SelectionRectangle, _startPoint.X);
            Canvas.SetTop(SelectionRectangle, _startPoint.Y);
            SelectionRectangle.Width = 0;
            SelectionRectangle.Height = 0;
            SelectionRectangle.Visibility = Visibility.Visible;

            HintText.Visibility = Visibility.Collapsed;
            OverlayCanvas.CaptureMouse();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelecting) return;

            var currentPoint = e.GetPosition(OverlayCanvas);

            var x = Math.Min(_startPoint.X, currentPoint.X);
            var y = Math.Min(_startPoint.Y, currentPoint.Y);
            var width = Math.Abs(currentPoint.X - _startPoint.X);
            var height = Math.Abs(currentPoint.Y - _startPoint.Y);

            Canvas.SetLeft(SelectionRectangle, x);
            Canvas.SetTop(SelectionRectangle, y);
            SelectionRectangle.Width = width;
            SelectionRectangle.Height = height;

            DimensionsText.Text = $"{(int)width} x {(int)height}";
            Canvas.SetLeft(DimensionsLabel, x);
            Canvas.SetTop(DimensionsLabel, y - 30);
            DimensionsLabel.Visibility = width > 10 && height > 10 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isSelecting) return;

            _isSelecting = false;
            OverlayCanvas.ReleaseMouseCapture();

            var width = (int)SelectionRectangle.Width;
            var height = (int)SelectionRectangle.Height;

            if (width < 10 || height < 10)
            {
                RaiseScreenshotCaptured(null);
                Close();
                return;
            }

            var screenX = (int)(Left + Canvas.GetLeft(SelectionRectangle));
            var screenY = (int)(Top + Canvas.GetTop(SelectionRectangle));

            Hide();

            System.Threading.Thread.Sleep(100);

            var imageData = _screenshotService.CaptureRegion(screenX, screenY, width, height);
            RaiseScreenshotCaptured(imageData);
            Close();
        }

        private void RaiseScreenshotCaptured(byte[] data)
        {
            ScreenshotCaptured?.Invoke(this, new ScreenshotCapturedEventArgs { ImageData = data });
        }
    }
}
