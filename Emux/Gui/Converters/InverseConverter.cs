using System;
using System.Globalization;
using System.Windows.Data;

namespace Emux.Gui.Converters
{
    public class InverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new NotSupportedException();
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            return !((bool) value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new NotSupportedException();
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            return !((bool)value);
        }
    }
}
