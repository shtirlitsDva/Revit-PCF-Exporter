using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Shared.Theme;

namespace ModelessForms.IssuesManager.Views
{
    /// <summary>
    /// A dimmed window that covers exactly one monitor. The user drags a rectangle on
    /// it. <see cref="Finished"/> reports the rectangle in screen pixels, or null when
    /// the user cancelled (Esc or right click). Used only by <see cref="ScreenCapture"/>.
    /// </summary>
    public partial class CaptureOverlay : Window
    {
        /// <summary>Selections smaller than this (in screen pixels) are taken as a click and ignored.</summary>
        private const int MinimumSize = 10;

        private readonly Int32Rect _monitorBounds;
        private Point _start;
        private bool _dragging;

        internal CaptureOverlay(Int32Rect monitorBounds)
        {
            _monitorBounds = monitorBounds;
            ThemeManager.Apply(this);
            InitializeComponent(); // the XAML replaces the Window style with Window.CaptureOverlay
            SourceInitialized += OnSourceInitialized;
            MouseEnter += (s, e) => Activate(); // the overlay under the mouse gets the keyboard, so Esc works
            KeyDown += OnKeyDown;
            MouseLeftButtonDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnMouseUp;
            MouseRightButtonUp += (s, e) => Finish(null);
            LostMouseCapture += (s, e) => ResetSelection();
        }

        /// <summary>Raised once, with the selected rectangle in screen pixels, or null when cancelled.</summary>
        internal event Action<Int32Rect?>? Finished;

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            NativeScreen.DisableTransitions(hwnd); // hide at once, so the overlay is not in the capture
            NativeScreen.PlaceOnMonitor(hwnd, _monitorBounds);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;
            e.Handled = true;
            Finish(null);
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            _start = e.GetPosition(Surface);
            _dragging = CaptureMouse();
            if (!_dragging) return;
            UpdateSelection(_start);
            Selection.Visibility = Visibility.Visible;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_dragging) UpdateSelection(e.GetPosition(Surface));
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_dragging) return;
            Point end = Clamp(e.GetPosition(Surface));
            _dragging = false;
            ReleaseMouseCapture();

            // PointToScreen on both corners gives screen pixels, also when the
            // monitor's DPI differs from the DPI of the other monitors.
            Point a = Surface.PointToScreen(_start);
            Point b = Surface.PointToScreen(end);
            int left = (int)Math.Round(Math.Min(a.X, b.X));
            int top = (int)Math.Round(Math.Min(a.Y, b.Y));
            int width = (int)Math.Round(Math.Abs(a.X - b.X));
            int height = (int)Math.Round(Math.Abs(a.Y - b.Y));

            if (width < MinimumSize || height < MinimumSize)
            {
                ResetSelection(); // a click, not a drag: let the user drag again
                return;
            }
            Finish(new Int32Rect(left, top, width, height));
        }

        private void UpdateSelection(Point current)
        {
            current = Clamp(current);
            Canvas.SetLeft(Selection, Math.Min(_start.X, current.X));
            Canvas.SetTop(Selection, Math.Min(_start.Y, current.Y));
            Selection.Width = Math.Abs(current.X - _start.X);
            Selection.Height = Math.Abs(current.Y - _start.Y);
        }

        private Point Clamp(Point p) => new Point(
            Math.Max(0, Math.Min(p.X, Surface.ActualWidth)),
            Math.Max(0, Math.Min(p.Y, Surface.ActualHeight)));

        private void ResetSelection()
        {
            _dragging = false;
            Selection.Visibility = Visibility.Collapsed;
        }

        private void Finish(Int32Rect? result)
        {
            Action<Int32Rect?>? handler = Finished;
            Finished = null; // raise once
            handler?.Invoke(result);
        }
    }
}
