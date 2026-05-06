using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using AudioStemPlayer.Core.Models;

namespace AudioStemPlayer.UI.Converters;

public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string? status = value as string;
        string resourceKey = status switch
        {
            ProcessingStatuses.Failed or ProcessingStatuses.Canceled => "DangerBrush",
            ProcessingStatuses.Succeeded => "AccentBrush",
            _ => "TextBrush"
        };

        if (Application.Current?.Resources.TryGetResource(resourceKey, null, out var resource) == true &&
            resource is IBrush brush)
        {
            return brush;
        }
        
        return status switch
        {
            ProcessingStatuses.Failed or ProcessingStatuses.Canceled => Brushes.IndianRed,
            ProcessingStatuses.Succeeded => Brushes.MediumSeaGreen,
            _ => Brushes.Gray
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}