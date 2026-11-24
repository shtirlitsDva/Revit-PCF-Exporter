using System;
using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.UI;
using ModelessForms.IssuesManager.Dictation;
using ModelessForms.IssuesManager.Pdf;
using ModelessForms.IssuesManager.Revit;
using ModelessForms.IssuesManager.Storage;
using ModelessForms.IssuesManager.ViewModels;
using ModelessForms.IssuesManager.Views;

namespace ModelessForms.IssuesManager
{
    /// <summary>
    /// Composition root of the Issue Tracker: creates the services, the view model
    /// and the window, and keeps one window per Revit session.
    /// </summary>
    internal static class IssueTrackerLauncher
    {
        private static IssueTrackerWindow? _window;
        private static IssueTrackerViewModel? _viewModel;

        /// <summary>Shows the Issue Tracker. Call from an IExternalCommand (a Revit API context).</summary>
        public static void Show(UIApplication app)
        {
            if (_window != null)
            {
                if (_window.WindowState == WindowState.Minimized) _window.WindowState = WindowState.Normal;
                _window.Show();
                _window.Activate();
                return;
            }

            IntPtr revitWindow = app.MainWindowHandle;
            var dialogs = new WpfDialogs(revitWindow);
            var capture = new ScreenCapture();
            var bridge = new RevitBridge();
            var dictation = new AzureDictation();
            IssueTrackerWindow window;
            IssueTrackerViewModel viewModel;
            try
            {
                viewModel = new IssueTrackerViewModel(
                    SettingsStore.ForCurrentUser(),
                    folder => new IssueStore(folder),
                    bridge, capture, dictation, new PdfReport(), dialogs);

                // Before the window exists: the first-run settings dialog is owned by Revit.
                if (!viewModel.Initialize())
                {
                    bridge.Dispose();
                    dictation.Dispose();
                    return;
                }

                window = new IssueTrackerWindow(viewModel);
            }
            catch
            {
                bridge.Dispose();
                dictation.Dispose();
                throw;
            }

            // Owned by Revit (D7): stays above Revit, minimizes with it, no taskbar button.
            new WindowInteropHelper(window).Owner = revitWindow;
            dialogs.OwnerWindow = window;
            capture.WindowToHide = window;
            window.Closed += (s, e) =>
            {
                bridge.Dispose();
                // Normally ShutdownAsync disposed it already; not when the user chose "Close anyway"
                // after a failed shutdown. Dispose is idempotent.
                dictation.Dispose();
                _window = null;
                _viewModel = null;
            };

            _window = window;
            _viewModel = viewModel;
            window.Show();
        }

        /// <summary>
        /// Called when Revit shuts down. Saves first and synchronously, because Revit
        /// may end before an asynchronous close completes (for example while a
        /// dictation is stopping). Then closes the window the normal way.
        /// </summary>
        public static void CloseForShutdown()
        {
            if (_window == null || _viewModel == null) return;
            _viewModel.FlushPendingSave();
            _window.CloseBecauseRevitExits();
        }
    }
}
