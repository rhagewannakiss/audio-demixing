using Avalonia.Data.Converters;
using System;
using System.Globalization;
using System.IO;

namespace AudioStemPlayer.UI.Converters
{
    public class FileNameConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string filePath && !string.IsNullOrEmpty(filePath))
                return Path.GetFileName(filePath);
            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}