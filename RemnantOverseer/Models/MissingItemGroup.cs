using CommunityToolkit.Mvvm.ComponentModel;
using RemnantOverseer.Models.Enums;
using RemnantOverseer.Services;
using System.Collections.Generic;

namespace RemnantOverseer.Models;

public class MissingItemGroup : ObservableObject
{
    public ItemTypes Type { get; set; } = ItemTypes.Unknown;
    public bool IsItemTypeGroup { get; set; }
    public string CanonicalWorldName { get; set; } = string.Empty;
    public List<Item> Items { get; set; } = [];

    public string Name => IsItemTypeGroup
        ? LocalizationService.ItemTypePluralName(Type)
        : string.IsNullOrEmpty(CanonicalWorldName)
            ? LocalizationService.Get("Common_Unknown")
            : LocalizationService.GameString(CanonicalWorldName, CanonicalWorldName);
}
