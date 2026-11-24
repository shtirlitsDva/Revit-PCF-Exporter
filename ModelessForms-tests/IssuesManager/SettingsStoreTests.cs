using System;
using System.IO;
using ModelessForms.IssuesManager.Model;
using ModelessForms.IssuesManager.Storage;
using Xunit;

namespace ModelessForms.Tests.IssuesManager
{
    public sealed class SettingsStoreTests : IDisposable
    {
        private readonly string _root;
        private readonly string _file;
        private readonly string _legacyFile;
        private readonly SettingsStore _store;

        public SettingsStoreTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "SettingsStoreTests_" + Guid.NewGuid().ToString("N"));
            _file = Path.Combine(_root, "new", "settings.json");
            _legacyFile = Path.Combine(_root, "old", "settings.json");
            _store = new SettingsStore(_file, _legacyFile, new DpapiSecretProtector());
        }

        public void Dispose()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }

        [Fact]
        public void First_run_has_no_storage_folder()
        {
            var s = _store.Load();

            Assert.Equal(string.Empty, s.StorageFolder);
            Assert.False(s.HasStorageFolder);
            Assert.Equal("da-DK", s.SpeechLanguage);
        }

        [Fact]
        public void Save_then_Load_roundtrips()
        {
            var s = new IssueTrackerSettings
            {
                StorageFolder = @"C:\Issues",
                LastCollection = "Pumps",
                AzureSpeechKey = "secret-key",
                AzureSpeechRegion = "westeurope",
                MicrophoneName = "USB Mic",
                SpeechLanguage = "en-GB",
            };
            _store.Save(s);

            var l = _store.Load();

            Assert.Equal(@"C:\Issues", l.StorageFolder);
            Assert.Equal("Pumps", l.LastCollection);
            Assert.Equal("secret-key", l.AzureSpeechKey);
            Assert.Equal("westeurope", l.AzureSpeechRegion);
            Assert.Equal("USB Mic", l.MicrophoneName);
            Assert.Equal("en-GB", l.SpeechLanguage);
        }

        [Fact]
        public void Speech_key_is_not_stored_as_plain_text()
        {
            _store.Save(new IssueTrackerSettings { AzureSpeechKey = "secret-key" });

            Assert.DoesNotContain("secret-key", File.ReadAllText(_file));
        }

        [Fact]
        public void Legacy_settings_file_is_upgraded_when_no_new_file_exists()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_legacyFile)!);
            File.WriteAllText(_legacyFile,
                @"{""AzureSpeechKey"":""old-key"",""AzureSpeechRegion"":""eastus"",""BaseFolder"":""D:\\Old""," +
                @"""DefaultCollection"":""Default"",""MicrophoneName"":"""",""SpeechLanguage"":""da-DK""}");

            var s = _store.Load();

            Assert.Equal(@"D:\Old", s.StorageFolder);
            Assert.Equal("Default", s.LastCollection);
            Assert.Equal("old-key", s.AzureSpeechKey);
            Assert.Equal("eastus", s.AzureSpeechRegion);
            Assert.True(File.Exists(_legacyFile)); // the old file is left alone
        }

        [Fact]
        public void New_settings_file_wins_over_legacy_file()
        {
            _store.Save(new IssueTrackerSettings { StorageFolder = @"C:\New" });
            Directory.CreateDirectory(Path.GetDirectoryName(_legacyFile)!);
            File.WriteAllText(_legacyFile, @"{""BaseFolder"":""D:\\Old""}");

            Assert.Equal(@"C:\New", _store.Load().StorageFolder);
        }

        [Fact]
        public void Corrupt_settings_file_throws()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
            File.WriteAllText(_file, "garbage");

            Assert.Throws<IssueStoreException>(() => _store.Load());
        }
    }
}
