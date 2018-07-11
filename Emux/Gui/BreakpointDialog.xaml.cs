using System.Windows;

namespace Emux.Gui
{
    /// <summary>
    /// Interaction logic for BreakpointDialog.xaml
    /// </summary>
    public partial class BreakpointDialog : Window
    {
        public BreakpointDialog()
        {
            InitializeComponent();
        }

        private void OkButtonOnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CancelButtonOnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
