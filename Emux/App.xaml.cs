using System.Windows;

namespace Emux
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            DeviceManager = new DeviceManager();
        }

        public new static App Current
        {
            get { return (App) Application.Current; }
        }

        public DeviceManager DeviceManager
        {
            get;
        }
    }
}
