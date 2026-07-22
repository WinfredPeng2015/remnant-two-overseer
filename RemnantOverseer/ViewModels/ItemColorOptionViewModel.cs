using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RemnantOverseer.Models.Enums;
using RemnantOverseer.Services;
using System;

namespace RemnantOverseer.ViewModels;

public sealed partial class ItemColorOptionViewModel : ObservableObject
{
    private readonly Action<ItemColorOptionViewModel> _changed;
    private ItemColorMode _mode;
    private Color _customColor;
    private string _customHex;

    public ItemColorOptionViewModel(
        ItemTypes type,
        string displayName,
        ItemColorMode mode,
        Color originalColor,
        Color presetColor,
        Color customColor,
        Action<ItemColorOptionViewModel> changed)
    {
        Type = type;
        DisplayName = displayName;
        _mode = mode;
        OriginalBrush = new SolidColorBrush(originalColor);
        PresetBrush = new SolidColorBrush(presetColor);
        CustomBrush = new SolidColorBrush(customColor);
        _customColor = customColor;
        _customHex = ItemColorService.ToHex(customColor);
        _changed = changed;
    }

    public ItemTypes Type { get; }
    public string DisplayName { get; }
    public SolidColorBrush OriginalBrush { get; }
    public SolidColorBrush PresetBrush { get; }
    public SolidColorBrush CustomBrush { get; }

    public ItemColorMode Mode
    {
        get => _mode;
        private set
        {
            if (!SetProperty(ref _mode, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsOriginal));
            OnPropertyChanged(nameof(IsPreset));
            OnPropertyChanged(nameof(IsCustom));
            _changed(this);
        }
    }

    public bool IsOriginal => Mode == ItemColorMode.Original;

    public bool IsPreset => Mode == ItemColorMode.Preset;

    public bool IsCustom => Mode == ItemColorMode.Custom;

    public Color CustomColor
    {
        get => _customColor;
        set
        {
            if (!SetProperty(ref _customColor, value))
            {
                return;
            }

            CustomBrush.Color = value;
            _customHex = ItemColorService.ToHex(value);
            OnPropertyChanged(nameof(CustomHex));
            _changed(this);
        }
    }

    public string CustomHex
    {
        get => _customHex;
        set
        {
            if (!SetProperty(ref _customHex, value))
            {
                return;
            }

            if (ItemColorService.TryParseColor(value, out var color) && color != CustomColor)
            {
                CustomColor = color;
            }
        }
    }

    [RelayCommand]
    private void SelectMode(string modeName)
    {
        if (!Enum.TryParse<ItemColorMode>(modeName, out var mode))
        {
            return;
        }

        if (Mode != mode)
        {
            Mode = mode;
            return;
        }

        OnPropertyChanged(nameof(IsOriginal));
        OnPropertyChanged(nameof(IsPreset));
        OnPropertyChanged(nameof(IsCustom));
    }
}
