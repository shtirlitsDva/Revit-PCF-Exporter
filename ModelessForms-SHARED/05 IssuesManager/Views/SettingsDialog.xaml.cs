using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using ModelessForms.IssuesManager.Model;
using Shared.Theme;

namespace ModelessForms.IssuesManager.Views
{
    /// <summary>Edits a copy of the settings. <see cref="Result"/> is set when the user saves.</summary>
    public partial class SettingsDialog : Window
    {
        private const string DefaultMicrophone = "Windows default microphone";
        private const string NotConnectedSuffix = "  (not connected)";

        private readonly IssueTrackerSettings _settings;

        public SettingsDialog(IssueTrackerSettings current, IReadOnlyList<string> microphones)
        {
            ThemeManager.Apply(this);
            InitializeComponent();
            _settings = current.Clone();

            FolderBox.Text = _settings.StorageFolder;
            KeyBox.Password = _settings.AzureSpeechKey;
            RegionBox.Text = _settings.AzureSpeechRegion;

            var micItems = new List<string> { DefaultMicrophone };
            micItems.AddRange(microphones);
            string selectedMic = DefaultMicrophone;
            if (_settings.MicrophoneName.Length > 0)
            {
                // Keep a saved microphone visible even when it is unplugged, so the
                // setting is not lost silently.
                selectedMic = microphones.Contains(_settings.MicrophoneName)
                    ? _settings.MicrophoneName
                    : _settings.MicrophoneName + NotConnectedSuffix;
                if (!micItems.Contains(selectedMic)) micItems.Add(selectedMic);
            }
            MicrophoneBox.ItemsSource = micItems;
            MicrophoneBox.SelectedItem = selectedMic;

            LanguageBox.ItemsSource = SpeechLanguage.All;
            LanguageBox.SelectedValue = _settings.SpeechLanguage;
        }

        /// <summary>The edited settings after Save; null when cancelled.</summary>
        public IssueTrackerSettings? Result { get; private set; }

        private void OnBrowse(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Folder for the issue collections",
                InitialDirectory = Directory.Exists(FolderBox.Text) ? FolderBox.Text : string.Empty,
            };
            if (dialog.ShowDialog(this) == true) FolderBox.Text = dialog.FolderName;
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            var folder = FolderBox.Text.Trim();
            if (folder.Length == 0 || !Directory.Exists(folder))
            {
                ShowError("Choose an existing storage folder.");
                return;
            }
            if (LanguageBox.SelectedValue == null)
            {
                ShowError("Choose a speech language.");
                return;
            }

            var mic = (string)MicrophoneBox.SelectedItem;
            if (mic == DefaultMicrophone) mic = string.Empty;
            else if (mic.EndsWith(NotConnectedSuffix)) mic = mic.Substring(0, mic.Length - NotConnectedSuffix.Length);

            _settings.StorageFolder = folder;
            _settings.AzureSpeechKey = KeyBox.Password.Trim();
            _settings.AzureSpeechRegion = RegionBox.Text.Trim();
            _settings.MicrophoneName = mic;
            _settings.SpeechLanguage = (string)LanguageBox.SelectedValue;

            Result = _settings;
            DialogResult = true;
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
