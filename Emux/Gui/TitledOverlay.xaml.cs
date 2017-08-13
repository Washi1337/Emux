using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Emux.Gui
{
    /// <summary>
    /// Interaction logic for DisabledOverlay.xaml
    /// </summary>
    public partial class TitledOverlay : UserControl
    {
        private readonly ManualResetEvent _cancel = new ManualResetEvent(false);

        public TitledOverlay()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            "Title", typeof(string), typeof(TitledOverlay), new PropertyMetadata(default(string)));

        public string Title
        {
            get { return (string) GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
            "Subtitle", typeof(string), typeof(TitledOverlay), new PropertyMetadata(default(string)));

        public string Subtitle
        {
            get { return (string) GetValue(SubtitleProperty); }
            set { SetValue(SubtitleProperty, value); }
        }

        public void EnableOverlay(int delay)
        {
            new Thread(() =>
            {
                _cancel.Reset();
                if (!_cancel.WaitOne(delay))
                    Dispatcher.Invoke(() => Visibility = Visibility.Visible);
            }).Start();
        }

        public void DisableOverlay()
        {
            _cancel.Set();
            Visibility = Visibility.Hidden;
        }
    }
}
