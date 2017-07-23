using System.Windows;
using System.Windows.Input;

namespace Emux
{
    /// <summary>
    /// Interaction logic for InputDialog.xaml
    /// </summary>
    public partial class InputDialog : Window
    {
        public InputDialog()
        {
            InitializeComponent();
            ContentsTextBox.SelectAll();
            ContentsTextBox.Focus();
        }

        public string Text
        {
            get { return ContentsTextBox.Text; }
        }
        
        private void CancelButtonOnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OkButtonOnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
