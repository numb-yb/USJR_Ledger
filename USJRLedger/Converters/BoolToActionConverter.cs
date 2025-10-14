using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace USJRLedger.Converters
{
    public class BoolToActionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "Deactivate" : "Activate";
            }
            return "Toggle Status";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string action)
            {
                return action.Equals("Activate", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }
}