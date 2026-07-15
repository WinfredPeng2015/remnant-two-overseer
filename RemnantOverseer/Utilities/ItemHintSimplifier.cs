using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RemnantOverseer.Utilities;

/// <summary>
/// Turns analyzer notes into short, language-neutral acquisition hints.
/// Translation is deliberately handled after this step, not while parsing notes.
/// </summary>
internal static class ItemHintSimplifier
{
    private const int MaxSteps = 5;
    private const int MaxStepLength = 72;

    private static readonly Regex FoundInLocationRegex = new(
        @"^Found in (?:the |an |a )?(?<location>.+?) location on (?<world>.+?)\.\s*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex FoundInLocationsRegex = new(
        @"^Found in (?:the )?(?<locations>.+?) locations on (?<world>.+?)\.\s*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex BuyFromRegex = new(
        @"^(?:Can be bought from|Buy from) (?<vendor>.+?) (?:at|in) (?<place>.+?)(?: after (?<condition>.+?))?\.?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex CraftedAtRegex = new(
        @"^Crafted at (?<station>.+?)(?: using (?<materials>.+?))?\.?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex GivenByRegex = new(
        @"^Given by (?<npc>.+?) in (?:the )?(?<location>.+?) location on (?<world>.+?)\.\s*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex DropsFromLocationRegex = new(
        @"^Drops from (?<enemy>.+?) in (?:the )?(?<location>.+?) location on (?<world>.+?)\.\s*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static string Simplify(string itemName, string itemId, string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        return KnownHint(description)
            ?? TryBuildStructuredHint(description)
            ?? TryBuildFallbackHint(description)
            ?? description;
    }

    // Translation has not happened yet, so the UI should show its normal translate link.
    public static bool IsLocalizedHint(string text) => false;

    private static string? KnownHint(string description)
    {
        if (ContainsAll(description, "Council Chamber location on Losomn", "accuse the wrong person"))
        {
            return Lines(
                "Location: Losomn / Council Chamber",
                "Council: accept traitor task",
                "Mirror -> Tribunal: take 3 keys; teal / red / purple",
                "Throne room: Assassin Dagger -> inspect jewel",
                "Council: accuse wrong person -> defeat Council");
        }

        if (ContainsAll(description, "The Prototype", "Prototype Claw", "Prototype Memory Core"))
        {
            var memory = description.Contains("blue state", StringComparison.OrdinalIgnoreCase)
                ? "Memory Core: blue"
                : "Memory Core: default red";
            var reward = description.StartsWith("Given by", StringComparison.OrdinalIgnoreCase)
                ? "Return parts to Prototype NPC"
                : "Defeat Prototype aberration";

            return Lines(
                "Location: N'Erud / Agronomy Sector",
                "Collect: Head / Arm / Claw / Memory Core",
                "Head: Quiescent Overlook crash site",
                "Arm / Claw: Anterior Recess / Withered Necropolis",
                $"{memory} -> {reward}");
        }

        if (ContainsAll(description, "Ethereal Manor", "Liquid Escape", "must not die"))
        {
            return Lines(
                "Start: Losomn / Ethereal Manor",
                "Get grabbed by Dran -> Liquid Escape",
                "Ward 13: exhaust old Ward dialogue",
                "Do not die afterwards",
                "Red Throne -> Fractured Ingress -> Tal'Ratha's Refuge",
                "Ashen Wasteland -> Tormented Asylum -> Dran's Dream");
        }

        if (ContainsAll(description, "Decorum Cipher", "Memory Core II", "Drzyr Replicator"))
        {
            return Lines(
                "Location: N'Erud / Ascension Spire",
                "Decorum Cipher: Terminus Station train roof",
                "Memory Core II: Dormant N'Erudian Facility",
                "Get Biome-Control Glyph -> use Glyph door",
                "Get Memory Core II -> insert cores at fixture");
        }

        if (ContainsAll(description, "Thaen Seed", "Thaen Tree Fruit Ripening"))
        {
            return Lines(
                "Location: Yaesha / Red Throne route",
                "Widow's Court: Ornate Key + Lockbox",
                "Inspect lockbox -> use key -> Thaen Seed",
                "Ward 13 garden: plant seed; Mature 1d / Elder 2d / Celestial 3d",
                "Fourth Celestial fruit: Ripened Heart");
        }

        if (ContainsAll(description, "Fortune Hunter", "The Backrooms", "Strange Box"))
        {
            return Lines(
                "Location: Labyrinth / Corrupted door",
                "Equip listed Explorer + Invader skills and gear",
                "Use cycling portal: wait about 2 seconds",
                "Enter falling scene",
                "Open Corrupted door -> Backrooms",
                "Get Strange Box");
        }

        return null;
    }

    private static string? TryBuildStructuredHint(string description)
    {
        var text = description.Trim();
        if (TryBuildRandomDropHint(text, out var randomDrop))
        {
            return randomDrop;
        }

        var buy = BuyFromRegex.Match(text);
        if (buy.Success)
        {
            var lines = new List<string> { $"Buy: {buy.Groups["vendor"].Value} / {buy.Groups["place"].Value}" };
            AddCondition(lines, buy.Groups["condition"].Value);
            return Lines(lines);
        }

        var crafted = CraftedAtRegex.Match(text);
        if (crafted.Success)
        {
            var lines = new List<string> { $"Craft: {crafted.Groups["station"].Value}" };
            AddCondition(lines, crafted.Groups["materials"].Value, "Materials");
            return Lines(lines);
        }

        var given = GivenByRegex.Match(text);
        if (given.Success)
        {
            return BuildSourceLocationHint($"Get from: {given.Groups["npc"].Value}", given.Groups["world"].Value, given.Groups["location"].Value, text[given.Length..]);
        }

        var drops = DropsFromLocationRegex.Match(text);
        if (drops.Success)
        {
            return BuildSourceLocationHint($"Drop: {drops.Groups["enemy"].Value}", drops.Groups["world"].Value, drops.Groups["location"].Value, text[drops.Length..]);
        }

        var foundSingle = FoundInLocationRegex.Match(text);
        if (foundSingle.Success)
        {
            return BuildSourceLocationHint(null, foundSingle.Groups["world"].Value, foundSingle.Groups["location"].Value, text[foundSingle.Length..]);
        }

        var foundMulti = FoundInLocationsRegex.Match(text);
        return foundMulti.Success
            ? BuildSourceLocationHint(null, foundMulti.Groups["world"].Value, foundMulti.Groups["locations"].Value, text[foundMulti.Length..])
            : null;
    }

    private static bool TryBuildRandomDropHint(string text, out string hint)
    {
        var normalized = TrimPeriod(text);
        if (normalized.Equals("Random drop", StringComparison.OrdinalIgnoreCase))
        {
            hint = "Random drop";
            return true;
        }

        var world = Regex.Match(normalized, @"^Random (?<world>N'Erud|Yaesha|Losomn) drop$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        if (world.Success)
        {
            hint = $"Random drop: {world.Groups["world"].Value}";
            return true;
        }

        if (normalized.Equals("Drops from a random aberration on a random world", StringComparison.OrdinalIgnoreCase))
        {
            hint = "Random world: defeat aberration";
            return true;
        }

        hint = string.Empty;
        return false;
    }

    private static string BuildSourceLocationHint(string? source, string world, string location, string remainder)
    {
        var lines = new List<string> { $"Location: {world} / {location}" };
        if (!string.IsNullOrWhiteSpace(source))
        {
            lines.Add(source);
        }

        lines.AddRange(BuildKeywordSteps(remainder, MaxSteps - lines.Count));
        return Lines(lines);
    }

    private static string? TryBuildFallbackHint(string description)
    {
        var steps = BuildKeywordSteps(description, MaxSteps);
        return steps.Count > 0 ? Lines(steps) : null;
    }

    private static List<string> BuildKeywordSteps(string text, int maxSteps)
    {
        var steps = new List<string>();
        foreach (var sentence in text.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var step = SimplifySentence(sentence);
            if (string.IsNullOrWhiteSpace(step))
            {
                continue;
            }

            steps.Add(step);
            if (steps.Count >= maxSteps)
            {
                break;
            }
        }

        return steps;
    }

    private static string SimplifySentence(string sentence)
    {
        var step = sentence.Trim();
        step = Regex.Replace(step, @"^(Afterwards|There),?\s+", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        step = Regex.Replace(step, @"^(You will need to|You need to)\s+", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        step = step.Replace("Complete the level and transition to ", "Complete area -> ", StringComparison.OrdinalIgnoreCase)
            .Replace(" by interacting with ", " via ", StringComparison.OrdinalIgnoreCase)
            .Replace("proceed until you reach ", "Go to ", StringComparison.OrdinalIgnoreCase)
            .Replace("Pick up the ", "Pick: ", StringComparison.OrdinalIgnoreCase)
            .Replace("Return to the ", "Back to ", StringComparison.OrdinalIgnoreCase)
            .Replace("the item will be given as the reward for ", "Reward: ", StringComparison.OrdinalIgnoreCase);

        return Truncate(step, MaxStepLength);
    }

    private static void AddCondition(List<string> lines, string condition, string label = "Requirement")
    {
        if (!string.IsNullOrWhiteSpace(condition))
        {
            lines.Add($"{label}: {Truncate(TrimPeriod(condition), MaxStepLength)}");
        }
    }

    private static string Lines(params string[] lines) => Lines((IEnumerable<string>)lines);

    private static string Lines(IEnumerable<string> lines) => string.Join("\n", lines);

    private static string TrimPeriod(string text) => text.Trim().TrimEnd('.');

    private static string Truncate(string text, int maxLength) => text.Length <= maxLength
        ? text
        : text[..Math.Max(0, maxLength - 3)].TrimEnd() + "...";

    private static bool ContainsAll(string text, params string[] needles)
    {
        foreach (var needle in needles)
        {
            if (!text.Contains(needle, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
