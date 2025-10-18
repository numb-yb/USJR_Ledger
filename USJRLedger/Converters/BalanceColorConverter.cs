using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace USJRLedger.Converters
{
    public class BalanceColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Colors.Black;

            if (decimal.TryParse(value.ToString(), out var balance))
                return balance < 0 ? Colors.Red : Colors.Black;

            return Colors.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
