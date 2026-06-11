using System;
using System.Windows;
using System.Windows.Interop;

using Autodesk.Revit.UI;

using PcfExporter.Configuration;
using PcfExporter.Context;
using PcfExporter.UI;
using PcfExporter.UI.ViewModels;
using PcfExporter.UI.Views;

namespace PcfExporter.App
{
    /// <summary>
    /// Owns the single modeless exporter window and its ExternalEvent executor.
    /// Created lazily on first command invocation (ExternalEvent.Create needs a
    /// valid API context); subsequent invocations re-activate the open window.
    /// </summary>
    internal static class PcfWindowController
    {
        private static MainWindow _window;
        private static RevitExecutor _executor;

        public static void ShowOrActivate(UIApplication uiApp)
        {
            if (_window != null)
            {
                //Window already open: bring it to front.
                if (_window.WindowState == WindowState.Minimized)
                    _window.WindowState = WindowState.Normal;
                _window.Activate();
                return;
            }

            _executor = new RevitExecutor();

            var dialogs = new DialogService(() => _window);
            var viewModel = new MainViewModel(_executor, new FileConfigurationStore(), dialogs);

            _window = new MainWindow(viewModel);

            //Parent the modeless window to Revit's main window so it stays on top of
            //Revit (and minimizes with it) without being topmost globally.
            var helper = new WindowInteropHelper(_window)
            {
                Owner = uiApp.MainWindowHandle
            };

            _window.Closed += (s, e) =>
            {
                _window = null;
                _executor.Dispose();
                _executor = null;
            };

            _window.Show();
        }

        /// <summary>
        /// Terminate-time cleanup (App.OnShutdown): closing the window runs
        /// its Closed handler, which disposes the ExternalEvent executor and
        /// clears the static keepers. Must run on the UI thread — DevReload's
        /// teardown executes in Revit API context, which is that thread.
        /// </summary>
        public static void Shutdown()
        {
            _window?.Close();
        }
    }
}
