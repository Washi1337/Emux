using System.Windows;

namespace Emux.Gui
{
    public class RegisterItem : DependencyObject
    {
        public event DependencyPropertyChangedEventHandler ValueChanged;

        public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register(
            "Offset", typeof(ushort), typeof(RegisterItem));

        public ushort Offset
        {
            get { return (ushort) GetValue(OffsetProperty); }
            set { SetValue(OffsetProperty, value); }
        }

        public static readonly DependencyProperty DisplayNameProperty = DependencyProperty.Register(
            "DisplayName", typeof(string), typeof(RegisterItem), new PropertyMetadata(default(string)));

        public string DisplayName
        {
            get { return (string) GetValue(DisplayNameProperty); }
            set { SetValue(DisplayNameProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(byte), typeof(RegisterItem), new PropertyMetadata((o, args) => ((RegisterItem)o).OnValueChanged(args)));

        public byte Value
        {
            get { return (byte) GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        private void OnValueChanged(DependencyPropertyChangedEventArgs args)
        {
            ValueChanged?.Invoke(this, args);
        }

    }
}
