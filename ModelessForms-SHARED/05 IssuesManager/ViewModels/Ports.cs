using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ModelessForms.IssuesManager.Model;

namespace ModelessForms.IssuesManager.ViewModels
{
    // The services the Issue Tracker view model needs. They hold no WPF or Revit
    // types, so the view model can be tested with fakes. Every failure is an
    // exception with a user-readable message.

    /// <summary>Calls into the Revit API through an ExternalEvent.</summary>
    public interface IRevitBridge
    {
        /// <summary>UniqueIds of the elements selected in the active Revit document.</summary>
        /// <exception cref="System.InvalidOperationException">No document is open, or Revit refused the request.</exception>
        Task<IReadOnlyList<string>> GetSelectedElementUniqueIdsAsync();

        /// <summary>Selects and zooms to the elements. Returns how many were not found in the active document.</summary>
        Task<int> SelectElementsAsync(IReadOnlyList<string> uniqueIds);
    }

    /// <summary>Lets the user select a screen region and captures it.</summary>
    public interface IScreenCapture
    {
        /// <summary>PNG bytes of the selected region, or null when the user cancelled.</summary>
        Task<byte[]?> CaptureRegionAsync();
    }

    public sealed class DictationOptions
    {
        public string AzureKey { get; init; } = string.Empty;
        public string AzureRegion { get; init; } = string.Empty;
        public string MicrophoneName { get; init; } = string.Empty;
        public string Language { get; init; } = string.Empty;
    }

    /// <summary>A dictation failure. <see cref="PartialText"/> holds text recognized before the failure.</summary>
    public sealed class DictationException : Exception
    {
        public DictationException(string message, Exception? inner = null, string partialText = "")
            : base(message, inner) { PartialText = partialText; }

        public string PartialText { get; }
    }

    /// <summary>Speech to text.</summary>
    public interface IDictation : IDisposable
    {
        bool IsRecording { get; }

        IReadOnlyList<string> GetMicrophoneNames();

        /// <exception cref="DictationException">The recording could not start.</exception>
        Task StartAsync(DictationOptions options);

        /// <summary>Stops the recording and returns the recognized text.</summary>
        /// <exception cref="DictationException">The recording did not stop correctly; see PartialText.</exception>
        Task<string> StopAsync();

        /// <summary>
        /// Raised on the UI thread when a running recording ends by itself because of
        /// an error (network, key, microphone). Call <see cref="StopAsync"/> afterwards
        /// to collect the text recognized before the error.
        /// </summary>
        event Action<DictationException>? Faulted;
    }

    /// <summary>Writes a collection to a PDF file. Runs on a background thread.</summary>
    public interface IPdfReport
    {
        /// <param name="collection">The collection to export.</param>
        /// <param name="screenshotPath">Maps a screenshot file name to its full path.</param>
        /// <param name="outputPath">The PDF file to write. An existing file is replaced.</param>
        void Export(IssueCollection collection, Func<string, string> screenshotPath, string outputPath);
    }

    /// <summary>One labelled text field in <see cref="IDialogs.AskTexts"/>.</summary>
    public sealed class TextField
    {
        public TextField(string label, string value) { Label = label; Value = value; }
        public string Label { get; }
        public string Value { get; set; }
    }

    /// <summary>Modal dialogs, owned by the Issue Tracker window.</summary>
    public interface IDialogs
    {
        /// <summary>Asks for one or more texts. Returns false when cancelled. The values are written back into the fields.</summary>
        bool AskTexts(string title, IReadOnlyList<TextField> fields);

        /// <summary>Asks to confirm a destructive action.</summary>
        bool ConfirmDanger(string title, string message, string confirmText);

        /// <summary>Asks a yes/no question that is not destructive.</summary>
        bool Confirm(string title, string message);

        void ShowError(string title, string message);

        void ShowInfo(string title, string message);

        /// <summary>Edits the settings. Returns the new settings, or null when cancelled.</summary>
        IssueTrackerSettings? EditSettings(IssueTrackerSettings current, IReadOnlyList<string> microphones);

        /// <summary>Asks where to save the PDF. Returns null when cancelled.</summary>
        string? AskPdfPath(string suggestedFileName);
    }
}
