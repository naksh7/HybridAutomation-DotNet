using System.Speech.Synthesis;

namespace HybridAutomation.Helpers
{
    /// <summary>
    /// Text-to-speech utility providing audio feedback for test execution with configurable voice settings and async speech synthesis capabilities.
    /// </summary>
#pragma warning disable CA1416
    public class Speech : IDisposable
    {
        private SpeechSynthesizer? _synthesizer = null;
        private bool _disposed = false;
        private bool _isStarted = false;

        /// <summary>
        /// Initializes the speech synthesizer with configurable volume and speech rate settings.
        /// </summary>
        /// <param name="volume">Voice volume level from 0 to 100 (default: 100)</param>
        /// <param name="speechRate">Speech rate from -10 to 10 (default: 5)</param>
        public void StartLogging(int volume = 100, int speechRate = 5)
        {
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.Rate = speechRate; // Medium speed (range: -10 to 10)
            _synthesizer.Volume = volume; // Full volume (range: 0 to 100)  
            _isStarted = Utilities.EnvironmentSetup.StartAudioService();
        }

        /// <summary>
        /// Converts text to speech synchronously if audio service is available.
        /// </summary>
        /// <param name="text">Text content to be spoken aloud</param>
        public void SpeakText(string text)
        {
            try
            {
                if (_isStarted)
                    _synthesizer?.Speak(text);
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Converts text to speech asynchronously using Task.Run for non-blocking operation.
        /// </summary>
        /// <param name="text">Text content to be spoken aloud asynchronously</param>
        /// <returns>Task representing the asynchronous speech operation</returns>
        public async Task SpeakTextAsync(string text)
        {
            try
            {
                if (_isStarted)
                    await Task.Run(() => _synthesizer?.Speak(text));
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Provides immediate text-to-speech functionality with automatic initialization and cleanup.
        /// </summary>
        /// <param name="text">Text content to be spoken immediately</param>
        /// <returns>Task representing the complete speech operation including setup and disposal</returns>
        public void InstantSpeak(string text)
        {
            try
            {
                StartLogging();
                if (_isStarted)
                    _synthesizer?.Speak(text);
                Dispose();
            }
            catch (Exception)
            {

            }
            Dispose();
        }

        /// <summary>
        /// Releases all resources used by the Speech class and suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases managed resources including the SpeechSynthesizer instance.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _synthesizer?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}