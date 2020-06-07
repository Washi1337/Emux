using Emux.GameBoy.Graphics;
using Emux.Properties;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Emux.Gui
{
    /// <summary>
    /// Interaction logic for OptionsDialog.xaml
    /// </summary>
    public partial class OptionsDialog : Window
    {
        private Binding _buttonContentBinding;

        public OptionsDialog()
        {
            InitializeComponent();

            UpdateScaleDD();
            VideoScaleDD.SelectionChanged += VideoScale_SelectionChanged;
        }

        private void UpdateScaleDD()
        {
            var configWidth = Settings.Default.VideoWidth;
            var configHeight = Settings.Default.VideoHeight;
            var widthScale = configWidth / (float)GameBoyGpu.FrameWidth;
            var heightScale = configHeight / (float)GameBoyGpu.FrameHeight;
            if (widthScale == heightScale && widthScale % 1 == 0 && heightScale % 1 == 0) // Exact scale
                VideoScaleDD.SelectedIndex = (int)widthScale;
            else
                VideoScaleDD.SelectedIndex = 0;
        }

        private void VideoScale_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var newScale = VideoScaleDD.SelectedIndex;
            if (newScale == 0)
                return;

            Settings.Default.VideoWidth = GameBoyGpu.FrameWidth * newScale;
            Settings.Default.VideoHeight = GameBoyGpu.FrameHeight * newScale;
        }

        public bool CanClose
        {
            get;
            set;
        }

        private void OptionsDialogOnClosing(object sender, CancelEventArgs e)
        {
            if (!CanClose)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void PaletteButtonOnClick(object sender, RoutedEventArgs e)
        {
            string settingName = "GBColor" + ((Button) sender).Tag;
            var color = (System.Windows.Media.Color)Properties.Settings.Default[settingName];

            using (var dialog = new System.Windows.Forms.ColorDialog())
            {
                dialog.FullOpen = true;
                dialog.CustomColors = new[]
                {
                    0xD0F8E0, 0x70C088, 0x566834, 0x201808
                };
                dialog.Color = System.Drawing.Color.FromArgb(color.R, color.G, color.B);
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Properties.Settings.Default[settingName] =
                        System.Windows.Media.Color.FromRgb(dialog.Color.R, dialog.Color.G, dialog.Color.B);
                }
            }

        }

        private void ResetToDefaultsButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                    "Are you sure you want to reset the settings to their default values?",
                    "Emux",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Properties.Settings.Default.Reset();
            }
        }

        private void KeyBindingButtonOnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button) sender;
            _buttonContentBinding = button.GetBindingExpression(ContentProperty).ParentBinding;
            button.Content = "[ Press a key ]";
        }

        private void KeyBindingButtonOnKeyDown(object sender, KeyEventArgs e)
        {
            if (_buttonContentBinding != null)
            {
                var button = (Button) sender;
                Properties.Settings.Default["KeyBinding" + button.Tag] = e.Key;
                button.SetBinding(ContentProperty, _buttonContentBinding);
                _buttonContentBinding = null;
            }
        }

        private void KeyBindingButtonOnLostFocus(object sender, RoutedEventArgs e)
        {
            if (_buttonContentBinding != null)
            {
                var button = (Button) sender;
                button.SetBinding(ContentProperty, _buttonContentBinding);
                _buttonContentBinding = null;
            }
        }
    }
}
