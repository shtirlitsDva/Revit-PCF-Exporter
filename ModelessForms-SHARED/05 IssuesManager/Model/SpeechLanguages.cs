using System.Collections.Generic;

namespace ModelessForms.IssuesManager.Model
{
    /// <summary>A language that Azure speech recognition supports, for the settings list.</summary>
    public sealed class SpeechLanguage
    {
        public SpeechLanguage(string code, string name) { Code = code; Name = name; }

        /// <summary>BCP-47 code, e.g. "da-DK".</summary>
        public string Code { get; }

        public string Name { get; }

        public override string ToString() => Name;

        public static readonly IReadOnlyList<SpeechLanguage> All = new[]
        {
            new SpeechLanguage("da-DK", "Danish"),
            new SpeechLanguage("en-US", "English (US)"),
            new SpeechLanguage("en-GB", "English (UK)"),
            new SpeechLanguage("de-DE", "German"),
            new SpeechLanguage("sv-SE", "Swedish"),
            new SpeechLanguage("nb-NO", "Norwegian"),
            new SpeechLanguage("fi-FI", "Finnish"),
            new SpeechLanguage("nl-NL", "Dutch"),
            new SpeechLanguage("fr-FR", "French"),
            new SpeechLanguage("es-ES", "Spanish"),
            new SpeechLanguage("it-IT", "Italian"),
            new SpeechLanguage("pl-PL", "Polish"),
            new SpeechLanguage("pt-BR", "Portuguese (Brazil)"),
            new SpeechLanguage("ru-RU", "Russian"),
            new SpeechLanguage("ja-JP", "Japanese"),
            new SpeechLanguage("zh-CN", "Chinese (Simplified)"),
            new SpeechLanguage("ko-KR", "Korean"),
        };
    }
}
