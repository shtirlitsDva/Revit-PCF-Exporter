using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using ModelessForms.IssuesManager.ViewModels;
using Forms = System.Windows.Forms;

namespace ModelessForms.IssuesManager.Views
{
    /// <summary>
    /// <see cref="IScreenCapture"/>: hides the Issue Tracker window, shows one
    /// <see cref="CaptureOverlay"/> on each monitor, and captures the selected region
    /// as PNG after the overlays are gone from the screen. The Issue Tracker window
    /// is shown again in all cases (finally block).
    /// </summary>
    public sealed class ScreenCapture : IScreenCapture
    {
        /// <summary>The window to hide during the capture. Set by the launcher.</summary>
        public Window? WindowToHide { get; set; }

        public async Task<byte[]?> CaptureRegionAsync()
        {
            Window? hidden = WindowToHide != null && WindowToHide.IsVisible ? WindowToHide : null;
            var overlays = new List<CaptureOverlay>();
            try
            {
                if (hidden != null)
                {
                    NativeScreen.DisableTransitions(new WindowInteropHelper(hidden).Handle);
                    hidden.Hide();
                }

                // Asynchronous continuations: the overlays are closed after their mouse/key handler returns.
                var selection = new TaskCompletionSource<Int32Rect?>(TaskCreationOptions.RunContinuationsAsynchronously);
                foreach (Forms.Screen screen in Forms.Screen.AllScreens)
                {
                    var b = screen.Bounds; // screen pixels, same space as PointToScreen and CopyFromScreen
                    var overlay = new CaptureOverlay(new Int32Rect(b.X, b.Y, b.Width, b.Height));
                    overlay.Finished += r => selection.TrySetResult(r);
                    overlays.Add(overlay);
                    overlay.Show();
                }
                ActivateOverlayUnderCursor(overlays);

                Int32Rect? region = await selection.Task;
                CloseAll(overlays);
                if (region == null) return null;

                await WaitUntilScreenIsRepainted();
                return CapturePng(region.Value);
            }
            finally
            {
                CloseAll(overlays);
                // A window that was closed meanwhile (Revit exit) has no source and cannot be shown.
                if (hidden != null && PresentationSource.FromVisual(hidden) != null)
                {
                    hidden.Show();
                    hidden.Activate();
                }
            }
        }

        private static void ActivateOverlayUnderCursor(List<CaptureOverlay> overlays)
        {
            System.Drawing.Point cursor = Forms.Cursor.Position;
            Forms.Screen[] screens = Forms.Screen.AllScreens;
            for (int i = 0; i < overlays.Count && i < screens.Length; i++)
            {
                if (!screens[i].Bounds.Contains(cursor)) continue;
                overlays[i].Activate();
                return;
            }
        }

        private static void CloseAll(List<CaptureOverlay> overlays)
        {
            foreach (CaptureOverlay overlay in overlays) overlay.Close(); // Close on a closed window does nothing
            overlays.Clear();
        }

        /// <summary>
        /// Lets WPF process the closed overlays, then waits for the desktop compositor
        /// to present the next frame, so the overlays are not in the captured pixels.
        /// </summary>
        private static async Task WaitUntilScreenIsRepainted()
        {
            await Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            NativeScreen.FlushCompositor();
            NativeScreen.FlushCompositor(); // the frame that was in progress, then the frame without the overlays
        }

        private static byte[] CapturePng(Int32Rect region)
        {
            using var bitmap = new Bitmap(region.Width, region.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(region.X, region.Y, 0, 0, new System.Drawing.Size(region.Width, region.Height));
            }
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }
    }

    /// <summary>The Win32 calls the screen capture needs.</summary>
    internal static class NativeScreen
    {
        private const int DWMWA_TRANSITIONS_FORCEDISABLED = 3;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOSIZE = 0x0001;

        /// <summary>Turns off the show/hide animation, so a hidden window is gone in the next frame.</summary>
        public static void DisableTransitions(IntPtr hwnd)
        {
            int disabled = 1;
            // Cosmetic: when DWM refuses, the window only fades out. The capture waits for
            // the compositor in all cases, so the result is not used.
            DwmSetWindowAttribute(hwnd, DWMWA_TRANSITIONS_FORCEDISABLED, ref disabled, sizeof(int));
        }

        /// <summary>
        /// Places the window exactly on the monitor, in screen pixels. It is moved first
        /// and sized second: the move can change the window DPI, and WPF then resizes the
        /// window for the new DPI. The second call sets the final size on that monitor.
        /// </summary>
        public static void PlaceOnMonitor(IntPtr hwnd, Int32Rect bounds)
        {
            if (!SetWindowPos(hwnd, IntPtr.Zero, bounds.X, bounds.Y, 0, 0, SWP_NOZORDER | SWP_NOACTIVATE | SWP_NOSIZE) ||
                !SetWindowPos(hwnd, IntPtr.Zero, bounds.X, bounds.Y, bounds.Width, bounds.Height, SWP_NOZORDER | SWP_NOACTIVATE))
            {
                throw new InvalidOperationException(
                    "Could not place the capture overlay on the monitor (Win32 error " + Marshal.GetLastWin32Error() + ").");
            }
        }

        /// <summary>Waits until the desktop compositor has presented the next frame.</summary>
        public static void FlushCompositor()
        {
            int hr = DwmFlush();
            if (hr != 0) Marshal.ThrowExceptionForHR(hr);
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("dwmapi.dll")]
        private static extern int DwmFlush();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);
    }
}
