using System.Windows;
using Emux.Properties;

namespace Emux
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            DeviceManager = new DeviceManager();
            Settings.Default.Reload();
        }
        
        public new static App Current
        {
            get { return (App) Application.Current; }
        }

        public DeviceManager DeviceManager
        {
            get;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Settings.Default.Save();
            base.OnExit(e);
        }
    }
}
