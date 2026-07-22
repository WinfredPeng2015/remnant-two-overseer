using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using RemnantOverseer.Models;
using RemnantOverseer.Models.Enums;
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

        var resourceName = item.IsLooted || item.IsPrerequisiteMissing
            ? "BaseTextDisabledBrush"
            : item.Type switch
            {
                ItemTypes.Amulet or ItemTypes.Ring => "ItemAccessoryBrush",
                ItemTypes.Engram => "ItemEngramBrush",
                ItemTypes.Mutator => "ItemMutatorBrush",
                ItemTypes.QuestItem => "ItemQuestBrush",
                ItemTypes.Relic => "ItemRelicBrush",
                ItemTypes.Weapon => "ItemWeaponBrush",
                ItemTypes.Trait => "ItemTraitBrush",
                _ => "BaseTextBrush"
            };

        return Application.Current!.TryGetResource(resourceName, out var result)
            ? result!
            : AvaloniaProperty.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
