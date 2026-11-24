using System;
using System.Collections.Generic;
using System.Linq;

namespace ModelessForms.IssuesManager.Model
{
    /// <summary>
    /// One documented model problem: a description, the Revit elements it is about,
    /// and screenshots. Persisted inside its <see cref="IssueCollection"/>.
    /// </summary>
    public sealed class Issue
    {
        /// <summary>Stable unique id. Also the prefix of the issue's screenshot file names.</summary>
        public required string Id { get; init; }

        /// <summary>Sequential number inside the collection (1, 2, 3 ...). Never reused.</summary>
        public required int Number { get; init; }

        public string Description { get; set; } = string.Empty;

        /// <summary>Revit <c>Element.UniqueId</c> values. Stable across sessions, unlike ElementId.</summary>
        public List<string> ElementUniqueIds { get; init; } = new List<string>();

        /// <summary>Screenshot file names in the collection's images folder, in display order.</summary>
        public List<string> Screenshots { get; init; } = new List<string>();

        public required DateTime Created { get; init; }

        public DateTime Modified { get; set; }

        private const int TitleLength = 60;

        /// <summary>"#7 – first line of the description", for the issue list and the PDF (spec D9).</summary>
        public string FormatTitle()
        {
            var firstLine = Description
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .FirstOrDefault(l => l.Length > 0);

            if (firstLine == null) return $"#{Number} – (no description)";
            if (firstLine.Length > TitleLength) firstLine = firstLine.Substring(0, TitleLength) + "…";
            return $"#{Number} – {firstLine}";
        }

        internal static string NewId(DateTime now) =>
            $"issue_{now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";
    }
}
