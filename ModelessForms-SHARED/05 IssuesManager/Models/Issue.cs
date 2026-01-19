using System;
using System.Collections.Generic;

namespace ModelessForms.IssuesManager.Models
{
    public class Issue
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public List<string> ElementGuids { get; set; }
        public List<string> Screenshots { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }

        public Issue()
        {
            Id = $"issue_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            Description = string.Empty;
            ElementGuids = new List<string>();
            Screenshots = new List<string>();
            Created = DateTime.Now;
            Modified = DateTime.Now;
        }

        public string GetDisplayTitle()
        {
            if (string.IsNullOrWhiteSpace(Description))
                return $"Issue #{Id.Substring(6, 8)}";

            var truncated = Description.Length > 30
                ? Description.Substring(0, 30) + "..."
                : Description;
            return truncated.Replace("\r", " ").Replace("\n", " ");
        }
    }
}
