using System;
using System.Configuration;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Microsoft.ProjectOxford.SpeechRecognition;

namespace HumanVoiceToComputerVoice
{
    public partial class MainWindow : Window
    {
        private string _recoLanguage = "en-us";
        private readonly string _subscriptionKey = ConfigurationManager.AppSettings["primaryKey"];
        private string _recognizedToken;
        private MicrophoneRecognitionClient _micClient;
        
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnMice(object sender, RoutedEventArgs e)
        {
            BtMicrophone.IsEnabled = false;
            BtComputerSpeak.IsEnabled = false;

            if (_micClient == null)
            {
                if (TextBoxLongDict.IsChecked != null && !TextBoxLongDict.IsChecked.Value)
                {
                    _micClient = CreateMicrophoneRecoClient(SpeechRecognitionMode.ShortPhrase, _recoLanguage, _subscriptionKey);
                }
                else
                {
                    _micClient = CreateMicrophoneRecoClient(SpeechRecognitionMode.LongDictation, _recoLanguage, _subscriptionKey);
                }
            }

            _micClient.StartMicAndRecognition();

        }

        MicrophoneRecognitionClient CreateMicrophoneRecoClient(SpeechRecognitionMode recoMode, string language, string subscriptionKey)
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

        void OnMicShortPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            var callback = (Action)(() =>
            {
                Console.WriteLine("--- OnMicShortPhraseResponseReceivedHandler ---");

                _micClient.EndMicAndRecognition();

                _micClient.Dispose();
                _micClient = null;

                WriteResponseResult(e);

                UpdateUi();
            });

            Dispatcher.Invoke(callback);
        }

        private void UpdateUi()
        {
            TextRecordedToken.Text = _recognizedToken;

            if (!string.IsNullOrEmpty(_recognizedToken))
            {
                BtComputerSpeak.IsEnabled = true;
            }

            BtMicrophone.IsEnabled = true;
        }

        void OnMicLongPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            Console.WriteLine("--- OnMicLongPhraseResponseReceivedHandler ---");
            if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.EndOfDictation ||
                e.PhraseResponse.RecognitionStatus == RecognitionStatus.DictationEndSilenceTimeout)
            {
                Dispatcher.Invoke((Action)(() =>
                {
                    UpdateUi();

                    // we got the final result, so it we can end the mic reco.  No need to do this
                    // for dataReco, since we already called endAudio() on it as soon as we were done
                    // sending all the data.
                }));
            }
            WriteResponseResult(e);
        }

        void OnMicrophoneStatus(object sender, MicrophoneEventArgs e)
        {
            Console.WriteLine("--- Microphone status change received by OnMicrophoneStatus() ---");
            Console.WriteLine("********* Microphone status: {0} *********", e.Recording);
            if (e.Recording)
            {
                Console.WriteLine("Please start speaking.");
            }
            Console.WriteLine();
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

                _recognizedToken = recognizedPhrases[0].DisplayText; 

                Console.WriteLine();
            }
        }

        void OnPartialResponseReceivedHandler(object sender, PartialSpeechResponseEventArgs e)
        {
            Console.WriteLine("--- Partial result received by OnPartialResponseReceivedHandler() ---");
            Console.WriteLine("{0}", e.PartialResult);
            Console.WriteLine();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnPlayButton(object sender, RoutedEventArgs e)
        {
            SpeechSynthesizer  reader = new SpeechSynthesizer();

            if(!string.IsNullOrEmpty(_recognizedToken))
                reader.SpeakAsync(_recognizedToken);
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            BtComputerSpeak.IsEnabled = false;
        }

    }
}
