using Avalonia.Media;
using RemnantOverseer.Models.Enums;
using System;
using System.Collections.Generic;

namespace RemnantOverseer.Services;

public sealed class ItemColorService
{
    private static readonly Color OriginalColor = Color.Parse("#FFDADADA");

    private readonly SettingsService _settingsService;
    private readonly Dictionary<ItemTypes, SolidColorBrush> _brushes = new();

    public ItemColorService(SettingsService settingsService)
    {
        _settingsService = settingsService;

        foreach (var type in Enum.GetValues<ItemTypes>())
        {
            _brushes[type] = new SolidColorBrush(OriginalColor);
        }

        ApplyAll();
    }

    public SolidColorBrush GetBrush(ItemTypes type) => _brushes[type];

    public Color GetOriginalColor() => OriginalColor;

    public Color GetPresetColor(ItemTypes type) => type switch
    {
        ItemTypes.Amulet or ItemTypes.Ring => Color.Parse("#FFAA7ADB"),
        ItemTypes.Engram => Color.Parse("#FFFFD25A"),
        ItemTypes.Mutator => Color.Parse("#FFF08A24"),
        ItemTypes.QuestItem => Color.Parse("#FFC4424D"),
        ItemTypes.Relic => Color.Parse("#FF69B96B"),
        ItemTypes.Weapon => Color.Parse("#FFB8860B"),
        ItemTypes.Trait => Color.Parse("#FFE4E4E4"),
        _ => OriginalColor
    };

    public void ApplyAll()
    {
        foreach (var type in Enum.GetValues<ItemTypes>())
        {
            Apply(type);
        }
    }

    public void Apply(ItemTypes type)
    {
        var preference = _settingsService.Get().GetItemColorPreference(type);
        _brushes[type].Color = preference.Mode switch
        {
            ItemColorMode.Original => OriginalColor,
            ItemColorMode.Custom when TryParseColor(preference.CustomColor, out var customColor) => customColor,
            _ => GetPresetColor(type)
        };
    }

    public static bool TryParseColor(string? value, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        if (!normalized.StartsWith('#'))
        {
            normalized = $"#{normalized}";
        }

        if (normalized.Length == 7)
        {
            normalized = $"#FF{normalized[1..]}";
        }

        try
        {
            color = Color.Parse(normalized);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
