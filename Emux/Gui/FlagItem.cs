using System;
using System.ComponentModel;
using System.Windows;

namespace Emux.Gui
{
    public class FlagItem : DependencyObject, INotifyPropertyChanged
    {
        public event EventHandler IsSetChanged;

        public static readonly DependencyProperty BitIndexProperty = DependencyProperty.Register(
            "BitIndex", typeof(int), typeof(FlagItem));

        public static readonly DependencyProperty FlagNameProperty = DependencyProperty.Register(
            "FlagName", typeof(string), typeof(FlagItem));

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
            "IsReadOnly", typeof(bool), typeof(FlagItem));

        public static readonly DependencyProperty IsSetProperty = DependencyProperty.Register(
            "IsSet", typeof(bool), typeof(FlagItem), new PropertyMetadata(PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((FlagItem)o).OnIsSetChanged();
        }

        public int BitIndex
        {
            get { return (int) GetValue(BitIndexProperty); }
            set { SetValue(BitIndexProperty, value); }
        }
        
        public string FlagName
        {
            get { return (string) GetValue(FlagNameProperty); }
            set { SetValue(FlagNameProperty, value); }
        }

        public bool IsReadOnly
        {
            get { return (bool) GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public bool IsSet
        {
            get { return (bool) GetValue(IsSetProperty); }
            set
            {
                if (value != IsSet)
                {
                    SetValue(IsSetProperty, value);
                    OnIsSetChanged();
                }
            }
        }

        protected virtual void OnIsSetChanged()
        {
            IsSetChanged?.Invoke(this, EventArgs.Empty);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}