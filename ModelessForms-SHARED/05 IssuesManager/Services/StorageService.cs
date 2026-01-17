using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModelessForms.IssuesManager.Models;

#if REVIT2025 || REVIT2026
using System.Text.Json;
#else
using System.Runtime.Serialization.Json;
using System.Text;
#endif

namespace ModelessForms.IssuesManager.Services
{
    public class StorageService
    {
        private static readonly string SettingsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IssueTracker");
        private static readonly string SettingsPath = Path.Combine(SettingsFolder, "settings.json");

        public Settings LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new Settings();

                var json = File.ReadAllText(SettingsPath);
                return DeserializeSettings(json) ?? new Settings();
            }
            catch
            {
                return new Settings();
            }
        }

        public void SaveSettings(Settings settings)
        {
            try
            {
                if (!Directory.Exists(SettingsFolder))
                    Directory.CreateDirectory(SettingsFolder);

                var json = SerializeSettings(settings);
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
            }
        }

        public Collection LoadCollection(string baseFolder, string collectionName)
        {
            try
            {
                var collectionFolder = Path.Combine(baseFolder, collectionName);
                var collectionPath = Path.Combine(collectionFolder, "collection.json");

                if (!File.Exists(collectionPath))
                    return new Collection(collectionName);

                var json = File.ReadAllText(collectionPath);
                return DeserializeCollection(json) ?? new Collection(collectionName);
            }
            catch
            {
                return new Collection(collectionName);
            }
        }

        public void SaveCollection(string baseFolder, Collection collection)
        {
            try
            {
                var collectionFolder = Path.Combine(baseFolder, collection.Name);
                var imagesFolder = Path.Combine(collectionFolder, "images");
                var collectionPath = Path.Combine(collectionFolder, "collection.json");

                if (!Directory.Exists(collectionFolder))
                    Directory.CreateDirectory(collectionFolder);

                if (!Directory.Exists(imagesFolder))
                    Directory.CreateDirectory(imagesFolder);

                var json = SerializeCollection(collection);
                File.WriteAllText(collectionPath, json);
            }
            catch
            {
            }
        }

        public void DeleteCollection(string baseFolder, string collectionName)
        {
            try
            {
                var collectionFolder = Path.Combine(baseFolder, collectionName);
                if (Directory.Exists(collectionFolder))
                    Directory.Delete(collectionFolder, true);
            }
            catch
            {
            }
        }

        public List<string> GetCollectionNames(string baseFolder)
        {
            try
            {
                if (!Directory.Exists(baseFolder))
                {
                    Directory.CreateDirectory(baseFolder);
                    return new List<string>();
                }

                return Directory.GetDirectories(baseFolder)
                    .Select(Path.GetFileName)
                    .Where(name => File.Exists(Path.Combine(baseFolder, name, "collection.json")))
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        public string GetImagesFolder(string baseFolder, string collectionName)
        {
            var folder = Path.Combine(baseFolder, collectionName, "images");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
        }

#if REVIT2025 || REVIT2026
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = null
        };

        private string SerializeSettings(Settings settings)
        {
            return JsonSerializer.Serialize(settings, JsonOptions);
        }

        private Settings DeserializeSettings(string json)
        {
            return JsonSerializer.Deserialize<Settings>(json, JsonOptions);
        }

        private string SerializeCollection(Collection collection)
        {
            return JsonSerializer.Serialize(collection, JsonOptions);
        }

        private Collection DeserializeCollection(string json)
        {
            return JsonSerializer.Deserialize<Collection>(json, JsonOptions);
        }
#else
        private string SerializeSettings(Settings settings)
        {
            var serializer = new DataContractJsonSerializer(typeof(Settings));
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, settings);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        private Settings DeserializeSettings(string json)
        {
            var serializer = new DataContractJsonSerializer(typeof(Settings));
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return (Settings)serializer.ReadObject(ms);
            }
        }

        private string SerializeCollection(Collection collection)
        {
            var serializer = new DataContractJsonSerializer(typeof(Collection));
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, collection);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        private Collection DeserializeCollection(string json)
        {
            var serializer = new DataContractJsonSerializer(typeof(Collection));
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return (Collection)serializer.ReadObject(ms);
            }
        }
#endif
    }
}
