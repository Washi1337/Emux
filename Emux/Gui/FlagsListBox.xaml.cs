using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Emux.Gui
{
    /// <summary>
    /// Interaction logic for FlagsListBox.xaml
    /// </summary>
    public partial class FlagsListBox : UserControl
    {
        public sealed class FlagItemCollection : ObservableCollection<FlagItem>, IAddChild
        {
            private readonly FlagsListBox _owner;

            public FlagItemCollection(FlagsListBox owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            protected override void InsertItem(int index, FlagItem item)
            {
                item.IsSetChanged += ItemOnIsSetChanged;
                base.InsertItem(index, item);
            }

            protected override void SetItem(int index, FlagItem item)
            {
                this[index].IsSetChanged -= ItemOnIsSetChanged;
                item.IsSetChanged += ItemOnIsSetChanged;
                base.SetItem(index, item);
            }

            protected override void RemoveItem(int index)
            {
                this[index].IsSetChanged -= ItemOnIsSetChanged;
                base.RemoveItem(index);
            }

            protected override void ClearItems()
            {
                foreach (var item in this)
                    item.IsSetChanged -= ItemOnIsSetChanged;
                base.ClearItems();
            }

            private void ItemOnIsSetChanged(object sender, EventArgs eventArgs)
            {
                var item = (FlagItem)sender;
                _owner.RawValue = (byte)((_owner.RawValue & ~(1 << item.BitIndex)) | (item.IsSet ? (1 << item.BitIndex) : 0));
            }

            public void AddChild(object value)
            {
                Add((FlagItem)value);
            }

            public void AddText(string text)
            {
                throw new NotImplementedException();
            }
        }
        

        public event EventHandler RawValueChanged;

        public static readonly DependencyProperty RawValueProperty = DependencyProperty.Register(
            "RawValue", typeof(byte), typeof(FlagsListBox));

        public static readonly DependencyProperty FlagItemsProperty = DependencyProperty.Register(
            "FlagItems", typeof(FlagItemCollection), typeof(FlagsListBox));


        public FlagsListBox()
        {
            InitializeComponent();
            SetValue(FlagItemsProperty, new FlagItemCollection(this));
        }

        public byte RawValue
        {
            get { return (byte)GetValue(RawValueProperty); }
            set
            {
                if (RawValue != value)
                {
                    SetValue(RawValueProperty, value);
                    UpdateFlagItems();
                    OnRawValueChanged();
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Bindable(true)]
        public FlagItemCollection FlagItems
        {
            get { return (FlagItemCollection)GetValue(FlagItemsProperty); }
        }

        protected virtual void OnRawValueChanged()
        {
            RawValueChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateFlagItems()
        {
            foreach (var flag in FlagItems)
                flag.IsSet = (RawValue & (1 << flag.BitIndex)) != 0;
        }
        
    }
}
