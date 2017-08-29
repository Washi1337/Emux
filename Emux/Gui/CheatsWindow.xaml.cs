using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Emux.GameBoy.Cheating;

namespace Emux.Gui
{
    /// <summary>
    /// Interaction logic for CheatsWindow.xaml
    /// </summary>
    public partial class CheatsWindow : Window
    {
        public static readonly DependencyProperty GamesharkControllerProperty = DependencyProperty.Register(
            "GamesharkController", typeof(GamesharkController), typeof(CheatsWindow), new PropertyMetadata(default(GamesharkController)));

        public CheatsWindow()
        {
            InitializeComponent();
        }

        public GamesharkController GamesharkController
        {
            get { return (GamesharkController) GetValue(GamesharkControllerProperty); }
            set { SetValue(GamesharkControllerProperty, value); }
        }

        private void CommandBindingOnCanAlwaysExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CloseCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void NewCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            bool exit = false;
            string text = "00000000";
            while (!exit)
            {
                try
                {
                    var dialog = new InputDialog();
                    dialog.Title = "Enter gameshark code";
                    dialog.Text = text;
                    bool? result = dialog.ShowDialog();
                    if (!result.HasValue || !result.Value)
                    {
                        exit = true;
                    }
                    else
                    {
                        text = dialog.Text.Replace(" ", "");

                        if (text.Length != 8)
                            throw new ArgumentException("Gameshark codes are 8 characters long.");

                        byte[] rawcode = new byte[4];
                        for (int i = 0; i < 4; i++)
                        {
                            byte value;
                            if (!byte.TryParse(text.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
                                throw new ArgumentException("Gameshark codes can only contain hexadecimal representation of bytes.");

                            rawcode[i] = value;
                        }
                        
                        GamesharkController.Codes.Add(new GamesharkCode(rawcode));
                        exit = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Invalid gameshark code. " + ex.Message, "Emux", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void DeleteCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            lock (GamesharkController.Codes)
            {
                var items = new GamesharkCode[GamesharkCodesView.SelectedItems.Count];
                GamesharkCodesView.SelectedItems.CopyTo(items, 0);
                foreach (var item in items)
                    GamesharkController.Codes.Remove(item);
            }
        }

        private void CheatsWindowOnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = GamesharkController != null;
            Hide();
        }
    }
}
