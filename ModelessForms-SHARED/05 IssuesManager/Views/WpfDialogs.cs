using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using ModelessForms.IssuesManager.Model;
using ModelessForms.IssuesManager.ViewModels;

namespace ModelessForms.IssuesManager.Views
{
    /// <summary>
    /// <see cref="IDialogs"/> with themed modal windows. The dialogs are owned by the
    /// Issue Tracker window when it is shown, and by the Revit main window before that
    /// (for example the first-run settings dialog).
    /// </summary>
    public sealed class WpfDialogs : IDialogs
    {
        private readonly IntPtr _revitWindow;

        public WpfDialogs(IntPtr revitWindow) { _revitWindow = revitWindow; }

        /// <summary>Set by the launcher when the Issue Tracker window exists.</summary>
        public Window? OwnerWindow { get; set; }

        public bool AskTexts(string title, IReadOnlyList<TextField> fields) =>
            DialogOwner.ShowModal(new TextFieldsDialog(title, fields), OwnerWindow, _revitWindow);

        public bool ConfirmDanger(string title, string message, string confirmText) =>
            MessageDialog.ConfirmDanger(OwnerWindow, title, message, confirmText, _revitWindow);

        public bool Confirm(string title, string message) =>
            MessageDialog.Confirm(OwnerWindow, title, message, _revitWindow);

        public void ShowError(string title, string message) =>
            MessageDialog.ShowError(OwnerWindow, title, message, _revitWindow);

        public void ShowInfo(string title, string message) =>
            MessageDialog.ShowInfo(OwnerWindow, title, message, _revitWindow);

        public IssueTrackerSettings? EditSettings(IssueTrackerSettings current, IReadOnlyList<string> microphones)
        {
            var dialog = new SettingsDialog(current, microphones);
            return DialogOwner.ShowModal(dialog, OwnerWindow, _revitWindow) ? dialog.Result : null;
        }

        public string? AskPdfPath(string suggestedFileName)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Export issues to PDF",
                FileName = suggestedFileName,
                DefaultExt = ".pdf",
                Filter = "PDF files (*.pdf)|*.pdf",
                AddExtension = true,
                OverwritePrompt = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            };
            bool ok = OwnerWindow != null && OwnerWindow.IsVisible
                ? dialog.ShowDialog(OwnerWindow) == true
                : dialog.ShowDialog() == true;
            return ok ? Path.GetFullPath(dialog.FileName) : null;
        }
    }
}
