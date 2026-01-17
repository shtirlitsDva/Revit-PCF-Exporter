using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.CoreAudioApi;

namespace ModelessForms.IssuesManager.Services
{
    public class MicrophoneInfo
    {
        public string FriendlyName { get; set; }
        public string DeviceId { get; set; }
    }

    public class SpeechService : IDisposable
    {
        private readonly string _azureKey;
        private readonly string _azureRegion;
        private readonly string _microphoneName;
        private readonly string _language;
        private SpeechRecognizer _recognizer;
        private AudioConfig _audioConfig;
        private StringBuilder _recognizedText;
        private TaskCompletionSource<string> _sessionCompletionSource;
        private bool _disposed;
        private bool _isRecording;

        public event Action<string> OnPartialResult;
        public event Action<string> OnFinalResult;
        public event Action<string> OnError;

        public bool IsRecording => _isRecording;

        public SpeechService(string azureKey, string azureRegion, string microphoneName = null, string language = "da-DK")
        {
            _azureKey = azureKey;
            _azureRegion = azureRegion;
            _microphoneName = microphoneName;
            _language = language;
        }

        public bool IsConfigured => !string.IsNullOrEmpty(_azureKey) && !string.IsNullOrEmpty(_azureRegion);

        public static List<MicrophoneInfo> GetAvailableMicrophones()
        {
            var microphones = new List<MicrophoneInfo>();
            try
            {
                using (var enumerator = new MMDeviceEnumerator())
                {
                    var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                    foreach (var device in devices)
                    {
                        microphones.Add(new MicrophoneInfo
                        {
                            FriendlyName = device.FriendlyName,
                            DeviceId = device.ID
                        });
                    }
                }
            }
            catch
            {
            }
            return microphones;
        }

        public static string GetDeviceIdByName(string friendlyName)
        {
            if (string.IsNullOrEmpty(friendlyName))
                return null;

            var microphones = GetAvailableMicrophones();
            var mic = microphones.FirstOrDefault(m => m.FriendlyName == friendlyName);
            return mic?.DeviceId;
        }

        public static List<(string Code, string Name)> GetSupportedLanguages()
        {
            return new List<(string, string)>
            {
                ("da-DK", "Danish"),
                ("en-US", "English (US)"),
                ("en-GB", "English (UK)"),
                ("de-DE", "German"),
                ("fr-FR", "French"),
                ("es-ES", "Spanish"),
                ("it-IT", "Italian"),
                ("nl-NL", "Dutch"),
                ("sv-SE", "Swedish"),
                ("nb-NO", "Norwegian"),
                ("fi-FI", "Finnish"),
                ("pl-PL", "Polish"),
                ("pt-BR", "Portuguese (Brazil)"),
                ("ja-JP", "Japanese"),
                ("zh-CN", "Chinese (Simplified)"),
                ("ko-KR", "Korean"),
                ("ru-RU", "Russian")
            };
        }

        public async Task StartRecordingAsync()
        {
            if (!IsConfigured)
                throw new InvalidOperationException("Azure Speech Service is not configured.");

            if (_isRecording)
                return;

            try
            {
                var config = SpeechConfig.FromSubscription(_azureKey, _azureRegion);
                config.SpeechRecognitionLanguage = _language;

                if (!string.IsNullOrEmpty(_microphoneName))
                {
                    var deviceId = GetDeviceIdByName(_microphoneName);
                    if (!string.IsNullOrEmpty(deviceId))
                        _audioConfig = AudioConfig.FromMicrophoneInput(deviceId);
                    else
                        _audioConfig = AudioConfig.FromDefaultMicrophoneInput();
                }
                else
                    _audioConfig = AudioConfig.FromDefaultMicrophoneInput();

                _recognizer = new SpeechRecognizer(config, _audioConfig);
                _recognizedText = new StringBuilder();
                _sessionCompletionSource = new TaskCompletionSource<string>();

                _recognizer.Recognizing += (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.RecognizingSpeech)
                        OnPartialResult?.Invoke(e.Result.Text);
                };

                _recognizer.Recognized += (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.RecognizedSpeech)
                    {
                        if (!string.IsNullOrEmpty(e.Result.Text))
                        {
                            if (_recognizedText.Length > 0)
                                _recognizedText.Append(" ");
                            _recognizedText.Append(e.Result.Text);
                        }
                    }
                };

                _recognizer.Canceled += (s, e) =>
                {
                    if (e.Reason == CancellationReason.Error)
                        OnError?.Invoke($"Recognition error: {e.ErrorDetails}");
                };

                _recognizer.SessionStopped += (s, e) =>
                {
                    _isRecording = false;
                    var finalText = _recognizedText.ToString();
                    _sessionCompletionSource?.TrySetResult(finalText);
                    OnFinalResult?.Invoke(finalText);
                };

                await _recognizer.StartContinuousRecognitionAsync();
                _isRecording = true;
            }
            catch (Exception ex)
            {
                _isRecording = false;
                OnError?.Invoke($"Failed to start recording: {ex.Message}");
                throw;
            }
        }

        public async Task<string> StopRecordingAsync()
        {
            if (!_isRecording || _recognizer == null)
                return string.Empty;

            try
            {
                await _recognizer.StopContinuousRecognitionAsync();
                var result = await _sessionCompletionSource.Task;
                return result;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Failed to stop recording: {ex.Message}");
                return _recognizedText?.ToString() ?? string.Empty;
            }
            finally
            {
                CleanupRecognizer();
            }
        }

        private void CleanupRecognizer()
        {
            _recognizer?.Dispose();
            _recognizer = null;
            _audioConfig?.Dispose();
            _audioConfig = null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CleanupRecognizer();
        }
    }
}
