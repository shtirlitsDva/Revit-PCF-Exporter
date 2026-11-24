using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using ModelessForms.IssuesManager.Model;

namespace ModelessForms.IssuesManager.ViewModels
{
    /// <summary>One issue in the list and in the detail panel.</summary>
    public sealed class IssueViewModel : ObservableObject
    {
        private readonly Func<string, string> _screenshotPath;

        public IssueViewModel(Issue issue, Func<string, string> screenshotPath)
        {
            Model = issue;
            _screenshotPath = screenshotPath;
            Screenshots = new ObservableCollection<ScreenshotViewModel>(
                issue.Screenshots.Select(f => new ScreenshotViewModel(f, screenshotPath(f))));
        }

        public Issue Model { get; }

        public int Number => Model.Number;

        /// <summary>"#7  First line of the description", for the list and the PDF.</summary>
        public string Title => Model.FormatTitle();

        public string CreatedText => Model.Created.ToString("yyyy-MM-dd HH:mm");

        public string IdText => Model.Id;

        public IReadOnlyList<string> ElementUniqueIds => Model.ElementUniqueIds;

        public bool HasElements => Model.ElementUniqueIds.Count > 0;

        public ObservableCollection<ScreenshotViewModel> Screenshots { get; }

        /// <summary>Raised after the user changed the description (not on load).</summary>
        public event Action<IssueViewModel>? Edited;

        public string Description
        {
            get => Model.Description;
            set
            {
                value ??= string.Empty;
                if (value == Model.Description) return;
                Model.Description = value;
                Model.Modified = DateTime.Now;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Title));
                Edited?.Invoke(this);
            }
        }

        /// <summary>Appends dictated text on a new line.</summary>
        public void AppendText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            Description = Description.Length == 0 ? text : Description + Environment.NewLine + text;
        }

        internal void AddScreenshot(string fileName) =>
            Screenshots.Add(new ScreenshotViewModel(fileName, _screenshotPath(fileName)));
    }

    /// <summary>A screenshot thumbnail. <see cref="Exists"/> is false when the file is missing.</summary>
    public sealed class ScreenshotViewModel
    {
        public ScreenshotViewModel(string fileName, string path)
        {
            FileName = fileName;
            Path = path;
            Exists = File.Exists(path);
        }

        public string FileName { get; }
        public string Path { get; }
        public bool Exists { get; }
    }
}
