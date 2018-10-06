using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Emux.NAudio;
using Microsoft.Win32;

namespace Emux.Gui
{
    /// <summary>
    /// Interaction logic for AudioMixerWindow.xaml
    /// </summary>
    public partial class AudioMixerWindow : Window
    {
        public static readonly DependencyProperty MixerProperty = DependencyProperty.Register(
            "Mixer", typeof(GameBoyNAudioMixer), typeof(AudioMixerWindow), new PropertyMetadata(default(GameBoyNAudioMixer)));

        public static readonly RoutedUICommand StartRecordingCommand = new RoutedUICommand(
            "Start a new audio recording.",
            "Start Recording",
            typeof(AudioMixerWindow));

        public static readonly RoutedUICommand StopRecordingCommand = new RoutedUICommand(
            "Stop the current audio recording.",
            "Stop Recording",
            typeof(AudioMixerWindow));

        private Stream _outputStream;
        public AudioMixerWindow()
        {
            InitializeComponent();
        }
        
        public GameBoyNAudioMixer Mixer
        {
            get { return (GameBoyNAudioMixer) GetValue(MixerProperty); }
            set { SetValue(MixerProperty, value); }
        }

        private void CommandBindingOnCanAlwaysExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CloseCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void AudioMixerWindowOnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = Mixer != null;
            Hide();
        }
        
        private void SaveAsCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.Filter = "Wave files (*.wav)|*.wav";
            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                FileTextBox.Text = dialog.FileName;
            }
        }

        private void StartRecordingCommandOnExecuted(object sender, RoutedEventArgs routedEventArgs)
        {
            string fileName = FileTextBox.Text;
            string directory = Path.GetDirectoryName(fileName);
            string name = Path.GetFileNameWithoutExtension(fileName);

            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                MessageBox.Show("Please specify an output path.");
            }
            else
            {
                int x = 0;
                while (File.Exists(fileName = Path.Combine(directory, name + x + Path.GetExtension(fileName))))
                {
                    x++;
                }

                _outputStream = File.Create(fileName);
                Mixer.StartRecording(_outputStream);
            }
        }

        private void StopRecordingCommandOnExecuted(object sender, RoutedEventArgs routedEventArgs)
        {
            Mixer.StopRecording();
        }

        private void StartRecordingCommandOnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Mixer != null && !Mixer.IsRecording;
        }

        private void StopRecordingCommandOnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Mixer != null && Mixer.IsRecording;
        }
    }
}
