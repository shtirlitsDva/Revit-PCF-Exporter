using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModelessForms.IssuesManager.Model;

namespace ModelessForms.IssuesManager.Storage
{
    /// <summary>All persistence of issue collections. Every failure throws <see cref="IssueStoreException"/>.</summary>
    public interface IIssueStore
    {
        string StorageFolder { get; }
        IReadOnlyList<string> ListCollections();
        IssueCollection Load(string name);
        void Save(IssueCollection collection);
        IssueCollection Create(string name);
        void Delete(string name);
        /// <summary>Removes the issue and saves. Then deletes its image files.</summary>
        /// <returns>Image files that could not be deleted (the issue itself is deleted and saved).</returns>
        IReadOnlyList<string> DeleteIssue(IssueCollection collection, Issue issue);
        string AddScreenshot(IssueCollection collection, Issue issue, byte[] png);
        /// <summary>Removes the screenshot from the issue and saves. Then deletes the image file.</summary>
        /// <returns>The image file, if it could not be deleted (the screenshot is removed and saved).</returns>
        IReadOnlyList<string> RemoveScreenshot(IssueCollection collection, Issue issue, string fileName);
        string ScreenshotPath(IssueCollection collection, string fileName);
    }

    /// <summary>
    /// File storage of issue collections, one folder per collection:
    /// <code>
    /// {StorageFolder}/{Name}/collection.json
    /// {StorageFolder}/{Name}/images/*.png
    /// </code>
    /// Single user: no locking. Writes are atomic. A load that fails throws; it
    /// never returns an empty collection that a later save could write over real data.
    /// </summary>
    public sealed class IssueStore : IIssueStore
    {
        private const string CollectionFileName = "collection.json";
        private const string ImagesFolderName = "images";

        private static readonly string[] ReservedNames =
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        };

        public const int MaxNameLength = 100;

        public IssueStore(string storageFolder)
        {
            if (string.IsNullOrWhiteSpace(storageFolder))
                throw new ArgumentException("The storage folder is not set.", nameof(storageFolder));
            StorageFolder = storageFolder;
        }

        public string StorageFolder { get; }

        public IReadOnlyList<string> ListCollections()
        {
            RequireStorageFolder();
            return Io("read the storage folder", StorageFolder, () =>
                Directory.GetDirectories(StorageFolder)
                    .Where(dir => File.Exists(Path.Combine(dir, CollectionFileName)))
                    .Select(dir => Path.GetFileName(dir)!)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList());
        }

        public IssueCollection Load(string name)
        {
            RequireStorageFolder();
            var folder = CollectionFolder(name);
            var file = Path.Combine(folder, CollectionFileName);
            if (!File.Exists(file))
                throw new IssueStoreException($"Collection '{name}' does not exist: {file}");

            var json = Io("read", file, () => File.ReadAllText(file));

            VersionedJson.ReadResult<IssueCollection> result;
            try
            {
                result = CollectionFormat.Json.Read<IssueCollection>(json);
            }
            catch (FormatException ex)
            {
                throw new IssueStoreException(
                    $"Collection '{name}' cannot be read. The file was not changed.\n{file}\n{ex.Message}", ex);
            }

            if (result.FileVersion < CollectionFormat.Json.CurrentVersion)
            {
                // Keep the original before the first save writes the new format.
                var backup = Path.Combine(folder, $"collection.v{result.FileVersion}.bak.json");
                if (!File.Exists(backup))
                    Io("write a backup of", backup, () => File.WriteAllText(backup, json));
            }

            var collection = result.Value;
            collection.Name = Path.GetFileName(folder);

            string? damage = FindDamage(collection);
            if (damage != null)
                throw new IssueStoreException(
                    $"Collection '{name}' is damaged and was not opened. The file was not changed.\n{file}\n{damage}");
            return collection;
        }

        /// <summary>
        /// Checks what the JSON reader does not: .NET does not enforce non-null
        /// properties, and a hand-edited file can break the rules the code relies on.
        /// Returns a description of the first problem, or null.
        /// </summary>
        private static string? FindDamage(IssueCollection collection)
        {
            if (collection.ProjectName == null || collection.AuthorName == null || collection.Issues == null)
                return "A collection field is empty (null).";

            var numbers = new HashSet<int>();
            foreach (var issue in collection.Issues)
            {
                if (issue == null) return "An issue entry is empty (null).";
                if (string.IsNullOrWhiteSpace(issue.Id)) return $"Issue #{issue.Number} has no Id.";
                if (issue.Description == null || issue.ElementUniqueIds == null || issue.Screenshots == null)
                    return $"Issue #{issue.Number} has an empty (null) field.";
                if (issue.Number < 1 || !numbers.Add(issue.Number))
                    return $"Issue number {issue.Number} is not valid or is used twice.";
                if (issue.Number >= collection.NextIssueNumber)
                    return $"Issue #{issue.Number} is not below the next issue number {collection.NextIssueNumber}.";
                foreach (var file in issue.Screenshots)
                {
                    // File names come from the JSON file; never let one point outside the images folder.
                    if (string.IsNullOrWhiteSpace(file) || Path.GetFileName(file) != file)
                        return $"Issue #{issue.Number} has an invalid screenshot file name: '{file}'.";
                }
            }
            return null;
        }

        public void Save(IssueCollection collection)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var folder = CollectionFolder(collection.Name);
            if (!Directory.Exists(folder))
                throw new IssueStoreException(
                    $"Collection '{collection.Name}' cannot be saved: its folder does not exist " +
                    $"(was it deleted or renamed?).\n{folder}");

            var file = Path.Combine(folder, CollectionFileName);
            var json = CollectionFormat.Json.Write(collection);
            Io("save", file, () => AtomicFile.WriteAllText(file, json));
        }

        public IssueCollection Create(string name)
        {
            RequireStorageFolder();
            name = ValidateName(name);
            var folder = CollectionFolder(name);
            if (Directory.Exists(folder))
                throw new IssueStoreException($"A collection or folder named '{name}' already exists.");

            var collection = new IssueCollection { Name = name, Created = DateTime.Now };
            Io("create", folder, () =>
            {
                Directory.CreateDirectory(Path.Combine(folder, ImagesFolderName));
                AtomicFile.WriteAllText(Path.Combine(folder, CollectionFileName), CollectionFormat.Json.Write(collection));
            });
            return collection;
        }

        public void Delete(string name)
        {
            var folder = CollectionFolder(name);
            if (!Directory.Exists(folder))
                throw new IssueStoreException($"Collection '{name}' does not exist: {folder}");
            Io("delete", folder, () => Directory.Delete(folder, recursive: true));
        }

        public IReadOnlyList<string> DeleteIssue(IssueCollection collection, Issue issue)
        {
            int index = collection.Issues.IndexOf(issue);
            if (index < 0) throw new ArgumentException("The issue is not in the collection.", nameof(issue));

            collection.RemoveIssue(issue);
            try
            {
                Save(collection);
            }
            catch
            {
                collection.Issues.Insert(index, issue);
                throw;
            }

            return DeleteScreenshotFiles(collection, issue.Screenshots);
        }

        public string AddScreenshot(IssueCollection collection, Issue issue, byte[] png)
        {
            if (png == null || png.Length == 0) throw new ArgumentException("The screenshot has no data.", nameof(png));
            if (!collection.Issues.Contains(issue)) throw new ArgumentException("The issue is not in the collection.", nameof(issue));

            var fileName = $"{issue.Id}_{Guid.NewGuid():N}.png";
            var path = ScreenshotPath(collection, fileName);
            Io("save the screenshot", path, () =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                stream.Write(png, 0, png.Length);
            });

            issue.Screenshots.Add(fileName);
            try
            {
                Save(collection);
            }
            catch (Exception saveError)
            {
                issue.Screenshots.Remove(fileName);
                // The save failed: also remove the new image, so no unreferenced file is left.
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    throw new IssueStoreException(
                        saveError.Message + "\n\nThe new image file could not be removed either:\n" + path, saveError);
                }
                throw;
            }
            return fileName;
        }

        public IReadOnlyList<string> RemoveScreenshot(IssueCollection collection, Issue issue, string fileName)
        {
            int index = issue.Screenshots.IndexOf(fileName);
            if (index < 0) throw new ArgumentException("The screenshot is not in the issue.", nameof(fileName));

            issue.Screenshots.RemoveAt(index);
            try
            {
                Save(collection);
            }
            catch
            {
                issue.Screenshots.Insert(index, fileName);
                throw;
            }
            return DeleteScreenshotFiles(collection, new[] { fileName });
        }

        public string ScreenshotPath(IssueCollection collection, string fileName)
        {
            // File names come from the JSON file; never let one point outside the images folder.
            if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName)
                throw new IssueStoreException($"Invalid screenshot file name: '{fileName}'.");
            return Path.Combine(CollectionFolder(collection.Name), ImagesFolderName, fileName);
        }

        /// <summary>Trims and checks a collection name. Throws with a user message if it cannot be a folder name.</summary>
        public static string ValidateName(string name)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length == 0)
                throw new IssueStoreException("The collection name is empty.");
            if (name.Length > MaxNameLength)
                throw new IssueStoreException($"The collection name is longer than {MaxNameLength} characters.");
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new IssueStoreException("The collection name contains a character that is not allowed in a folder name.");
            if (name.EndsWith(".", StringComparison.Ordinal))
                throw new IssueStoreException("The collection name cannot end with a dot.");

            var stem = name.Split('.')[0];
            if (ReservedNames.Contains(stem, StringComparer.OrdinalIgnoreCase))
                throw new IssueStoreException($"'{name}' is a reserved name in Windows.");

            return name;
        }

        /// <summary>
        /// Deletes image files AFTER the collection was saved without them. A file that
        /// cannot be deleted (locked, no access) is returned, not thrown: the saved state is
        /// already correct, and the caller reports the files that are left on disk.
        /// </summary>
        private List<string> DeleteScreenshotFiles(IssueCollection collection, IEnumerable<string> fileNames)
        {
            var notDeleted = new List<string>();
            foreach (var fileName in fileNames)
            {
                var path = ScreenshotPath(collection, fileName);
                try
                {
                    if (File.Exists(path)) File.Delete(path);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    notDeleted.Add(path);
                }
            }
            return notDeleted;
        }

        private string CollectionFolder(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || Path.GetFileName(name) != name)
                throw new IssueStoreException($"Invalid collection name: '{name}'.");
            return Path.Combine(StorageFolder, name);
        }

        private void RequireStorageFolder()
        {
            if (!Directory.Exists(StorageFolder))
                throw new IssueStoreException(
                    $"The storage folder does not exist or cannot be reached:\n{StorageFolder}\n" +
                    "Check the network connection, or choose another folder in Settings.");
        }

        private static void Io(string action, string path, Action body) =>
            Io<object?>(action, path, () => { body(); return null; });

        private static T Io<T>(string action, string path, Func<T> body)
        {
            try
            {
                return body();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new IssueStoreException($"Could not {action}:\n{path}\n{ex.Message}", ex);
            }
        }
    }
}
