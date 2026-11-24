using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ModelessForms.IssuesManager.Model
{
    /// <summary>
    /// A named set of issues (for example one per project or per review round).
    /// Stored as one folder: <c>{StorageFolder}/{Name}/collection.json</c> + <c>images/</c>.
    /// </summary>
    public sealed class IssueCollection
    {
        /// <summary>
        /// The folder name. It is NOT stored in the file: the folder is the only
        /// source of the name. Set by the store on load and create.
        /// </summary>
        [JsonIgnore]
        public string Name { get; internal set; } = string.Empty;

        public required DateTime Created { get; init; }

        public string ProjectName { get; set; } = string.Empty;

        public string AuthorName { get; set; } = string.Empty;

        /// <summary>The number the next new issue gets. Numbers are never reused.</summary>
        [JsonInclude]
        public int NextIssueNumber { get; private set; } = 1;

        /// <summary>Issues in creation order (ascending <see cref="Issue.Number"/>).</summary>
        [JsonInclude]
        public List<Issue> Issues { get; private set; } = new List<Issue>();

        /// <summary>Adds a new, empty issue for the given Revit elements and returns it.</summary>
        public Issue AddIssue(IEnumerable<string> elementUniqueIds, DateTime now)
        {
            if (elementUniqueIds == null) throw new ArgumentNullException(nameof(elementUniqueIds));

            var issue = new Issue
            {
                Id = Issue.NewId(now),
                Number = NextIssueNumber,
                ElementUniqueIds = elementUniqueIds.ToList(),
                Created = now,
                Modified = now,
            };
            NextIssueNumber++;
            Issues.Add(issue);
            return issue;
        }

        /// <summary>
        /// A deep copy. Work on another thread (the PDF export) reads the copy, so the
        /// user can go on editing the original (spec M7).
        /// </summary>
        public IssueCollection Snapshot() => new IssueCollection
        {
            Name = Name,
            Created = Created,
            ProjectName = ProjectName,
            AuthorName = AuthorName,
            NextIssueNumber = NextIssueNumber,
            Issues = Issues.Select(i => new Issue
            {
                Id = i.Id,
                Number = i.Number,
                Description = i.Description,
                ElementUniqueIds = new List<string>(i.ElementUniqueIds),
                Screenshots = new List<string>(i.Screenshots),
                Created = i.Created,
                Modified = i.Modified,
            }).ToList(),
        };

        internal void RemoveIssue(Issue issue)
        {
            if (!Issues.Remove(issue))
                throw new InvalidOperationException($"Issue #{issue.Number} is not in collection '{Name}'.");
        }
    }
}
