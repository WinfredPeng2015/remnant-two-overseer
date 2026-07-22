using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using RemnantOverseer.Models;
using System;
using System.Globalization;

namespace RemnantOverseer.Utilities;

public class ItemTypeToForegroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Item item)
        {
            return AvaloniaProperty.UnsetValue;
        }

        if (item.IsLooted || item.IsPrerequisiteMissing)
        {
            return Application.Current!.TryGetResource("BaseTextDisabledBrush", out var disabledBrush)
                ? disabledBrush!
                : AvaloniaProperty.UnsetValue;
        }

        return App.Resolve<Services.ItemColorService>().GetBrush(item.Type);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
