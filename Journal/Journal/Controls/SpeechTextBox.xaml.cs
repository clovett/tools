using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Text;
using Windows.Media.SpeechRecognition;
using Windows.UI.Popups;
using Windows.Media.SpeechSynthesis;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Microsoft.Journal.Controls
{
    public sealed partial class SpeechTextBox : UserControl
    {
        // This enum and these members are used for state management for the various buttons and input
        private enum InputState
        {
            ReadyForInput,
            ListeningForInput,
            TypingInput,
        };

        private InputState currentInputState;

        // State maintenance of the Speech Recognizer
        private SpeechRecognizer recognizer;
        private IAsyncOperation<SpeechRecognitionResult> currentRecognizerOperation;

        private SpeechSynthesizer synthesizer;

        public SpeechTextBox()
        {
            this.InitializeComponent();
            this.synthesizer = new SpeechSynthesizer();
            InitializeRecognizer();
            this.InternalTextBox.GotFocus += InternalTextBox_GotFocus;
            this.InternalTextBox.LostFocus += InternalTextBox_LostFocus;
            this.InternalTextBox.TextChanged += InternalTextBox_TextChanged;
        }

        void InternalTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.Text = this.InternalTextBox.Text;
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(SpeechTextBox), new PropertyMetadata(null, new PropertyChangedCallback(OnTextChanged)));

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SpeechTextBox)d).OnTextChanged();
        }

        private void OnTextChanged()
        {
            if (InternalTextBox.Text != this.Text)
            {
                InternalTextBox.Text = "" + this.Text;
            }
        }

        public event EventHandler<RoutedEventArgs> TextBoxLostFocus;
        public event EventHandler<RoutedEventArgs> TextBoxGotFocus;

        void InternalTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (TextBoxLostFocus != null)
            {
                TextBoxLostFocus(sender, e);
            }
        }

        void InternalTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TextBoxGotFocus != null)
            {
                TextBoxGotFocus(sender, e);
            }
        }

        /// <summary>
        /// Initializes the Speech Recognizer object and its completion handler, used for subsequent reco operations.
        /// </summary>
        private void InitializeRecognizer()
        {
            this.recognizer = new SpeechRecognizer();
        }

        private void OnTextInputGainedFocus(object sender, RoutedEventArgs e)
        {
            SetInputState(InputState.TypingInput);
        }

        private void OnTextInputLostFocus(object sender, RoutedEventArgs e)
        {
            if (this.currentInputState == InputState.TypingInput)
            {
                SetInputState(InputState.ReadyForInput);
            }
        }

        private void OnSpeechActionButtonTapped(object sender, PointerRoutedEventArgs e)
        {
            switch (this.currentInputState)
            {
                case InputState.ReadyForInput:
                    StartListening();
                    break;
                case InputState.ListeningForInput:
                    // The input bar is currently disabled and we may have an outstanding speech operation.
                    // There's unfortunately no way to manually endpoint (tell the recognizer "Hey, we're done!" and so
                    // we'll just cancel here.
                    if (this.currentRecognizerOperation != null)
                    {
                        // bugbug: this crashes due to bug in SpeechRecognizerAsyncOperation::OnCancel.
                        // this.currentRecognizerOperation.Cancel();
                        this.currentRecognizerOperation = null;
                        PlaySound("Assets/CancelledEarcon.wav");
                    }
                    else
                    {
                        SetInputState(InputState.ReadyForInput);
                    }
                    break;

            }

        }

        /// <summary>
        /// Sets the current state associated with the text box and button and performs the needed UI modifications
        /// associated with the new state
        /// </summary>
        /// <param name="newState"> the new state being selected </param>
        private void SetInputState(InputState newState)
        {
            this.currentInputState = newState;

            // Hide all of the possible button elements for the microphone icon; we'll restore the one we want momentarily.
            this.SpeechActionButtonMicrophone.Opacity = 0;
            this.SpeechActionButtonStop.Opacity = 0;
            this.SpeechActionButtonStopBorder.Opacity = 0;

            // Preemptively restore the absolute width of the text box; we'll resize it if needed.
            //this.InternalTextBox.Width = App.Current.Host.Content.ActualWidth - 66;

            switch (newState)
            {
                case InputState.ReadyForInput:
                    this.InternalTextBox.FontStyle = FontStyle.Normal;
                    this.InternalTextBox.IsEnabled = true;
                    this.SpeechActionButtonMicrophone.Opacity = 1;
                    this.SpeechActionButtonContainer.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    break;
                case InputState.ListeningForInput:
                    this.InternalTextBox.FontStyle = FontStyle.Italic;
                    this.InternalTextBox.IsEnabled = false;
                    this.SpeechActionButtonStop.Opacity = 1;
                    this.SpeechActionButtonStopBorder.Opacity = 1;
                    this.InternalTextBox.Width += SpeechActionButtonContainer.ActualWidth;
                    this.SpeechActionButtonContainer.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    break;
                case InputState.TypingInput:
                    this.InternalTextBox.Foreground = new SolidColorBrush(Colors.Black);
                    this.SpeechActionButtonContainer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;
            }
        }


        /// <summary>
        /// Generates a new recognition operation from the speech recognizer, hooks up its completion handler, and
        /// updates state as needed for the duration of the recognition operation. Also checks any errors for
        /// known user-actionable steps, like accepting the privacy policy before using in-app recognition.
        /// </summary>
        private async void StartListening()
        {
            try
            {
                // Start listening to the user and set up the completion handler for when the result
                PlaySound("Assets/ListeningEarcon.wav");
                await this.recognizer.CompileConstraintsAsync();
                this.currentRecognizerOperation = this.recognizer.RecognizeAsync();
                this.currentRecognizerOperation.Completed = new AsyncOperationCompletedHandler<SpeechRecognitionResult>(OnRecognitionCompleted);
                SetInputState(InputState.ListeningForInput);
            }
            catch (Exception recoException)
            {
                const int privacyPolicyHResult = unchecked((int)0x80045509);
                PlaySound("Assets/CancelledEarcon.wav");

                if (recoException.HResult == privacyPolicyHResult)
                {
                    ShowMessage(AppResources.SpeechPrivacyPolicyError, "Policy Error");
                }
                else
                {
                    // user may have cancelled when there was no text.
                }
            }
        }

        /// <summary>
        /// Uses a MediaElement to play a given sound effect
        /// </summary>
        /// <param name="path"> the relative path of the sound being played </param>
        private void PlaySound(string path)
        {
            SoundPlayer.Source = new Uri("ms-appx:///" + path);
            SoundPlayer.Play();
        }

        void OnRecognitionCompleted(IAsyncOperation<SpeechRecognitionResult> asyncInfo, AsyncStatus asyncStatus)
        {
            this.currentRecognizerOperation = null;

            var nowait = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {

                bool recognitionSuccessful = false;
                SetInputState(InputState.ReadyForInput);

                switch (asyncStatus)
                {
                    case AsyncStatus.Completed:
                        SpeechRecognitionResult result = asyncInfo.GetResults();
                        var constraint = result.Constraint;
                        if (!String.IsNullOrEmpty(result.Text))
                        {
                            recognitionSuccessful = true;
                            AcceptVoiceInput(result.Text, result);
                        }
                        break;
                    case AsyncStatus.Error:
                        ShowMessage(String.Format(
                            AppResources.SpeechRecognitionErrorTemplate,
                            asyncInfo.ErrorCode.HResult,
                            asyncInfo.ErrorCode.Message), "Recognition Error");
                        break;
                    default:
                        break;
                }

                if (!recognitionSuccessful)
                {
                    // For errors and cancellations, we'll revert back to the starting state
                }
            }));

        }

        private void AcceptVoiceInput(string voiceText, SpeechRecognitionResult result)
        {
            if (voiceText.EndsWith("."))
            {
                voiceText = voiceText.Substring(0, voiceText.Length - 1);
            }

            var nowait = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {
                InternalTextBox.Text = voiceText;
            }));
        }

        /// <summary>
        /// Initiates synthesis of a speech synthesis markup language (SSML) document, which allows for finer and more
        /// robust control than plain text.
        /// </summary>
        /// <param name="ssmlToSpeak"> The body fo the SSML document to be spoken </param>
        public async void StartSpeakingSsml(string ssmlToSpeak)
        {
            // Begin speaking using our synthesizer, wiring the completion event to stop tracking the action when it
            // finishes.
            try
            {
                var stream = await this.synthesizer.SynthesizeSsmlToStreamAsync(ssmlToSpeak);
                SoundPlayer.SetSource(stream, stream.ContentType);
                SoundPlayer.Play();
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, "Speech Error");
            }
        }

        private async void ShowMessage(string msg, string title)
        {
            var dialog = new MessageDialog(msg, title);
            dialog.Commands.Add(new UICommand("OK"));
            await dialog.ShowAsync();
        }
    }
}
