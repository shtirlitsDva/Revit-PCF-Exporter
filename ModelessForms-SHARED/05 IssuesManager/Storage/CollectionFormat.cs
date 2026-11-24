using System;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;

namespace ModelessForms.IssuesManager.Storage
{
    /// <summary>
    /// The format history of <c>collection.json</c>. To change the format: add a
    /// step class for the current version and add it to <see cref="Json"/>.
    /// </summary>
    internal static class CollectionFormat
    {
        public static readonly VersionedJson Json = new VersionedJson(new IFormatUpgrade[]
        {
            new V0ToV1(),
        });

        /// <summary>
        /// v0: files of the old Issue Tracker. Revit 2024 wrote them with
        /// DataContractJsonSerializer (dates as "/Date(ms+zone)/", null strings),
        /// Revit 2025 with System.Text.Json (ISO dates). Issues were stored newest-first,
        /// had no number, used "ElementGuids", and the file stored the collection "Name".
        /// <para>v1: dates are ISO, issues have numbers and are stored in number order,
        /// "ElementUniqueIds", "NextIssueNumber", no "Name".</para>
        /// </summary>
        private sealed class V0ToV1 : IFormatUpgrade
        {
            public int FromVersion => 0;

            public void Apply(JsonObject doc)
            {
                doc.Remove("Name");
                doc["Created"] = LegacyJson.Date(doc, "Created");
                LegacyJson.NullToEmptyString(doc, "ProjectName");
                LegacyJson.NullToEmptyString(doc, "AuthorName");

                var issues = doc["Issues"] as JsonArray
                             ?? throw new FormatException("The collection has no 'Issues' list.");

                // Take the nodes out of the array first: a node can have only one parent,
                // and the array is refilled in the new order below.
                var nodes = issues.ToList();
                issues.Clear();

                var upgraded = nodes
                    .Select((node, index) => (Issue: UpgradeIssue(node), Index: index))
                    .ToList();

                // Old code inserted new issues at index 0, so for equal timestamps the
                // higher index is the older issue.
                var ordered = upgraded
                    .OrderBy(x => x.Issue["Created"]!.GetValue<DateTime>())
                    .ThenByDescending(x => x.Index)
                    .Select(x => x.Issue)
                    .ToList();

                int number = 1;
                foreach (var issue in ordered)
                {
                    issue["Number"] = number++;
                    issues.Add(issue);
                }
                doc["NextIssueNumber"] = number;
            }

            private static JsonObject UpgradeIssue(JsonNode? node)
            {
                var issue = node as JsonObject ?? throw new FormatException("An issue is not a JSON object.");

                var id = LegacyJson.RequiredString(issue, "Id");
                if (id.Trim().Length == 0)
                    throw new FormatException("An issue has no 'Id'.");

                issue["Created"] = LegacyJson.Date(issue, "Created");
                issue["Modified"] = LegacyJson.Date(issue, "Modified");
                LegacyJson.NullToEmptyString(issue, "Description");

                var elements = issue["ElementGuids"] as JsonArray
                               ?? throw new FormatException($"Issue '{id}' has no 'ElementGuids' list.");
                issue.Remove("ElementGuids");
                issue["ElementUniqueIds"] = elements;

                if (!(issue["Screenshots"] is JsonArray))
                    throw new FormatException($"Issue '{id}' has no 'Screenshots' list.");

                return issue;
            }
        }
    }

    /// <summary>Helpers to read values that the old serializers wrote.</summary>
    internal static class LegacyJson
    {
        /// <summary>
        /// Reads a required date that is either ISO 8601 or the
        /// DataContractJsonSerializer form "/Date(1737120652123+0100)/", and returns
        /// it as a local-time JSON value.
        /// </summary>
        public static JsonNode Date(JsonObject owner, string field)
        {
            var text = RequiredString(owner, field);
            if (text.Trim().Length == 0)
                throw new FormatException($"Missing date '{field}'.");

            return JsonValue.Create(ParseDate(text, field));
        }

        /// <summary>
        /// Reads a field that must be a JSON string. A missing field, null or another
        /// JSON type (number, object ...) is a <see cref="FormatException"/>.
        /// </summary>
        public static string RequiredString(JsonObject owner, string field)
        {
            if (!owner.TryGetPropertyValue(field, out var node) || node == null)
                throw new FormatException($"The field '{field}' is missing or null.");
            if (node is JsonValue value && value.TryGetValue(out string? text))
                return text;
            throw new FormatException($"The field '{field}' is not text: {node.ToJsonString()}");
        }

        public static void NullToEmptyString(JsonObject owner, string field)
        {
            // Old files can have null (DataContractJsonSerializer) or no value
            // (files written before the field existed). Both mean "not entered".
            if (owner[field] == null) owner[field] = string.Empty;
        }

        private static DateTime ParseDate(string text, string field)
        {
            const string prefix = "/Date(";
            if (text.StartsWith(prefix, StringComparison.Ordinal) && text.EndsWith(")/", StringComparison.Ordinal))
            {
                // The zone suffix (+0100) is only display information; the number is UTC milliseconds.
                var inner = text.Substring(prefix.Length, text.Length - prefix.Length - 2);
                int zone = inner.IndexOfAny(new[] { '+', '-' }, 1);
                var millis = zone > 0 ? inner.Substring(0, zone) : inner;
                if (long.TryParse(millis, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long ms))
                    return DateTimeOffset.FromUnixTimeMilliseconds(ms).LocalDateTime;
            }
            else if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var iso))
            {
                return iso.LocalDateTime;
            }

            throw new FormatException($"Date '{field}' has an unknown format: {text}");
        }
    }
}
