using System.Diagnostics;
using System.Windows;

namespace Emux
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutDialog : Window
    {
        public AboutDialog()
        {
            InitializeComponent();
            VersionLabel.Content = typeof(AboutDialog).Assembly.GetName().Version.ToString();
        }

        private void SourceCodeHyperlinkOnRequestNavigate(object sender, RoutedEventArgs routedEventArgs)
        {
            Process.Start(Properties.Settings.Default.Repository);
        }

        private void LicenseHyperlinkOnRequestNavigate(object sender, RoutedEventArgs routedEventArgs)
        {
            Process.Start(Properties.Settings.Default.Repository + "/blob/master/LICENSE");
        }
    }
}
