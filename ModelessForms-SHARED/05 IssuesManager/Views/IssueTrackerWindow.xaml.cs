using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using ModelessForms.IssuesManager.ViewModels;
using Shared.Theme;

namespace ModelessForms.IssuesManager.Views
{
    /// <summary>The Issue Tracker window. All behaviour is in <see cref="IssueTrackerViewModel"/>.</summary>
    public partial class IssueTrackerWindow : Window
    {
        private readonly IssueTrackerViewModel _viewModel;
        private bool _shutdownDone;
        private Task<bool>? _shutdown;
        private bool _revitIsExiting;

        public IssueTrackerWindow(IssueTrackerViewModel viewModel)
        {
            _viewModel = viewModel;
            ThemeManager.Apply(this); // before InitializeComponent: the view looks up theme keys
            InitializeComponent();
            DataContext = viewModel;
            Closing += OnClosing;
        }

        /// <summary>Closes the window because Revit exits: a failed save is reported, not asked about.</summary>
        public void CloseBecauseRevitExits()
        {
            _revitIsExiting = true;
            Close();
        }

        /// <summary>
        /// The view model saves and stops dictation before the window closes, and can
        /// keep the window open when unsaved changes would be lost. WPF does not allow
        /// Close() while a window is closing, so an asynchronous shutdown cancels this
        /// close and closes again when it is done. A second close request while the
        /// shutdown runs waits for the same shutdown.
        /// Nothing may throw out of this handler: it runs on the Revit UI thread.
        /// </summary>
        private void OnClosing(object? sender, CancelEventArgs e)
        {
            if (_shutdownDone) return;
            e.Cancel = true;
            if (_shutdown != null) return; // already shutting down

            Task<bool> shutdown;
            try
            {
                shutdown = _viewModel.ShutdownAsync(_revitIsExiting);
            }
            catch (Exception ex)
            {
                shutdown = Task.FromException<bool>(ex);
            }

            if (shutdown.IsCompletedSuccessfully)
            {
                // Finished synchronously: let this close go on (Close() is not allowed here).
                _shutdownDone = shutdown.Result;
                e.Cancel = !_shutdownDone;
                return;
            }

            _shutdown = shutdown;
            CloseWhenDone(shutdown);
        }

        private async void CloseWhenDone(Task<bool> shutdown)
        {
            // Continue after the Closing handler has returned: Close() is not allowed inside it.
            await Task.Yield();
            bool close;
            try
            {
                close = await shutdown;
            }
            catch (Exception ex)
            {
                // The data is not known to be saved: the user decides. Keeping the window
                // open without a choice could make it impossible to close.
                try
                {
                    close = MessageDialog.ConfirmDanger(this, "Issue Tracker",
                        "Closing failed:\n" + ex.Message + "\n\nClose anyway? Changes that are not saved are lost.",
                        "Close anyway");
                }
                catch (Exception dialogError)
                {
                    ShowLastResortError(dialogError);
                    close = false;
                }
            }
            finally
            {
                _shutdown = null;
            }

            if (!close) return;
            _shutdownDone = true;
            try
            {
                Close();
            }
            catch (Exception ex)
            {
                // async void: an exception here would reach the Revit dispatcher and can end Revit.
                ShowLastResortError(ex);
            }
        }

        /// <summary>
        /// For a failure inside the close path itself, where the themed dialog may be the
        /// part that failed: the plain system message box (fallback F8).
        /// </summary>
        private static void ShowLastResortError(Exception ex) =>
            System.Windows.MessageBox.Show("The Issue Tracker could not close:\n" + ex.Message,
                "Issue Tracker", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
