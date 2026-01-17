using System;

namespace ModelessForms.IssuesManager.Models
{
    public class Settings
    {
        public string BaseFolder { get; set; }
        public string DefaultCollection { get; set; }
        public string AzureSpeechKey { get; set; }
        public string AzureSpeechRegion { get; set; }
        public string MicrophoneName { get; set; }
        public string SpeechLanguage { get; set; }

        public Settings()
        {
            BaseFolder = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "IssueTracker");
            DefaultCollection = "Default";
            AzureSpeechKey = string.Empty;
            AzureSpeechRegion = string.Empty;
            MicrophoneName = string.Empty;
            SpeechLanguage = "da-DK";
        }
    }
}
