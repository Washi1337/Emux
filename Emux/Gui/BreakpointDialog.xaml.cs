using System;
using System.Data;
using System.Windows;
using Emux.Expressions;
using Emux.GameBoy.Cpu;

namespace Emux.Gui
{
    /// <summary>
    /// Interaction logic for BreakpointDialog.xaml
    /// </summary>
    public partial class BreakpointDialog : Window
    {
        private readonly BreakpointInfo _info;

        public BreakpointDialog(BreakpointInfo info)
        {
            _info = info;
            InitializeComponent();
            AddressTextBox.Text = info.Address.ToString("X4");
            ConditionTextBox.Text = info.ConditionString;
        }

        private void OkButtonOnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _info.ConditionString = ConditionTextBox.Text;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Parser error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButtonOnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
