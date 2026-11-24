using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModelessForms.IssuesManager.Model;
using ModelessForms.IssuesManager.Storage;

namespace ModelessForms.IssuesManager.ViewModels
{
    /// <summary>
    /// The Issue Tracker window. Owns the open collection and saves every change
    /// automatically: structural changes (new/delete issue, screenshots, properties)
    /// at once, description typing after <see cref="SaveDelay"/>, and always before
    /// switching issue or collection, exporting or closing.
    /// A failed save keeps the data in memory and shows <see cref="SaveError"/>.
    /// </summary>
    public sealed partial class IssueTrackerViewModel : ObservableObject
    {
        private const string Title = "Issue Tracker";

        private readonly ISettingsStore _settingsStore;
        private readonly Func<string, IIssueStore> _openStore;
        private readonly IRevitBridge _revit;
        private readonly IScreenCapture _capture;
        private readonly IDictation _dictation;
        private readonly IPdfReport _pdf;
        private readonly IDialogs _dialogs;

        private IssueTrackerSettings _settings = new IssueTrackerSettings();
        private IIssueStore? _store;
        private IssueCollection? _collection;
        private bool _dirty;
        private int _editVersion;
        private bool _restoringSelection;
        private IssueViewModel? _dictationTarget;
        private TaskCompletionSource<bool>? _stopping; // set while a dictation stop runs
        private Task _background = Task.CompletedTask;

        public IssueTrackerViewModel(
            ISettingsStore settingsStore,
            Func<string, IIssueStore> openStore,
            IRevitBridge revit,
            IScreenCapture capture,
            IDictation dictation,
            IPdfReport pdf,
            IDialogs dialogs)
        {
            _settingsStore = settingsStore;
            _openStore = openStore;
            _revit = revit;
            _capture = capture;
            _dictation = dictation;
            _pdf = pdf;
            _dialogs = dialogs;
            _dictation.Faulted += OnDictationFaulted;
        }

        /// <summary>How long after the last keystroke a description is saved.</summary>
        public TimeSpan SaveDelay { get; set; } = TimeSpan.FromSeconds(1);

        public ObservableCollection<string> CollectionNames { get; } = new ObservableCollection<string>();

        /// <summary>Issues of the open collection, newest first.</summary>
        public ObservableCollection<IssueViewModel> Issues { get; } = new ObservableCollection<IssueViewModel>();

        [ObservableProperty] private string? _selectedCollectionName;
        [ObservableProperty] private IssueViewModel? _selectedIssue;
        [ObservableProperty] private ScreenshotViewModel? _selectedScreenshot;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private bool _isRecording;
        [ObservableProperty] private string _statusText = string.Empty;
        [ObservableProperty] private string? _saveError;

        public bool HasCollection => _collection != null;

        public bool HasSelectedIssue => SelectedIssue != null;

        public string DictationLabel => IsRecording ? "Stop dictation" : "Dictate";

        /// <summary>The collection can be switched (not while busy or recording).</summary>
        public bool CanSwitchCollection => !IsBusy && !IsRecording;

        public string CollectionInfo
        {
            get
            {
                if (_collection == null) return string.Empty;
                var parts = new List<string>();
                if (_collection.ProjectName.Length > 0) parts.Add("Project: " + _collection.ProjectName);
                if (_collection.AuthorName.Length > 0) parts.Add("Author: " + _collection.AuthorName);
                parts.Add($"{_collection.Issues.Count} issue(s)");
                return string.Join("   ·   ", parts);
            }
        }

        public string StorageFolder => _settings.StorageFolder;

        // =====================================================================
        // Start-up and shut-down
        // =====================================================================

        /// <summary>
        /// Loads the settings and opens the storage folder. Asks for settings when
        /// no folder is set or the folder cannot be opened. Returns false when the
        /// user cancels; the window must then close.
        /// </summary>
        public bool Initialize()
        {
            try
            {
                _settings = _settingsStore.Load();
            }
            catch (IssueStoreException ex)
            {
                _dialogs.ShowError(Title, ex.Message);
                return false;
            }

            while (true)
            {
                if (!_settings.HasStorageFolder)
                {
                    if (!AskAndSaveSettings()) return false;
                    continue;
                }

                try
                {
                    OpenStore(_settings.StorageFolder);
                    break;
                }
                catch (IssueStoreException ex)
                {
                    _dialogs.ShowError(Title, ex.Message);
                    if (!AskAndSaveSettings()) return false;
                }
            }

            // Open the collection from last time. If it is gone (deleted or renamed),
            // open the first one, so the user does not start with an empty window.
            SelectedCollectionName = CollectionNames.Contains(_settings.LastCollection)
                ? _settings.LastCollection
                : CollectionNames.FirstOrDefault();
            return true;
        }

        /// <summary>
        /// Stops dictation and saves. If the save fails, asks the user whether to
        /// close anyway (when Revit exits, only tells the user). Returns false when
        /// the window must stay open.
        /// </summary>
        public async Task<bool> ShutdownAsync(bool revitIsExiting = false)
        {
            if (IsRecording) await StopDictationAsync(showErrors: true);

            if (!FlushPendingSave())
            {
                if (revitIsExiting)
                {
                    // Revit closes in any case: there is nothing to choose, only to tell.
                    _dialogs.ShowError(Title, "Revit is closing. Some changes could not be saved and are lost:\n\n" + SaveError);
                }
                else if (!_dialogs.ConfirmDanger(Title,
                    "Some changes could not be saved:\n\n" + SaveError + "\n\nClose and lose these changes?",
                    "Close without saving"))
                {
                    return false;
                }
            }

            if (_collection != null)
            {
                _settings.LastCollection = _collection.Name;
                try
                {
                    _settingsStore.Save(_settings);
                }
                catch (IssueStoreException ex)
                {
                    _dialogs.ShowError(Title, ex.Message);
                }
            }

            _dictation.Faulted -= OnDictationFaulted;
            _dictation.Dispose();
            return true;
        }

        /// <summary>Completes when background work started by an event (dictation fault) is done.</summary>
        internal Task WhenIdleAsync() => _background;

        // =====================================================================
        // Saving
        // =====================================================================

        /// <summary>Saves now if there are unsaved changes. Returns false if the save failed.</summary>
        public bool FlushPendingSave()
        {
            _editVersion++; // cancels a scheduled delayed save
            return SaveNow();
        }

        [RelayCommand]
        private void RetrySave() => FlushPendingSave();

        private bool SaveNow()
        {
            if (_collection == null || _store == null || !_dirty) return true;
            try
            {
                _store.Save(_collection);
                _dirty = false;
                SaveError = null;
                return true;
            }
            catch (IssueStoreException ex)
            {
                SaveError = "Not saved. Your changes are kept in memory. " + ex.Message;
                return false;
            }
        }

        private void MarkDirty()
        {
            _dirty = true;
            OnPropertyChanged(nameof(CollectionInfo));
            ScheduleSave();
        }

        // async void is required for a fire-and-forget delay on the UI thread; every
        // exception is caught here because an escaping one would reach Revit.
        private async void ScheduleSave()
        {
            int version = ++_editVersion;
            try
            {
                await Task.Delay(SaveDelay);
                if (version == _editVersion) SaveNow();
            }
            catch (Exception ex)
            {
                _dialogs.ShowError(Title, "Automatic save failed: " + ex.Message);
            }
        }

        // =====================================================================
        // Storage folder and settings
        // =====================================================================

        private void OpenStore(string folder)
        {
            var store = _openStore(folder);
            var names = store.ListCollections(); // throws if the folder cannot be read

            CloseCollection();
            _store = store;
            CollectionNames.Clear();
            foreach (var name in names) CollectionNames.Add(name);
            OnPropertyChanged(nameof(StorageFolder));
        }

        private bool AskAndSaveSettings()
        {
            var edited = _dialogs.EditSettings(_settings, _dictation.GetMicrophoneNames());
            if (edited == null)
            {
                _dialogs.ShowInfo(Title, "The Issue Tracker needs a storage folder for the issue collections, " +
                                         "so it closes now. Open it again to choose a folder.");
                return false;
            }
            try
            {
                _settingsStore.Save(edited);
            }
            catch (IssueStoreException ex)
            {
                _dialogs.ShowError(Title, ex.Message);
                return false;
            }
            _settings = edited;
            return true;
        }

        [RelayCommand(CanExecute = nameof(CanOpenSettings))]
        private void OpenSettings() => Guarded(() =>
        {
            // Save first: a new folder must never receive the current collection (review D4).
            if (!FlushPendingSave())
            {
                _dialogs.ShowError(Title, "Settings cannot be changed while there are unsaved changes.\n\n" + SaveError);
                return;
            }

            var edited = _dialogs.EditSettings(_settings, _dictation.GetMicrophoneNames());
            if (edited == null) return;

            bool folderChanged = !string.Equals(edited.StorageFolder, _settings.StorageFolder, StringComparison.OrdinalIgnoreCase);
            IIssueStore? newStore = null;
            IReadOnlyList<string>? newNames = null;
            if (folderChanged)
            {
                try
                {
                    newStore = _openStore(edited.StorageFolder);
                    newNames = newStore.ListCollections();
                }
                catch (IssueStoreException ex)
                {
                    _dialogs.ShowError(Title, ex.Message + "\n\nThe settings were not changed.");
                    return;
                }
            }

            try
            {
                _settingsStore.Save(edited);
            }
            catch (IssueStoreException ex)
            {
                _dialogs.ShowError(Title, ex.Message);
                return;
            }
            _settings = edited;

            if (folderChanged)
            {
                CloseCollection();
                _store = newStore;
                CollectionNames.Clear();
                foreach (var name in newNames!) CollectionNames.Add(name);
                OnPropertyChanged(nameof(StorageFolder));
                // Open directly: the new folder can have a collection with the same name as the
                // old one, and then a plain assignment does not raise a selection change.
                string? first = CollectionNames.FirstOrDefault();
                RestoreCollectionSelection(first);
                OpenCollection(first);
            }
        });

        private bool CanOpenSettings() => !IsBusy && !IsRecording;

        // =====================================================================
        // Collections
        // =====================================================================

        // Guarded: this runs inside the ComboBox binding, which would swallow an
        // exception without a message.
        partial void OnSelectedCollectionNameChanged(string? oldValue, string? newValue) => Guarded(() =>
        {
            if (_restoringSelection) return;

            if (_collection != null && !FlushPendingSave())
            {
                _dialogs.ShowError(Title,
                    "The open collection has changes that could not be saved, so it stays open.\n\n" + SaveError);
                RestoreCollectionSelection(oldValue);
                return;
            }

            OpenCollection(newValue);
        });

        private void RestoreCollectionSelection(string? name)
        {
            _restoringSelection = true;
            try { SelectedCollectionName = name; }
            finally { _restoringSelection = false; }
        }

        private void OpenCollection(string? name)
        {
            CloseCollection();
            if (name == null || _store == null) return;

            try
            {
                _collection = _store.Load(name);
            }
            catch (IssueStoreException ex)
            {
                _dialogs.ShowError(Title, ex.Message);
                return;
            }

            var store = _store;
            var collection = _collection;
            foreach (var issue in collection.Issues.OrderByDescending(i => i.Number))
                Issues.Add(CreateIssueViewModel(issue, store, collection));

            _settings.LastCollection = name;
            SelectedIssue = Issues.FirstOrDefault();
            RaiseCollectionChanged();
        }

        /// <summary>
        /// Discards the open collection from memory. Callers that must keep changes
        /// save BEFORE calling this; here the state is cleared first so that the
        /// selection change below does not try to save.
        /// </summary>
        private void CloseCollection()
        {
            _collection = null;
            _dirty = false;
            SaveError = null;
            foreach (var issue in Issues) issue.Edited -= OnIssueEdited;
            SelectedIssue = null;
            Issues.Clear();
            RaiseCollectionChanged();
        }

        private IssueViewModel CreateIssueViewModel(Issue issue, IIssueStore store, IssueCollection collection)
        {
            var vm = new IssueViewModel(issue, file => store.ScreenshotPath(collection, file));
            vm.Edited += OnIssueEdited;
            return vm;
        }

        private void OnIssueEdited(IssueViewModel issue) => MarkDirty();

        [RelayCommand(CanExecute = nameof(CanManageCollections))]
        private void NewCollection() => Guarded(() =>
        {
            if (_store == null) return;
            var fields = new[]
            {
                new TextField("Collection name", string.Empty),
                new TextField("Project name", string.Empty),
                new TextField("Author", string.Empty),
            };
            if (!_dialogs.AskTexts("New collection", fields)) return;

            IssueCollection created;
            try
            {
                created = _store.Create(fields[0].Value);
                created.ProjectName = fields[1].Value.Trim();
                created.AuthorName = fields[2].Value.Trim();
                _store.Save(created);
            }
            catch (IssueStoreException ex)
            {
                _dialogs.ShowError("New collection", ex.Message);
                return;
            }

            int index = CollectionNames.TakeWhile(n => string.Compare(n, created.Name, StringComparison.OrdinalIgnoreCase) < 0).Count();
            CollectionNames.Insert(index, created.Name);
            SelectedCollectionName = created.Name;
        });

        private bool CanManageCollections() => _store != null && !IsBusy && !IsRecording;

        [RelayCommand(CanExecute = nameof(CanChangeCollection))]
        private void EditCollection() => Guarded(() =>
        {
            var collection = _collection!;
            var fields = new[]
            {
                new TextField("Project name", collection.ProjectName),
                new TextField("Author", collection.AuthorName),
            };
            if (!_dialogs.AskTexts($"Collection '{collection.Name}'", fields)) return;

            collection.ProjectName = fields[0].Value.Trim();
            collection.AuthorName = fields[1].Value.Trim();
            _dirty = true;
            FlushPendingSave();
            OnPropertyChanged(nameof(CollectionInfo));
        });

        [RelayCommand(CanExecute = nameof(CanChangeCollection))]
        private void DeleteCollection() => Guarded(() =>
        {
            var collection = _collection!;
            if (!_dialogs.ConfirmDanger("Delete collection",
                    $"Delete the collection '{collection.Name}' with its {collection.Issues.Count} issue(s) and all screenshots?\n\n" +
                    "This cannot be undone.",
                    "Delete collection"))
                return;

            try
            {
                _store!.Delete(collection.Name);
            }
            catch (IssueStoreException ex)
            {
                _dialogs.ShowError("Delete collection", ex.Message);
                return;
            }

            // No collection is open afterwards: the user picks the next one (no
            // automatic "open the first" here; review item 5, user ruling 2026-11-24).
            CloseCollection();
            _restoringSelection = true;
            try
            {
                CollectionNames.Remove(collection.Name);
                SelectedCollectionName = null;
            }
            finally
            {
                _restoringSelection = false;
            }
        });

        private bool CanChangeCollection() => _collection != null && !IsBusy && !IsRecording;

        // =====================================================================
        // Issues
        // =====================================================================

        partial void OnSelectedIssueChanging(IssueViewModel? value)
        {
            // Save the edits of the issue that is being left.
            FlushPendingSave();
        }

        partial void OnSelectedIssueChanged(IssueViewModel? value)
        {
            SelectedScreenshot = null;
            OnPropertyChanged(nameof(HasSelectedIssue));
            RefreshCommands();
        }

        [RelayCommand(CanExecute = nameof(CanAddIssue))]
        private Task NewIssue() => GuardedAsync(async () =>
        {
            var collection = _collection!;
            IReadOnlyList<string> ids;
            IsBusy = true;
            try
            {
                ids = await _revit.GetSelectedElementUniqueIdsAsync();
            }
            catch (Exception ex)
            {
                _dialogs.ShowError("New issue", ex.Message);
                return;
            }
            finally
            {
                IsBusy = false;
            }

            if (ids.Count == 0 &&
                !_dialogs.Confirm("New issue",
                    "No elements are selected in Revit.\n\nCreate the issue without related elements?"))
                return;

            var issue = collection.AddIssue(ids, DateTime.Now);
            var vm = CreateIssueViewModel(issue, _store!, collection);
            Issues.Insert(0, vm);
            _dirty = true;
            SelectedIssue = vm; // the selection change saves
            FlushPendingSave();
            RaiseCollectionChanged();
            StatusText = ids.Count == 0
                ? $"Issue #{issue.Number} created without elements."
                : $"Issue #{issue.Number} created with {ids.Count} element(s).";
        });

        private bool CanAddIssue() => _collection != null && !IsBusy;

        [RelayCommand(CanExecute = nameof(CanChangeIssue))]
        private void DeleteIssue() => Guarded(() =>
        {
            var vm = SelectedIssue!;
            if (!_dialogs.ConfirmDanger("Delete issue",
                    $"Delete issue {vm.Title} and its {vm.Screenshots.Count} screenshot(s)?\n\nThis cannot be undone.",
                    "Delete issue"))
                return;

            IReadOnlyList<string> notDeleted;
            try
            {
                notDeleted = _store!.DeleteIssue(_collection!, vm.Model);
            }
            catch (IssueStoreException ex)
            {
                _dialogs.ShowError("Delete issue", ex.Message);
                return;
            }

            _dirty = false; // DeleteIssue saved the whole collection
            SaveError = null;
            int index = Issues.IndexOf(vm);
            vm.Edited -= OnIssueEdited;
            Issues.Remove(vm);
            SelectedIssue = Issues.Count == 0 ? null : Issues[Math.Min(index, Issues.Count - 1)];
            RaiseCollectionChanged();
            StatusText = $"Issue #{vm.Number} deleted.";
            ReportFilesLeft("Delete issue", $"Issue #{vm.Number} is deleted.", notDeleted);
        });

        private bool CanChangeIssue() => SelectedIssue != null && !IsBusy && !(IsRecording && SelectedIssue == _dictationTarget);

        // =====================================================================
        // Screenshots
        // =====================================================================

        partial void OnSelectedScreenshotChanged(ScreenshotViewModel? value) => RefreshCommands();

        [RelayCommand(CanExecute = nameof(CanEditSelectedIssue))]
        private Task AddScreenshot() => GuardedAsync(async () =>
        {
            var vm = SelectedIssue!;
            byte[]? png;
            IsBusy = true;
            try
            {
                png = await _capture.CaptureRegionAsync();
            }
            catch (Exception ex)
            {
                _dialogs.ShowError("Screenshot", "The screenshot could not be taken: " + ex.Message);
                return;
            }
            finally
            {
                IsBusy = false;
            }

            if (png == null) return; // the user cancelled

            string fileName;
            try
            {
                fileName = _store!.AddScreenshot(_collection!, vm.Model, png); // also saves pending edits
            }
            catch (IssueStoreException ex)
            {
                _dialogs.ShowError("Screenshot", ex.Message);
                return;
            }
            _dirty = false;
            SaveError = null;

            vm.AddScreenshot(fileName);
            SelectedScreenshot = vm.Screenshots.Last();
        });

        private bool CanEditSelectedIssue() => SelectedIssue != null && !IsBusy;

        [RelayCommand(CanExecute = nameof(CanRemoveScreenshot))]
        private void RemoveScreenshot() => Guarded(() =>
        {
            var vm = SelectedIssue!;
            var shot = SelectedScreenshot!;
            if (!_dialogs.ConfirmDanger("Remove screenshot", "Remove the selected screenshot? The image file is deleted.", "Remove"))
                return;

            IReadOnlyList<string> notDeleted;
            try
            {
                notDeleted = _store!.RemoveScreenshot(_collection!, vm.Model, shot.FileName); // also saves pending edits
            }
            catch (IssueStoreException ex)
            {
                _dialogs.ShowError("Remove screenshot", ex.Message);
                return;
            }
            _dirty = false;
            SaveError = null;
            vm.Screenshots.Remove(shot);
            ReportFilesLeft("Remove screenshot", "The screenshot is removed from the issue.", notDeleted);
        });

        /// <summary>The change is saved, but some image files could not be deleted: tell the user where they are.</summary>
        private void ReportFilesLeft(string title, string done, IReadOnlyList<string> notDeleted)
        {
            if (notDeleted.Count == 0) return;
            _dialogs.ShowInfo(title, done + "\n\nThese image files could not be deleted (in use or no access). " +
                                     "Delete them by hand if you do not need them:\n" + string.Join("\n", notDeleted));
        }

        private bool CanRemoveScreenshot() => SelectedIssue != null && SelectedScreenshot != null && !IsBusy;

        // =====================================================================
        // Revit
        // =====================================================================

        [RelayCommand(CanExecute = nameof(CanSelectElements))]
        private Task SelectElements() => GuardedAsync(async () =>
        {
            var ids = SelectedIssue!.ElementUniqueIds;
            int missing;
            IsBusy = true;
            try
            {
                missing = await _revit.SelectElementsAsync(ids);
            }
            catch (Exception ex)
            {
                _dialogs.ShowError("Select in Revit", ex.Message);
                return;
            }
            finally
            {
                IsBusy = false;
            }

            StatusText = missing == 0
                ? $"{ids.Count} element(s) selected in Revit."
                : $"{missing} of {ids.Count} element(s) were not found in the active document.";
        });

        private bool CanSelectElements() => SelectedIssue != null && SelectedIssue.HasElements && !IsBusy;

        // =====================================================================
        // Dictation
        // =====================================================================

        [RelayCommand(CanExecute = nameof(CanToggleDictation))]
        private Task ToggleDictation() => GuardedAsync(async () =>
        {
            if (IsRecording)
            {
                await StopDictationAsync(showErrors: true);
                return;
            }

            if (!_settings.HasSpeechService)
            {
                _dialogs.ShowError("Dictation", "Azure Speech is not set up. Enter the key and the region in Settings.");
                return;
            }

            var target = SelectedIssue!;
            try
            {
                await _dictation.StartAsync(new DictationOptions
                {
                    AzureKey = _settings.AzureSpeechKey,
                    AzureRegion = _settings.AzureSpeechRegion,
                    MicrophoneName = _settings.MicrophoneName,
                    Language = _settings.SpeechLanguage,
                });
            }
            catch (DictationException ex)
            {
                _dialogs.ShowError("Dictation", ex.Message);
                return;
            }

            _dictationTarget = target;
            IsRecording = true;
            StatusText = $"Recording into issue #{target.Number}… Click the button again to stop.";
        });

        private bool CanToggleDictation() => _stopping == null && (IsRecording || (SelectedIssue != null && !IsBusy));

        /// <summary>
        /// Stops the recording once. A stop that is already running (after a fault, or from
        /// the window closing) is shared, so the text is never collected or added twice.
        /// </summary>
        private Task StopDictationAsync(bool showErrors)
        {
            if (_stopping != null) return _stopping.Task;
            var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _stopping = done;
            RefreshCommands();
            return RunStopAsync(showErrors, done);
        }

        private async Task RunStopAsync(bool showErrors, TaskCompletionSource<bool> done)
        {
            try
            {
                await StopAndAppendAsync(showErrors);
                done.SetResult(true);
            }
            catch (Exception ex)
            {
                done.SetException(ex);
                throw;
            }
            finally
            {
                _stopping = null;
                RefreshCommands();
            }
        }

        private async Task StopAndAppendAsync(bool showErrors)
        {
            var target = _dictationTarget!;
            string text;
            string? error = null;
            try
            {
                text = await _dictation.StopAsync();
            }
            catch (DictationException ex)
            {
                text = ex.PartialText;
                error = ex.Message;
            }
            finally
            {
                IsRecording = false;
                _dictationTarget = null;
            }

            target.AppendText(text); // marks the collection dirty -> saved
            StatusText = text.Length > 0 ? $"Dictated text added to issue #{target.Number}." : "No speech was recognized.";
            if (error != null && showErrors) _dialogs.ShowError("Dictation", error);
        }

        private void OnDictationFaulted(DictationException ex)
        {
            // A stop that already runs collects the text and reports its own errors (one message).
            if (!IsRecording || _stopping != null) return;
            _dialogs.ShowError("Dictation", ex.Message);
            _background = StopAfterFaultAsync();
        }

        private async Task StopAfterFaultAsync()
        {
            try
            {
                // The cause is already shown; a second dictation error would only repeat it.
                await StopDictationAsync(showErrors: false);
            }
            catch (Exception ex)
            {
                // Nobody awaits this task in the app: show anything unexpected instead of losing it.
                _dialogs.ShowError("Dictation", "Unexpected error while stopping the dictation:\n" + ex.Message);
            }
        }

        // =====================================================================
        // PDF
        // =====================================================================

        [RelayCommand(CanExecute = nameof(CanExport))]
        private Task ExportPdf() => GuardedAsync(async () =>
        {
            var collection = _collection!;
            var store = _store!;
            if (!FlushPendingSave())
            {
                _dialogs.ShowError("Export PDF", "Save the changes first. " + SaveError);
                return;
            }

            var path = _dialogs.AskPdfPath($"{collection.Name}_{DateTime.Now:yyyyMMdd}.pdf");
            if (path == null) return;

            IsBusy = true;
            StatusText = "Writing PDF…";
            try
            {
                // A copy: the user can go on typing while the PDF is written on another thread.
                var snapshot = collection.Snapshot();
                await Task.Run(() => _pdf.Export(snapshot, file => store.ScreenshotPath(snapshot, file), path));
            }
            catch (Exception ex)
            {
                StatusText = string.Empty;
                _dialogs.ShowError("Export PDF", "The PDF could not be written:\n" + path + "\n\n" + ex.Message);
                return;
            }
            finally
            {
                IsBusy = false;
            }

            StatusText = "PDF saved.";
            _dialogs.ShowInfo("Export PDF", "The PDF is saved:\n" + path);
        });

        private bool CanExport() => _collection != null && _collection.Issues.Count > 0 && !IsBusy && !IsRecording;

        // =====================================================================
        // Helpers
        // =====================================================================

        partial void OnIsBusyChanged(bool value) => RefreshCommands();

        partial void OnIsRecordingChanged(bool value)
        {
            OnPropertyChanged(nameof(DictationLabel));
            RefreshCommands();
        }

        private void RaiseCollectionChanged()
        {
            OnPropertyChanged(nameof(HasCollection));
            OnPropertyChanged(nameof(CollectionInfo));
            RefreshCommands();
        }

        private void RefreshCommands()
        {
            OnPropertyChanged(nameof(CanSwitchCollection));
            OpenSettingsCommand.NotifyCanExecuteChanged();
            NewCollectionCommand.NotifyCanExecuteChanged();
            EditCollectionCommand.NotifyCanExecuteChanged();
            DeleteCollectionCommand.NotifyCanExecuteChanged();
            NewIssueCommand.NotifyCanExecuteChanged();
            DeleteIssueCommand.NotifyCanExecuteChanged();
            AddScreenshotCommand.NotifyCanExecuteChanged();
            RemoveScreenshotCommand.NotifyCanExecuteChanged();
            SelectElementsCommand.NotifyCanExecuteChanged();
            ToggleDictationCommand.NotifyCanExecuteChanged();
            ExportPdfCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Commands run on the Revit UI thread; an exception that escapes would reach
        /// Revit. Expected failures are handled in each command; this shows anything else.
        /// </summary>
        private void Guarded(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _dialogs.ShowError(Title, "Unexpected error: " + ex.Message + "\n\n" + ex.GetType().FullName);
            }
        }

        private async Task GuardedAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                IsBusy = false;
                _dialogs.ShowError(Title, "Unexpected error: " + ex.Message + "\n\n" + ex.GetType().FullName);
            }
        }
    }
}
