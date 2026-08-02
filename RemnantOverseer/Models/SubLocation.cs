using CommunityToolkit.Mvvm.ComponentModel;
using RemnantOverseer.Services;
using System.Collections.Generic;

namespace RemnantOverseer.Models;

public class SubLocation : ObservableObject
{
    public string CanonicalName { get; set; } = string.Empty;
    public string LocalizationKey { get; set; } = string.Empty;
    public string Name => LocalizationService.GameString(
        LocalizationKey,
        LocalizationService.GameString(CanonicalName, CanonicalName));

    public List<Item> Items { get; set; } = [];
    public bool IsWorldStone { get; set; }
    public bool IsRespawnLocation { get; set; }

    public string? FormattedWorldStoneName => IsWorldStone
        ? LocalizationService.Format("Location_WorldStoneRespawn", Name)
        : null;

    public SubLocation ShallowCopy()
    {
        return (SubLocation)MemberwiseClone();
    }

    public void RefreshLocalizedProperties()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(FormattedWorldStoneName));
    }
}
