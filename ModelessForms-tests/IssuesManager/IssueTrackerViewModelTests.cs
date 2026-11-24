using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ModelessForms.IssuesManager.Model;
using ModelessForms.IssuesManager.Storage;
using ModelessForms.IssuesManager.ViewModels;
using Xunit;

namespace ModelessForms.Tests.IssuesManager
{
    public sealed class IssueTrackerViewModelTests : IDisposable
    {
        private readonly string _root;
        private readonly string _folder;
        private readonly FakeSettingsStore _settings = new FakeSettingsStore();
        private readonly FakeRevit _revit = new FakeRevit();
        private readonly FakeCapture _capture = new FakeCapture();
        private readonly FakeDictation _dictation = new FakeDictation();
        private readonly FakePdf _pdf = new FakePdf();
        private readonly FakeDialogs _dialogs = new FakeDialogs();

        public IssueTrackerViewModelTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "IssueTrackerVmTests_" + Guid.NewGuid().ToString("N"));
            _folder = Path.Combine(_root, "store");
            Directory.CreateDirectory(_folder);
        }

        public void Dispose()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }

        private IssueTrackerViewModel NewVm() =>
            new IssueTrackerViewModel(_settings, folder => new IssueStore(folder), _revit, _capture, _dictation, _pdf, _dialogs)
            {
                SaveDelay = TimeSpan.FromHours(1), // tests flush explicitly
            };

        /// <summary>A VM that is initialized on a storage folder with one collection "Pumps".</summary>
        private IssueTrackerViewModel ReadyVm(string collection = "Pumps")
        {
            _settings.Stored.StorageFolder = _folder;
            new IssueStore(_folder).Create(collection);
            var vm = NewVm();
            Assert.True(vm.Initialize());
            return vm;
        }

        private IssueCollection OnDisk(string name = "Pumps") => new IssueStore(_folder).Load(name);

        // ---------------- start-up ----------------

        [Fact]
        public void First_run_without_folder_asks_for_settings_and_cancel_closes()
        {
            var vm = NewVm();

            Assert.False(vm.Initialize());
            Assert.Equal(1, _dialogs.SettingsShown);
            Assert.Contains("storage folder", Assert.Single(_dialogs.Infos)); // spec M3: closes with a message
        }

        [Fact]
        public void First_run_with_chosen_folder_saves_settings_and_starts_empty()
        {
            _dialogs.SettingsAnswer = s => { s.StorageFolder = _folder; return s; };
            var vm = NewVm();

            Assert.True(vm.Initialize());
            Assert.Equal(_folder, _settings.Stored.StorageFolder);
            Assert.Empty(vm.CollectionNames);
            Assert.False(vm.HasCollection);
            Assert.False(vm.NewIssueCommand.CanExecute(null));
        }

        [Fact]
        public void Unreachable_folder_shows_error_then_settings()
        {
            _settings.Stored.StorageFolder = Path.Combine(_root, "offline");
            var vm = NewVm();

            Assert.False(vm.Initialize());
            Assert.Single(_dialogs.Errors);
            Assert.Equal(1, _dialogs.SettingsShown);
            Assert.False(Directory.Exists(Path.Combine(_root, "offline")));
        }

        [Fact]
        public void Opens_last_collection_and_lists_issues_newest_first()
        {
            var store = new IssueStore(_folder);
            store.Create("A");
            var b = store.Create("B");
            b.AddIssue(Array.Empty<string>(), DateTime.Now);
            b.AddIssue(Array.Empty<string>(), DateTime.Now);
            store.Save(b);
            _settings.Stored.StorageFolder = _folder;
            _settings.Stored.LastCollection = "B";

            var vm = NewVm();
            Assert.True(vm.Initialize());

            Assert.Equal(new[] { "A", "B" }, vm.CollectionNames);
            Assert.Equal("B", vm.SelectedCollectionName);
            Assert.Equal(new[] { 2, 1 }, vm.Issues.Select(i => i.Number));
            Assert.Equal(2, vm.SelectedIssue!.Number);
        }

        [Fact]
        public void Corrupt_collection_shows_error_and_is_not_overwritten()
        {
            var vm = ReadyVm();
            var file = Path.Combine(_folder, "Broken", "collection.json");
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, "{ broken");
            vm.CollectionNames.Add("Broken");

            vm.SelectedCollectionName = "Broken";
            vm.FlushPendingSave();

            Assert.Single(_dialogs.Errors);
            Assert.Equal("{ broken", File.ReadAllText(file));
            Assert.False(vm.HasCollection);
        }

        // ---------------- collections ----------------

        [Fact]
        public void New_collection_is_created_with_properties_and_selected()
        {
            var vm = ReadyVm();
            _dialogs.TextAnswers.Enqueue(new[] { "Valves", "Project X", "Me" });

            vm.NewCollectionCommand.Execute(null);

            Assert.Equal("Valves", vm.SelectedCollectionName);
            Assert.Contains("Valves", vm.CollectionNames);
            Assert.Equal("Project X", OnDisk("Valves").ProjectName);
            Assert.Equal("Me", OnDisk("Valves").AuthorName);
        }

        [Fact]
        public void New_collection_with_existing_name_shows_error_and_keeps_data()
        {
            var vm = ReadyVm();
            await_NewIssue(vm);
            _dialogs.TextAnswers.Enqueue(new[] { "pumps", "", "" });

            vm.NewCollectionCommand.Execute(null);

            Assert.Single(_dialogs.Errors);
            Assert.Single(OnDisk().Issues);
        }

        [Fact]
        public void Edit_collection_properties_saves_them()
        {
            var vm = ReadyVm();
            _dialogs.TextAnswers.Enqueue(new[] { "Proj", "Auth" });

            vm.EditCollectionCommand.Execute(null);

            Assert.Equal("Proj", OnDisk().ProjectName);
            Assert.Equal("Auth", OnDisk().AuthorName);
        }

        [Fact]
        public void Delete_collection_after_confirm_removes_it()
        {
            var vm = ReadyVm();
            new IssueStore(_folder).Create("Other");
            vm.CollectionNames.Add("Other");

            vm.DeleteCollectionCommand.Execute(null);

            Assert.False(Directory.Exists(Path.Combine(_folder, "Pumps")));
            Assert.DoesNotContain("Pumps", vm.CollectionNames);
            Assert.Equal("Other", vm.SelectedCollectionName);
        }

        [Fact]
        public void Delete_collection_cancelled_keeps_it()
        {
            var vm = ReadyVm();
            _dialogs.DangerAnswer = false;

            vm.DeleteCollectionCommand.Execute(null);

            Assert.True(Directory.Exists(Path.Combine(_folder, "Pumps")));
        }

        // ---------------- issues ----------------

        private static void await_NewIssue(IssueTrackerViewModel vm) =>
            vm.NewIssueCommand.ExecuteAsync(null).GetAwaiter().GetResult();

        [Fact]
        public async Task New_issue_takes_the_Revit_selection_and_is_saved()
        {
            var vm = ReadyVm();
            _revit.Selection = new[] { "uid-1", "uid-2" };

            await vm.NewIssueCommand.ExecuteAsync(null);

            Assert.Equal(1, vm.SelectedIssue!.Number);
            Assert.Equal(new[] { "uid-1", "uid-2" }, vm.SelectedIssue.ElementUniqueIds);
            Assert.Equal(new[] { "uid-1", "uid-2" }, OnDisk().Issues.Single().ElementUniqueIds);
        }

        [Fact]
        public async Task New_issue_without_selection_asks_and_declining_creates_nothing()
        {
            var vm = ReadyVm();
            _dialogs.ConfirmAnswer = false;

            await vm.NewIssueCommand.ExecuteAsync(null);

            Assert.Single(_dialogs.Confirms);
            Assert.Empty(vm.Issues);
            Assert.Empty(OnDisk().Issues);
        }

        [Fact]
        public async Task New_issue_when_Revit_fails_shows_error()
        {
            var vm = ReadyVm();
            _revit.Error = new InvalidOperationException("No Revit document is open.");

            await vm.NewIssueCommand.ExecuteAsync(null);

            Assert.Equal(new[] { "No Revit document is open." }, _dialogs.Errors);
            Assert.Empty(vm.Issues);
        }

        [Fact]
        public async Task Editing_description_updates_title_and_is_saved_on_flush()
        {
            var vm = ReadyVm();
            await vm.NewIssueCommand.ExecuteAsync(null);
            var issue = vm.SelectedIssue!;
            var changed = new List<string?>();
            issue.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            issue.Description = "Valve clashes with beam\nsecond line";

            Assert.Contains(nameof(IssueViewModel.Title), changed);
            Assert.Equal("#1  Valve clashes with beam", issue.Title);
            Assert.Equal(string.Empty, OnDisk().Issues.Single().Description);

            vm.FlushPendingSave();

            Assert.Equal("Valve clashes with beam\nsecond line", OnDisk().Issues.Single().Description);
        }

        [Fact]
        public async Task Switching_issue_saves_pending_edit()
        {
            var vm = ReadyVm();
            await vm.NewIssueCommand.ExecuteAsync(null);
            await vm.NewIssueCommand.ExecuteAsync(null);
            vm.SelectedIssue!.Description = "edited";

            vm.SelectedIssue = vm.Issues.Last();

            Assert.Equal("edited", OnDisk().Issues.Single(i => i.Number == 2).Description);
        }

        [Fact]
        public void Failed_save_on_collection_switch_keeps_the_ComboBox_on_the_open_collection()
        {
            // A real WPF ComboBox: the view model sets the old name back while the ComboBox
            // is still changing its selection. The ComboBox must show the open collection.
            Theme.ThemeTests.RunSta(() =>
            {
                _settings.Stored.StorageFolder = _folder;
                var store = new IssueStore(_folder);
                store.Create("Pumps");
                store.Create("Valves");
                var vm = NewVm();
                Assert.True(vm.Initialize());
                vm.NewIssueCommand.ExecuteAsync(null).GetAwaiter().GetResult();
                vm.SelectedIssue!.Description = "pending";

                var combo = new System.Windows.Controls.ComboBox { ItemsSource = vm.CollectionNames };
                combo.SetBinding(System.Windows.Controls.Primitives.Selector.SelectedItemProperty,
                    new System.Windows.Data.Binding(nameof(vm.SelectedCollectionName))
                    {
                        Source = vm,
                        Mode = System.Windows.Data.BindingMode.TwoWay,
                    });
                Assert.Equal("Pumps", combo.SelectedItem);

                var file = Path.Combine(_folder, "Pumps", "collection.json");
                using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    combo.SelectedItem = "Valves";
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                        () => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                }

                Assert.Equal("Pumps", vm.SelectedCollectionName);
                Assert.Equal("Pumps", combo.SelectedItem);
                Assert.Equal("pending", vm.SelectedIssue!.Description);
                Assert.Single(_dialogs.Errors);
            });
        }

        [Fact]
        public async Task Failed_save_keeps_data_shows_banner_and_retry_works()
        {
            var vm = ReadyVm();
            await vm.NewIssueCommand.ExecuteAsync(null);
            vm.SelectedIssue!.Description = "keep me";
            var file = Path.Combine(_folder, "Pumps", "collection.json");

            using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                vm.FlushPendingSave();
                Assert.NotNull(vm.SaveError);
                Assert.Equal("keep me", vm.SelectedIssue.Description);
            }

            vm.RetrySaveCommand.Execute(null);

            Assert.Null(vm.SaveError);
            Assert.Equal("keep me", OnDisk().Issues.Single().Description);
        }

        [Fact]
        public async Task Delete_issue_after_confirm()
        {
            var vm = ReadyVm();
            await vm.NewIssueCommand.ExecuteAsync(null);

            vm.DeleteIssueCommand.Execute(null);

            Assert.Empty(vm.Issues);
            Assert.Null(vm.SelectedIssue);
            Assert.Empty(OnDisk().Issues);
        }

        [Fact]
        public async Task Delete_issue_with_a_locked_image_removes_the_issue_and_warns()
        {
            var vm = ReadyVm();
            await vm.NewIssueCommand.ExecuteAsync(null);
            await vm.AddScreenshotCommand.ExecuteAsync(null);
            var path = vm.SelectedIssue!.Screenshots.Single().Path;

            using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
                vm.DeleteIssueCommand.Execute(null);

            Assert.Empty(vm.Issues);
            Assert.Empty(OnDisk().Issues);
            Assert.Empty(_dialogs.Errors);
            Assert.Contains(path, Assert.Single(_dialogs.Infos));
        }

        // ---------------- screenshots ----------------

        [Fact]
        public async Task Remove_screenshot_with_a_locked_image_removes_the_thumbnail_and_warns()
        {
            var vm = ReadyVm();
            await vm.NewIssueCommand.ExecuteAsync(null);
            await vm.AddScreenshotCommand.ExecuteAsync(null);
            var shot = vm.SelectedIssue!.Screenshots.Single();
            vm.SelectedScreenshot = shot;

            using (new FileStream(shot.Path, FileMode.Open, FileAccess.Read, FileShare.None))
                vm.RemoveScreenshotCommand.Execute(null);

            Assert.Empty(vm.SelectedIssue.Screenshots);
            Assert.Empty(OnDisk().Issues.Single().Screenshots);
            Assert.Empty(_dialogs.Errors);
            Assert.Contains(shot.Path, Assert.Single(_dialogs.Infos));
        }

        [Fact]
        public async Task Add_and_remove_screenshot()
        {
            var vm = ReadyVm();
            await vm.NewIssueCommand.ExecuteAsync(null);

            await vm.AddScreenshotCommand.ExecuteAsync(null);

            var shot = Assert.Single(vm.SelectedIssue!.Screenshots);
            Assert.True(File.Exists(shot.Path));
            Assert.Single(OnDisk().Issues.Single().Screenshots);

            vm.SelectedScreenshot = shot;
            vm.RemoveScreenshotCommand.Execute(null);

            Assert.Empty(vm.SelectedIssue.Screenshots);
            Assert.False(File.Exists(shot.Path));
            Assert.Empty(OnDisk().Issues.Single().Screenshots);
        }

        [Fact]
        public async Task Cancelled_capture_adds_nothing()
        {
            var vm = ReadyVm();
            await vm.NewIssueCommand.ExecuteAsync(null);
            _capture.Next = null;

            await vm.AddScreenshotCommand.ExecuteAsync(null);

            Assert.Empty(vm.SelectedIssue!.Screenshots);
            Assert.Empty(_dialogs.Errors);
        }

        [Fact]
        public async Task Missing_screenshot_file_is_shown_as_missing()
        {
            var vm = ReadyVm();
            await vm.NewIssueCommand.ExecuteAsync(null);
            await vm.AddScreenshotCommand.ExecuteAsync(null);
            File.Delete(vm.SelectedIssue!.Screenshots.Single().Path);

            var reopened = NewVm();
            reopened.Initialize();

            Assert.False(reopened.SelectedIssue!.Screenshots.Single().Exists);
        }

        // ---------------- Revit ----------------

        [Fact]
        public async Task Select_in_Revit_reports_missing_elements()
        {
            var vm = ReadyVm();
            _revit.Selection = new[] { "uid-1", "uid-2" };
            await vm.NewIssueCommand.ExecuteAsync(null);
            _revit.MissingOnSelect = 1;

            await vm.SelectElementsCommand.ExecuteAsync(null);

            Assert.Equal(new[] { "uid-1", "uid-2" }, _revit.LastSelected);
            Assert.Contains("1 of 2", vm.StatusText);
        }

        // ---------------- dictation ----------------

        [Fact]
        public async Task Dictation_appends_to_the_issue_where_it_started()
        {
            _settings.Stored.AzureSpeechKey = "k";
            _settings.Stored.AzureSpeechRegion = "r";
            var vm = ReadyVm();
            await vm.NewIssueCommand.ExecuteAsync(null);
            await vm.NewIssueCommand.ExecuteAsync(null);
            var first = vm.SelectedIssue!;
            first.Description = "Existing";

            await vm.ToggleDictationCommand.ExecuteAsync(null);
            Assert.True(vm.IsRecording);
            vm.SelectedIssue = vm.Issues.Last();
            await vm.ToggleDictationCommand.ExecuteAsync(null);

            Assert.False(vm.IsRecording);
            Assert.Equal("Existing" + Environment.NewLine + "dictated text", first.Description);
            Assert.Equal(string.Empty, vm.Issues.Last().Description);
        }

        [Fact]
        public async Task Dictation_without_key_shows_error_and_does_not_start()
        {
            var vm = ReadyVm();
            await vm.NewIssueCommand.ExecuteAsync(null);

            await vm.ToggleDictationCommand.ExecuteAsync(null);

            Assert.Single(_dialogs.Errors);
            Assert.False(vm.IsRecording);
        }

        [Fact]
        public async Task Dictation_start_failure_shows_exactly_one_error()
        {
            _settings.Stored.AzureSpeechKey = "k";
            _settings.Stored.AzureSpeechRegion = "r";
            var vm = ReadyVm();
            await vm.NewIssueCommand.ExecuteAsync(null);
            _dictation.StartError = new DictationException("Microphone 'USB Mic' is not connected.");

            await vm.ToggleDictationCommand.ExecuteAsync(null);

            Assert.Equal(new[] { "Microphone 'USB Mic' is not connected." }, _dialogs.Errors);
            Assert.False(vm.IsRecording);
        }

        [Fact]
        public async Task Dictation_fault_shows_one_error_and_keeps_partial_text()
        {
            _settings.Stored.AzureSpeechKey = "k";
            _settings.Stored.AzureSpeechRegion = "r";
            var vm = ReadyVm();
            await vm.NewIssueCommand.ExecuteAsync(null);
            await vm.ToggleDictationCommand.ExecuteAsync(null);
            _dictation.TextToReturn = "partial";

            _dictation.RaiseFault(new DictationException("Network lost"));
            await vm.WhenIdleAsync();

            Assert.Equal(new[] { "Network lost" }, _dialogs.Errors);
            Assert.False(vm.IsRecording);
            Assert.Equal("partial", vm.SelectedIssue!.Description);
        }

        [Fact]
        public async Task Stop_click_during_a_fault_stop_does_not_stop_twice()
        {
            _settings.Stored.AzureSpeechKey = "k";
            _settings.Stored.AzureSpeechRegion = "r";
            var vm = ReadyVm();
            await vm.NewIssueCommand.ExecuteAsync(null);
            await vm.ToggleDictationCommand.ExecuteAsync(null);
            _dictation.TextToReturn = "hello";
            _dictation.StopGate = new TaskCompletionSource<bool>();

            _dictation.RaiseFault(new DictationException("Network lost"));
            Assert.False(vm.ToggleDictationCommand.CanExecute(null)); // stopping: the button is disabled
            var close = vm.ShutdownAsync();                           // closing waits for the same stop
            _dictation.StopGate.SetResult(true);
            await vm.WhenIdleAsync();
            Assert.True(await close);

            Assert.Equal(1, _dictation.StopCalls);
            Assert.Equal("hello", OnDisk().Issues.Single().Description);
        }

        // ---------------- settings ----------------

        [Fact]
        public async Task Changing_folder_to_one_with_a_collection_of_the_same_name_opens_it()
        {
            var vm = ReadyVm();
            await vm.NewIssueCommand.ExecuteAsync(null);
            var newFolder = Path.Combine(_root, "other");
            var other = new IssueStore(newFolder.Length > 0 ? CreateDir(newFolder) : newFolder).Create("Pumps");
            other.AddIssue(Array.Empty<string>(), DateTime.Now);
            other.AddIssue(Array.Empty<string>(), DateTime.Now);
            new IssueStore(newFolder).Save(other);
            _dialogs.SettingsAnswer = s => { s.StorageFolder = newFolder; return s; };

            vm.OpenSettingsCommand.Execute(null);

            Assert.Equal("Pumps", vm.SelectedCollectionName);
            Assert.True(vm.HasCollection);
            Assert.Equal(2, vm.Issues.Count);
        }

        private static string CreateDir(string path) { Directory.CreateDirectory(path); return path; }

        [Fact]
        public async Task Changing_storage_folder_saves_old_collection_in_old_folder_only()
        {
            var vm = ReadyVm();
            await vm.NewIssueCommand.ExecuteAsync(null);
            vm.SelectedIssue!.Description = "pending";
            var newFolder = Path.Combine(_root, "other");
            Directory.CreateDirectory(newFolder);
            _dialogs.SettingsAnswer = s => { s.StorageFolder = newFolder; return s; };

            vm.OpenSettingsCommand.Execute(null);

            Assert.Equal("pending", OnDisk().Issues.Single().Description);
            Assert.Empty(Directory.GetFileSystemEntries(newFolder));
            Assert.Empty(vm.CollectionNames);
            Assert.Equal(newFolder, _settings.Stored.StorageFolder);
        }

        // ---------------- PDF ----------------

        [Fact]
        public async Task Export_pdf_works_on_a_copy_of_the_collection()
        {
            var vm = ReadyVm();
            await vm.NewIssueCommand.ExecuteAsync(null);
            vm.SelectedIssue!.Description = "before";
            _dialogs.PdfPath = Path.Combine(_root, "out.pdf");

            await vm.ExportPdfCommand.ExecuteAsync(null);
            vm.SelectedIssue.Description = "typed while exporting";

            Assert.Equal("before", _pdf.Exported!.Issues.Single().Description);
            Assert.Equal("Pumps", _pdf.Exported.Name);
        }

        [Fact]
        public async Task Export_pdf_saves_first_and_exports()
        {
            var vm = ReadyVm();
            await vm.NewIssueCommand.ExecuteAsync(null);
            vm.SelectedIssue!.Description = "pending";
            _dialogs.PdfPath = Path.Combine(_root, "out.pdf");

            await vm.ExportPdfCommand.ExecuteAsync(null);

            Assert.Equal("pending", OnDisk().Issues.Single().Description);
            Assert.Equal(_dialogs.PdfPath, _pdf.Path);
            Assert.False(vm.IsBusy);
            Assert.Single(_dialogs.Infos);
        }

        [Fact]
        public async Task Export_pdf_failure_shows_error()
        {
            var vm = ReadyVm();
            await vm.NewIssueCommand.ExecuteAsync(null);
            _dialogs.PdfPath = Path.Combine(_root, "out.pdf");
            _pdf.Error = new IOException("The file is open in another program.");

            await vm.ExportPdfCommand.ExecuteAsync(null);

            Assert.Single(_dialogs.Errors);
            Assert.False(vm.IsBusy);
        }

        // ---------------- shutdown ----------------

        [Fact]
        public async Task Shutdown_saves_pending_edit_and_stops_dictation()
        {
            _settings.Stored.AzureSpeechKey = "k";
            _settings.Stored.AzureSpeechRegion = "r";
            var vm = ReadyVm();
            await vm.NewIssueCommand.ExecuteAsync(null);
            vm.SelectedIssue!.Description = "pending";
            await vm.ToggleDictationCommand.ExecuteAsync(null);

            await vm.ShutdownAsync();

            Assert.Equal("pending" + Environment.NewLine + "dictated text", OnDisk().Issues.Single().Description);
            Assert.True(_dictation.Disposed);
            Assert.Equal("Pumps", _settings.Stored.LastCollection);
        }
    }
}
