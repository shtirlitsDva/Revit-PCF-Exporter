using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using ModelessForms.IssuesManager.Model;

namespace ModelessForms.IssuesManager.Storage
{
    public interface ISettingsStore
    {
        /// <summary>Loads the settings. First run (no file) gives settings without a storage folder.</summary>
        IssueTrackerSettings Load();

        void Save(IssueTrackerSettings settings);
    }

    /// <summary>
    /// Stores <see cref="IssueTrackerSettings"/> as versioned JSON, with the Azure key
    /// encrypted. If the settings file does not exist yet, the settings file of the old
    /// Issue Tracker (if any) is read once and upgraded; the old file is not changed.
    /// </summary>
    public sealed class SettingsStore : ISettingsStore
    {
        private readonly string _file;
        private readonly string _legacyFile;
        private readonly ISecretProtector _protector;
        private readonly VersionedJson _format;

        public SettingsStore(string file, string legacyFile, ISecretProtector protector)
        {
            _file = file;
            _legacyFile = legacyFile;
            _protector = protector;
            _format = new VersionedJson(new IFormatUpgrade[] { new V0ToV1(protector) });
        }

        /// <summary>The settings store for the current Windows user.</summary>
        public static SettingsStore ForCurrentUser()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return new SettingsStore(
                Path.Combine(appData, "Norsyn", "IssueTracker", "settings.json"),
                Path.Combine(appData, "IssueTracker", "settings.json"),
                new DpapiSecretProtector());
        }

        public IssueTrackerSettings Load()
        {
            string? source = File.Exists(_file) ? _file
                           : File.Exists(_legacyFile) ? _legacyFile
                           : null;
            if (source == null)
                return new IssueTrackerSettings();

            string json;
            try
            {
                json = File.ReadAllText(source);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new IssueStoreException($"Could not read the settings:\n{source}\n{ex.Message}", ex);
            }

            try
            {
                var stored = _format.Read<StoredSettings>(json).Value;
                return new IssueTrackerSettings
                {
                    StorageFolder = stored.StorageFolder,
                    LastCollection = stored.LastCollection,
                    AzureSpeechKey = _protector.Unprotect(stored.AzureSpeechKeyProtected),
                    AzureSpeechRegion = stored.AzureSpeechRegion,
                    MicrophoneName = stored.MicrophoneName,
                    SpeechLanguage = stored.SpeechLanguage,
                };
            }
            catch (FormatException ex)
            {
                throw new IssueStoreException($"The settings file cannot be read:\n{source}\n{ex.Message}", ex);
            }
            catch (CryptographicException ex)
            {
                throw new IssueStoreException(
                    "The Azure Speech key in the settings cannot be decrypted (the file was made by another " +
                    $"Windows user or PC). Delete the settings file and enter the settings again:\n{source}", ex);
            }
        }

        public void Save(IssueTrackerSettings settings)
        {
            var stored = new StoredSettings
            {
                StorageFolder = settings.StorageFolder,
                LastCollection = settings.LastCollection,
                AzureSpeechKeyProtected = _protector.Protect(settings.AzureSpeechKey),
                AzureSpeechRegion = settings.AzureSpeechRegion,
                MicrophoneName = settings.MicrophoneName,
                SpeechLanguage = settings.SpeechLanguage,
            };

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
                AtomicFile.WriteAllText(_file, _format.Write(stored));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new IssueStoreException($"Could not save the settings:\n{_file}\n{ex.Message}", ex);
            }
        }

        /// <summary>The on-disk shape (current version).</summary>
        private sealed class StoredSettings
        {
            public string StorageFolder { get; set; } = string.Empty;
            public string LastCollection { get; set; } = string.Empty;
            public string AzureSpeechKeyProtected { get; set; } = string.Empty;
            public string AzureSpeechRegion { get; set; } = string.Empty;
            public string MicrophoneName { get; set; } = string.Empty;
            public string SpeechLanguage { get; set; } = string.Empty;
        }

        /// <summary>
        /// v0: the old file (%APPDATA%\IssueTracker\settings.json): BaseFolder,
        /// DefaultCollection, plain-text AzureSpeechKey. v1: renamed fields, encrypted key.
        /// </summary>
        private sealed class V0ToV1 : IFormatUpgrade
        {
            private readonly ISecretProtector _protector;

            public V0ToV1(ISecretProtector protector) => _protector = protector;

            public int FromVersion => 0;

            public void Apply(JsonObject doc)
            {
                Rename(doc, "BaseFolder", "StorageFolder");
                Rename(doc, "DefaultCollection", "LastCollection");

                var key = doc["AzureSpeechKey"]?.GetValue<string>() ?? string.Empty;
                doc.Remove("AzureSpeechKey");
                doc["AzureSpeechKeyProtected"] = _protector.Protect(key);

                foreach (var field in new[] { "StorageFolder", "LastCollection", "AzureSpeechRegion", "MicrophoneName", "SpeechLanguage" })
                    LegacyJson.NullToEmptyString(doc, field);
            }

            private static void Rename(JsonObject doc, string from, string to)
            {
                if (!doc.TryGetPropertyValue(from, out var value)) return;
                doc.Remove(from);
                doc[to] = value;
            }
        }
    }
}
