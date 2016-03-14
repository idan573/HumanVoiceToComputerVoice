using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Speech.Synthesis;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Shapes;
using Microsoft.ProjectOxford.SpeechRecognition;

namespace HumanVoiceToComputerVoice
{
    public partial class MainWindow : Window
    {
        private SpeechRecognitionHandler _speechRecognition;
        public ErrorsHandler ErrorHandler { get; set; }

        #region Error Handler

        public class ErrorsHandler
        {
            public ErrorsHandler()
            {
                Is64 = Environment.Is64BitProcess;
                IsInternetValid = CheckInternetConnection();
            }
            private bool CheckInternetConnection()
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        using (var stream = client.OpenRead("http://www.google.com"))
                        {
                            return true;
                        }
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
            public bool IsMicConnected { get; set; } = true;
            public bool Is64 { get; }
            public bool IsInternetValid { get; }
            public bool IsPrimaryKeyIsValid { get; set; } = true;

            public bool CanConnect
            {
                get { return IsMicConnected && Is64 && IsInternetValid && IsPrimaryKeyIsValid; }
            }
        }

        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnMice(object sender, RoutedEventArgs e)
        {
            BtMicrophone.IsEnabled = false;
            BtComputerSpeak.IsEnabled = false;

            _speechRecognition.StartSpeechAndRecognition();
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
            SpeechSynthesizer reader = new SpeechSynthesizer();

            if (!string.IsNullOrEmpty(_speechRecognition.RecognizedToken))
                reader.SpeakAsync(_speechRecognition.RecognizedToken);
        }
        private void OnLoad(object sender, RoutedEventArgs e)
        {
            ErrorHandler = new ErrorsHandler();

            if (ErrorHandler.Is64 && ErrorHandler.IsInternetValid)
                _speechRecognition = new SpeechRecognitionHandler(this);

            BtComputerSpeak.IsEnabled = false;

            if (!ErrorHandler.CanConnect)
            {
                TxtConnectedStatus.Content = "Not Connected";

                TxtConnectedStatus.Foreground = new SolidColorBrush(Colors.Red);

                EllipseStatusIndicator.Fill = new SolidColorBrush(Colors.Red);

                return;
            }

            TxtConnectedStatus.Content = "Connected";
            TxtConnectedStatus.Foreground = new SolidColorBrush(Colors.Green);
            EllipseStatusIndicator.Fill = new SolidColorBrush(Colors.Green);
        }
        private void OnInfo(object sender, RoutedEventArgs e)
        {
            string message;
            if (!ErrorHandler.Is64)
            {
                message = "Please use x64 device";
            }
            else if (!ErrorHandler.IsInternetValid)
            {
                message = "No internet connection";
            }
            else if (!ErrorHandler.IsPrimaryKeyIsValid)
            {
                message = "Primary key is invalid";
            }
            else if (!ErrorHandler.IsMicConnected)
            {
                message = "Please set default microphone";
            }
            else
            {
                message = "connected";
            }

            MessageBox.Show(message);
        }
    }
}
