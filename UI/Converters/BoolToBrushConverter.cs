using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AudioStemPlayer.UI.Converters
{
    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
            {
                return Application.Current?.FindResource("AccentBrush") as IBrush 
                       ?? new SolidColorBrush(Color.Parse("#2f6fed"));
            }
            return new SolidColorBrush(Color.Parse("#F0F0F0"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}