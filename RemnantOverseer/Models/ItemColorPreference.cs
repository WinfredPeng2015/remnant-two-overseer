using RemnantOverseer.Models.Enums;

namespace RemnantOverseer.Models;

public class ItemColorPreference
{
    public ItemColorMode Mode { get; set; } = ItemColorMode.Preset;
    public string? CustomColor { get; set; }
}
