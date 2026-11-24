using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ModelessForms.IssuesManager.Storage
{
    /// <summary>
    /// One step of a file-format upgrade chain: changes a document from
    /// <see cref="FromVersion"/> to <c>FromVersion + 1</c>. Steps work on the raw JSON,
    /// so old formats never need old C# classes.
    /// </summary>
    internal interface IFormatUpgrade
    {
        int FromVersion { get; }

        /// <summary>Changes <paramref name="doc"/> in place. Throws <see cref="FormatException"/> on bad data.</summary>
        void Apply(JsonObject doc);
    }

    /// <summary>
    /// Reads and writes JSON files that carry a <c>FormatVersion</c> field. A file
    /// without the field is version 0 (the files of the old Issue Tracker).
    /// On read, every upgrade step from the file's version to the current version runs in order.
    /// </summary>
    internal sealed class VersionedJson
    {
        public const string VersionField = "FormatVersion";

        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = null,
        };

        private readonly IReadOnlyList<IFormatUpgrade> _upgrades;

        public int CurrentVersion { get; }

        /// <param name="upgrades">Exactly one step for each version 0 .. current-1.</param>
        public VersionedJson(IEnumerable<IFormatUpgrade> upgrades)
        {
            _upgrades = upgrades.OrderBy(u => u.FromVersion).ToList();
            for (int i = 0; i < _upgrades.Count; i++)
            {
                if (_upgrades[i].FromVersion != i)
                    throw new ArgumentException($"Upgrade steps must cover versions 0..n-1 without gaps; step {i} is missing.");
            }
            CurrentVersion = _upgrades.Count;
        }

        /// <summary>The result of a read: the object, and the file version it was read from.</summary>
        public readonly struct ReadResult<T>
        {
            public ReadResult(T value, int fileVersion) { Value = value; FileVersion = fileVersion; }
            public T Value { get; }
            public int FileVersion { get; }
        }

        /// <summary>
        /// Parses <paramref name="json"/>, upgrades it to the current version and
        /// deserializes it. Throws <see cref="FormatException"/> for any bad content,
        /// including a version that is newer than this code knows.
        /// </summary>
        public ReadResult<T> Read<T>(string json)
        {
            JsonObject doc;
            try
            {
                doc = JsonNode.Parse(json) as JsonObject
                      ?? throw new FormatException("The file does not contain a JSON object.");
            }
            catch (JsonException ex)
            {
                throw new FormatException("The file is not valid JSON: " + ex.Message, ex);
            }

            int fileVersion = ReadVersion(doc);
            if (fileVersion > CurrentVersion)
                throw new FormatException(
                    $"The file has format version {fileVersion}, which is newer than this Issue Tracker " +
                    $"(version {CurrentVersion}). Update the add-in.");

            for (int v = fileVersion; v < CurrentVersion; v++)
            {
                _upgrades[v].Apply(doc);
                doc[VersionField] = v + 1;
            }

            T? value;
            try
            {
                value = doc.Deserialize<T>(Options);
            }
            catch (JsonException ex)
            {
                throw new FormatException("The file content does not match the expected format: " + ex.Message, ex);
            }

            if (value == null) throw new FormatException("The file contains null.");
            return new ReadResult<T>(value, fileVersion);
        }

        /// <summary>Serializes <paramref name="value"/> with the current <c>FormatVersion</c> as the first field.</summary>
        public string Write<T>(T value)
        {
            var body = JsonSerializer.SerializeToNode(value, Options) as JsonObject
                       ?? throw new InvalidOperationException($"{typeof(T).Name} does not serialize to a JSON object.");

            var doc = new JsonObject { [VersionField] = CurrentVersion };
            foreach (var property in body.ToList())
            {
                body.Remove(property.Key);
                doc[property.Key] = property.Value;
            }
            return doc.ToJsonString(Options);
        }

        private static int ReadVersion(JsonObject doc)
        {
            // Agreed rule (spec F1): a missing FormatVersion means version 0.
            if (!doc.TryGetPropertyValue(VersionField, out var node) || node == null)
                return 0;

            if (node is JsonValue v && v.TryGetValue(out int version) && version >= 0)
                return version;

            throw new FormatException($"'{VersionField}' is not a valid version number.");
        }
    }
}
