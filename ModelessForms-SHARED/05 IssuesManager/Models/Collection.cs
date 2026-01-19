using System;
using System.Collections.Generic;

namespace ModelessForms.IssuesManager.Models
{
    public class Collection
    {
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public List<Issue> Issues { get; set; }
        public string ProjectName { get; set; }
        public string AuthorName { get; set; }

        public Collection()
        {
            Name = "Default";
            Created = DateTime.Now;
            Issues = new List<Issue>();
            ProjectName = string.Empty;
            AuthorName = string.Empty;
        }

        public Collection(string name)
        {
            Name = name;
            Created = DateTime.Now;
            Issues = new List<Issue>();
            ProjectName = string.Empty;
            AuthorName = string.Empty;
        }
    }
}
