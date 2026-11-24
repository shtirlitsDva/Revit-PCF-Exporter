using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ModelessForms.IssuesManager.Model;
using ModelessForms.IssuesManager.Storage;
using ModelessForms.IssuesManager.ViewModels;

namespace ModelessForms.Tests.IssuesManager
{
    internal sealed class FakeSettingsStore : ISettingsStore
    {
        public IssueTrackerSettings Stored = new IssueTrackerSettings();
        public int SaveCount;

        public IssueTrackerSettings Load() => Stored.Clone();

        public void Save(IssueTrackerSettings settings)
        {
            Stored = settings.Clone();
            SaveCount++;
        }
    }

    internal sealed class FakeRevit : IRevitBridge
    {
        public IReadOnlyList<string> Selection = Array.Empty<string>();
        public Exception? Error;
        public int MissingOnSelect;
        public IReadOnlyList<string>? LastSelected;

        public Task<IReadOnlyList<string>> GetSelectedElementUniqueIdsAsync() =>
            Error != null ? Task.FromException<IReadOnlyList<string>>(Error) : Task.FromResult(Selection);

        public Task<int> SelectElementsAsync(IReadOnlyList<string> uniqueIds)
        {
            LastSelected = uniqueIds;
            return Task.FromResult(MissingOnSelect);
        }
    }

    internal sealed class FakeCapture : IScreenCapture
    {
        public byte[]? Next = { 1, 2, 3 };
        public Task<byte[]?> CaptureRegionAsync() => Task.FromResult(Next);
    }

    internal sealed class FakeDictation : IDictation
    {
        public bool IsRecording { get; private set; }
        public string TextToReturn = "dictated text";
        public DictationOptions? LastOptions;
        public DictationException? StartError;
        public bool Disposed;

        public IReadOnlyList<string> GetMicrophoneNames() => new[] { "USB Mic" };

        public Task StartAsync(DictationOptions options)
        {
            if (StartError != null) return Task.FromException(StartError);
            LastOptions = options;
            IsRecording = true;
            return Task.CompletedTask;
        }

        public int StopCalls;
        /// <summary>When set, StopAsync waits for this task (a slow stop).</summary>
        public TaskCompletionSource<bool>? StopGate;

        public async Task<string> StopAsync()
        {
            StopCalls++;
            if (StopGate != null) await StopGate.Task;
            IsRecording = false;
            return TextToReturn;
        }

        public event Action<DictationException>? Faulted;

        public void RaiseFault(DictationException ex) => Faulted?.Invoke(ex);

        public void Dispose() => Disposed = true;
    }

    internal sealed class FakePdf : IPdfReport
    {
        public IssueCollection? Exported;
        public string? Path;
        public Exception? Error;

        public void Export(IssueCollection collection, Func<string, string> screenshotPath, string outputPath)
        {
            if (Error != null) throw Error;
            Exported = collection;
            Path = outputPath;
        }
    }

    /// <summary>Scripted dialog answers; records every error and info shown.</summary>
    internal sealed class FakeDialogs : IDialogs
    {
        public readonly Queue<string[]?> TextAnswers = new Queue<string[]?>();
        public bool ConfirmAnswer = true;
        public bool DangerAnswer = true;
        public Func<IssueTrackerSettings, IssueTrackerSettings?> SettingsAnswer = _ => null;
        public string? PdfPath;

        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Infos = new List<string>();
        public readonly List<string> Confirms = new List<string>();
        public int SettingsShown;
        public IReadOnlyList<TextField>? LastFields;

        public bool AskTexts(string title, IReadOnlyList<TextField> fields)
        {
            LastFields = fields;
            var answer = TextAnswers.Count > 0 ? TextAnswers.Dequeue() : null;
            if (answer == null) return false;
            for (int i = 0; i < fields.Count && i < answer.Length; i++) fields[i].Value = answer[i];
            return true;
        }

        public bool ConfirmDanger(string title, string message, string confirmText)
        {
            Confirms.Add(message);
            return DangerAnswer;
        }

        public bool Confirm(string title, string message)
        {
            Confirms.Add(message);
            return ConfirmAnswer;
        }

        public void ShowError(string title, string message) => Errors.Add(message);

        public void ShowInfo(string title, string message) => Infos.Add(message);

        public IssueTrackerSettings? EditSettings(IssueTrackerSettings current, IReadOnlyList<string> microphones)
        {
            SettingsShown++;
            return SettingsAnswer(current.Clone());
        }

        public string? AskPdfPath(string suggestedFileName) => PdfPath;
    }
}
