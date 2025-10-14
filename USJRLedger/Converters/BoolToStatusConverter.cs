using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace USJRLedger.Converters
{
    public class BoolToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "Active" : "Inactive";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.Equals("Active", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }
}