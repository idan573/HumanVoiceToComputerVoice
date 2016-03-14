using System;
using System.Configuration;
using Microsoft.ProjectOxford.SpeechRecognition;

namespace HumanVoiceToComputerVoice
{
    #region SpeechRecognitionHandler

    internal class SpeechRecognitionHandler
    {
        public string RecognizedToken { get; private set; }

        private const string RecoLanguage = "en-us";
        private readonly string _subscriptionKey = ConfigurationManager.AppSettings["primaryKey"];
        private MicrophoneRecognitionClient _micClient;
        private readonly MainWindow _mainWindow;

        internal SpeechRecognitionHandler(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;

            var errorHandler = mainWindow.ErrorHandler;
            if (_subscriptionKey == null || !errorHandler.IsInternetValid || !errorHandler.Is64)
            {
                errorHandler.IsPrimaryKeyIsValid = false;
                return;
            }

            try
            {
                if (_micClient != null)
                    return;

                if (mainWindow.TextBoxLongDict.IsChecked != null && !mainWindow.TextBoxLongDict.IsChecked.Value)
                {
                    _micClient = CreateMicrophoneRecoClient(SpeechRecognitionMode.ShortPhrase, RecoLanguage,
                        _subscriptionKey);
                }
                else
                {
                    _micClient = CreateMicrophoneRecoClient(SpeechRecognitionMode.LongDictation, RecoLanguage,
                        _subscriptionKey);
                }
            }
            catch
            {
                errorHandler.IsMicConnected = false;
            }
        }

        public void StartSpeechAndRecognition()
        {
            _micClient.StartMicAndRecognition();
        }

        private MicrophoneRecognitionClient CreateMicrophoneRecoClient(SpeechRecognitionMode recoMode, string language,
            string subscriptionKey)
        {
            MicrophoneRecognitionClient micClient = SpeechRecognitionServiceFactory.CreateMicrophoneClient(
                recoMode,
                language,
                subscriptionKey);

            // Event handlers for speech recognition results
            micClient.OnMicrophoneStatus += OnMicrophoneStatus;
            micClient.OnPartialResponseReceived += OnPartialResponseReceivedHandler;

            if (recoMode == SpeechRecognitionMode.ShortPhrase)
            {
                micClient.OnResponseReceived += OnMicShortPhraseResponseReceivedHandler;
            }
            else
            {
                micClient.OnResponseReceived += OnMicLongPhraseResponseReceivedHandler;
            }

            return micClient;
        }

        private void OnMicShortPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            _mainWindow.Dispatcher.Invoke((Action)(() =>
            {
                Console.WriteLine("--- OnMicShortPhraseResponseReceivedHandler ---");

                _micClient.EndMicAndRecognition();

                _micClient.Dispose();
                _micClient = null;

                WriteResponseResult(e);

                UpdateUi();
            }));
        }

        private void UpdateUi()
        {
            _mainWindow.TextRecordedToken.Text = RecognizedToken;

            if (!string.IsNullOrEmpty(RecognizedToken))
            {
                _mainWindow.BtComputerSpeak.IsEnabled = true;
            }

            _mainWindow.BtMicrophone.IsEnabled = true;
        }

        private void OnMicLongPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            Console.WriteLine("--- OnMicLongPhraseResponseReceivedHandler ---");
            if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.EndOfDictation ||
                e.PhraseResponse.RecognitionStatus == RecognitionStatus.DictationEndSilenceTimeout)
            {
                _mainWindow.Dispatcher.Invoke((Action)(() =>
                {
                    UpdateUi();

                    // we got the final result, so it we can end the mic reco.  No need to do this
                    // for dataReco, since we already called endAudio() on it as soon as we were done
                    // sending all the data.
                }));
            }
            WriteResponseResult(e);
        }

        private void OnMicrophoneStatus(object sender, MicrophoneEventArgs e)
        {
            _mainWindow.Dispatcher.Invoke(() =>
            {
                Console.WriteLine("--- Microphone status change received by OnMicrophoneStatus() ---");
                Console.WriteLine("********* Microphone status: {0} *********", e.Recording);
                if (e.Recording)
                {
                    Console.WriteLine("Please start speaking.");
                }
                Console.WriteLine();
            });
        }

        private void WriteResponseResult(SpeechResponseEventArgs e)
        {
            var recognizedPhrases = e.PhraseResponse.Results;
            var length = recognizedPhrases.Length;
            if (length == 0)
            {
                Console.WriteLine("No phrase resonse is available.");
            }
            else
            {
                Console.WriteLine("********* Final n-BEST Results *********");
                for (int i = 0; i < length; i++)
                {
                    Console.WriteLine("[{0}] Confidence={1}, Text=\"{2}\"",
                        i, recognizedPhrases[i].Confidence,
                        recognizedPhrases[i].DisplayText);
                }

                RecognizedToken = recognizedPhrases[0].DisplayText;

                Console.WriteLine();
            }
        }

        private void OnPartialResponseReceivedHandler(object sender, PartialSpeechResponseEventArgs e)
        {
            Console.WriteLine("--- Partial result received by OnPartialResponseReceivedHandler() ---");
            Console.WriteLine("{0}", e.PartialResult);
            Console.WriteLine();
        }
    }

    #endregion
}
