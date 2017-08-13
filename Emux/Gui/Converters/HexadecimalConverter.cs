using System;
using System.Globalization;
using System.Windows.Data;

namespace Emux.Gui.Converters
{
    public class HexadecimalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (targetType != typeof(string))
                throw new NotSupportedException();

            if (value is byte)
                return ((byte)value).ToString("X2");
            if (value is ushort)
                return ((ushort)value).ToString("X4");

            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
