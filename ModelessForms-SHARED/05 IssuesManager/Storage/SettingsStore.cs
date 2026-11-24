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

        /// <summary>The plain-text key field in the old Issue Tracker's settings file.</summary>
        private const string LegacyKeyField = "AzureSpeechKey";

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

            RemoveKeyFromLegacyFile();
        }

        /// <summary>
        /// The old Issue Tracker stored the Azure key as plain text. When the key is
        /// safely in the new (encrypted) file, blank it in the old file. The other old
        /// fields are not changed. Runs after a successful save only.
        /// </summary>
        private void RemoveKeyFromLegacyFile()
        {
            if (!File.Exists(_legacyFile)) return;
            try
            {
                var doc = JsonNode.Parse(File.ReadAllText(_legacyFile)) as JsonObject;
                if (doc == null || !doc.TryGetPropertyValue(LegacyKeyField, out var node)) return;
                if (node is JsonValue value && value.TryGetValue(out string? key) && key.Length == 0) return;

                doc[LegacyKeyField] = string.Empty;
                AtomicFile.WriteAllText(_legacyFile, doc.ToJsonString(VersionedJson.Options));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is System.Text.Json.JsonException)
            {
                throw new IssueStoreException(
                    "The settings were saved. But the Azure Speech key could not be removed from the settings " +
                    $"file of the old Issue Tracker, where it is stored as plain text. Delete this file:\n{_legacyFile}\n{ex.Message}", ex);
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
                // The old Settings class always wrote every field as text. A missing
                // field, null or another type means the file is damaged: refuse it
                // (no repair; review item 7, user ruling 2026-11-24).
                foreach (var field in new[] { "BaseFolder", "DefaultCollection", "AzureSpeechKey", "AzureSpeechRegion", "MicrophoneName", "SpeechLanguage" })
                    LegacyJson.RequiredString(doc, field);

                Rename(doc, "BaseFolder", "StorageFolder");
                Rename(doc, "DefaultCollection", "LastCollection");

                var key = LegacyJson.RequiredString(doc, LegacyKeyField);
                doc.Remove(LegacyKeyField);
                doc["AzureSpeechKeyProtected"] = _protector.Protect(key);
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
