using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SmartX.WebApp.Converters
{
    /// <summary>
    /// Converts a boolean to its inverse. Used in XAML to toggle visibility
    /// between the "Open" and "Coming Soon" buttons (dotnet, 2022).
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b ? !b : true;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b ? !b : false;
    }

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
       => value is bool b && b ? Colors.Green : Colors.Gray;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

