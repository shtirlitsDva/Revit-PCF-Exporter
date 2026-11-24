using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using ModelessForms.IssuesManager.ViewModels;
using NAudio.CoreAudioApi;

namespace ModelessForms.IssuesManager.Dictation
{
    /// <summary>
    /// Continuous speech-to-text with Azure Cognitive Services. Start/Stop are
    /// called on the UI thread. SDK events arrive on SDK threads: recognized text is
    /// collected under a lock, and an error that ends the session is posted to the
    /// UI thread as <see cref="Faulted"/>.
    /// </summary>
    public sealed class AzureDictation : IDictation
    {
        private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(10);

        private readonly object _textLock = new object();
        private readonly StringBuilder _text = new StringBuilder();
        private SpeechRecognizer? _recognizer;
        private AudioConfig? _audio;
        private TaskCompletionSource<bool>? _sessionStopped;
        private SynchronizationContext? _ui;
        private volatile bool _stopping; // read on the Speech SDK thread (OnCanceled)

        public bool IsRecording => _recognizer != null;

        public event Action<DictationException>? Faulted;

        /// <summary>Friendly names of the active capture devices.</summary>
        public IReadOnlyList<string> GetMicrophoneNames()
        {
            using var enumerator = new MMDeviceEnumerator();
            var names = new List<string>();
            foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
            {
                using (device) names.Add(device.FriendlyName); // MMDevice holds a COM reference
            }
            return names;
        }

        public async Task StartAsync(DictationOptions options)
        {
            if (_recognizer != null) throw new DictationException("Dictation is already running.");
            if (string.IsNullOrWhiteSpace(options.AzureKey) || string.IsNullOrWhiteSpace(options.AzureRegion))
                throw new DictationException("Azure Speech is not set up. Enter the key and the region in Settings.");
            if (string.IsNullOrWhiteSpace(options.Language))
                throw new DictationException("No speech language is set. Choose one in Settings.");

            // A recording error can only reach the user through Faulted, on the UI
            // thread. Refuse to start when that path is missing, instead of dropping
            // errors later (review item 9, user ruling 2026-11-24).
            if (Faulted == null)
                throw new InvalidOperationException("AzureDictation: subscribe to Faulted before StartAsync.");
            _ui = SynchronizationContext.Current
                  ?? throw new InvalidOperationException("AzureDictation: StartAsync must be called on the UI thread.");
            lock (_textLock) _text.Clear();
            _stopping = false;

            try
            {
                var config = SpeechConfig.FromSubscription(options.AzureKey, options.AzureRegion);
                config.SpeechRecognitionLanguage = options.Language;

                _audio = string.IsNullOrEmpty(options.MicrophoneName)
                    ? AudioConfig.FromDefaultMicrophoneInput()
                    : AudioConfig.FromMicrophoneInput(MicrophoneDeviceId(options.MicrophoneName));

                _recognizer = new SpeechRecognizer(config, _audio);
                _sessionStopped = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _recognizer.Recognized += OnRecognized;
                _recognizer.Canceled += OnCanceled;
                _recognizer.SessionStopped += OnSessionStopped;

                await _recognizer.StartContinuousRecognitionAsync().ConfigureAwait(true);
            }
            catch (DictationException)
            {
                Cleanup();
                throw;
            }
            catch (Exception ex)
            {
                Cleanup();
                throw new DictationException("Dictation could not start: " + ex.Message, ex);
            }
        }

        public async Task<string> StopAsync()
        {
            if (_recognizer == null) throw new DictationException("Dictation is not running.");
            _stopping = true;
            try
            {
                await _recognizer.StopContinuousRecognitionAsync().ConfigureAwait(true);
                var stopped = _sessionStopped!.Task;
                if (await Task.WhenAny(stopped, Task.Delay(StopTimeout)).ConfigureAwait(true) != stopped)
                    throw new DictationException(
                        "The speech service did not confirm the end of the recording in time. " +
                        "The text recognized so far is kept.", null, CollectedText());
                return CollectedText();
            }
            catch (DictationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DictationException("Dictation did not stop correctly: " + ex.Message, ex, CollectedText());
            }
            finally
            {
                Cleanup();
            }
        }

        public void Dispose() => Cleanup();

        private void OnRecognized(object? sender, SpeechRecognitionEventArgs e)
        {
            if (e.Result.Reason != ResultReason.RecognizedSpeech || string.IsNullOrWhiteSpace(e.Result.Text)) return;
            lock (_textLock)
            {
                if (_text.Length > 0) _text.Append(' ');
                _text.Append(e.Result.Text);
            }
        }

        private void OnCanceled(object? sender, SpeechRecognitionCanceledEventArgs e)
        {
            if (e.Reason != CancellationReason.Error || _stopping) return;

            var error = new DictationException($"Dictation stopped because of an error: {e.ErrorDetails} ({e.ErrorCode})");
            // StartAsync guarantees a UI context and a subscriber; the view model
            // unsubscribes only after the recording has stopped.
            _ui!.Post(_ => Faulted?.Invoke(error), null);
        }

        private void OnSessionStopped(object? sender, SessionEventArgs e) => _sessionStopped?.TrySetResult(true);

        private string CollectedText()
        {
            lock (_textLock) return _text.ToString();
        }

        private static string MicrophoneDeviceId(string friendlyName)
        {
            using var enumerator = new MMDeviceEnumerator();
            string? id = null;
            foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
            {
                using (device) // MMDevice holds a COM reference
                {
                    if (id == null && device.FriendlyName == friendlyName) id = device.ID;
                }
            }
            return id ?? throw new DictationException(
                $"The microphone '{friendlyName}' is not connected. Connect it, or choose another microphone in Settings.");
        }

        private void Cleanup()
        {
            if (_recognizer != null)
            {
                _recognizer.Recognized -= OnRecognized;
                _recognizer.Canceled -= OnCanceled;
                _recognizer.SessionStopped -= OnSessionStopped;
                _recognizer.Dispose();
                _recognizer = null;
            }
            _audio?.Dispose();
            _audio = null;
        }
    }
}
