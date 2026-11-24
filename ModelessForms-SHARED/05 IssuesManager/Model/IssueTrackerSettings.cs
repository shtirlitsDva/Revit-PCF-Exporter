namespace ModelessForms.IssuesManager.Model
{
    /// <summary>
    /// Per-user settings of the Issue Tracker. In memory the Azure key is plain
    /// text; the settings store encrypts it on disk.
    /// </summary>
    public sealed class IssueTrackerSettings
    {
        /// <summary>The folder that holds all collections. Empty until the user chooses one.</summary>
        public string StorageFolder { get; set; } = string.Empty;

        /// <summary>The collection that was open last time.</summary>
        public string LastCollection { get; set; } = string.Empty;

        public string AzureSpeechKey { get; set; } = string.Empty;

        public string AzureSpeechRegion { get; set; } = string.Empty;

        /// <summary>Microphone friendly name. Empty = the Windows default microphone.</summary>
        public string MicrophoneName { get; set; } = string.Empty;

        /// <summary>BCP-47 code for speech recognition, e.g. "da-DK".</summary>
        public string SpeechLanguage { get; set; } = "da-DK";

        public bool HasStorageFolder => !string.IsNullOrWhiteSpace(StorageFolder);

        public bool HasSpeechService =>
            !string.IsNullOrWhiteSpace(AzureSpeechKey) && !string.IsNullOrWhiteSpace(AzureSpeechRegion);

        public IssueTrackerSettings Clone() => (IssueTrackerSettings)MemberwiseClone();
    }
}
