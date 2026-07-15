using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RemnantOverseer.Utilities;

/// <summary>
/// Extracts short acquisition facts from analyzer notes. Translation happens later.
/// </summary>
internal static class ItemHintSimplifier
{
    private const int MaxLines = 5;
    private const int MaxStepLength = 72;

    private static readonly Regex FoundInLocationRegex = new(
        @"^Found in (?:the |an |a )?(?<location>.+?) location on (?<world>[^.,]+)(?:[.,]\s*)?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex FoundInLocationsRegex = new(
        @"^Found in (?:the )?(?<locations>.+?) locations on (?<world>[^.,]+)(?:[.,]\s*)?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex BuyFromRegex = new(
        @"^(?:Can be bought from|Buy from) (?<vendor>.+?) (?:at|in) (?<place>[^.]+?)(?: after (?<condition>[^.]+))?(?:\.|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex CraftedAtRegex = new(
        @"^Crafted at (?<station>[^.]+?)(?: using (?<materials>[^.]+))?(?:\.|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex CraftedByRegex = new(
        @"^Crafted by (?<vendor>[^,.]+?) from (?:the )?(?<material>[^,.]+)(?:[,.]|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex GivenByRegex = new(
        @"^Given by (?<npc>.+?) in (?:the )?(?<location>.+?) location on (?<world>[^.,]+)(?:[.,]\s*)?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex DropsFromLocationRegex = new(
        @"^Drops from (?<enemy>.+?) in (?:the )?(?<location>.+?) location on (?<world>[^.,]+)(?:[.,]\s*)?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex FinishLocationRegex = new(
        @"^Finish (?:the )?(?:level|area) in (?:the )?(?<location>.+?) location(?: on (?<world>[^,.]+))?[,.]?\s*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static string Simplify(string itemName, string itemId, string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        return KnownHint(description)
            ?? TryBuildStructuredHint(description.Trim())
            ?? TryBuildFallbackHint(description.Trim())
            ?? Step("Check item source");
    }

    // Hints are intentionally English during the simplification phase.
    public static bool IsLocalizedHint(string text) => false;

    private static string? KnownHint(string description)
    {
        if (description.Contains("Council Chamber", StringComparison.OrdinalIgnoreCase)
            && (description.Contains("Council Tribunal", StringComparison.OrdinalIgnoreCase)
                || description.Contains("council keys", StringComparison.OrdinalIgnoreCase)
                || description.Contains("Assassin Dagger", StringComparison.OrdinalIgnoreCase)))
        {
            return Lines(
                Location("Losomn", "Council Chamber"),
                Step("Council: accept traitor task"),
                Step("Mirror -> Tribunal: 3 keys; teal / red / purple"),
                Step("Throne room: Assassin Dagger -> inspect jewel"),
                Step("Accuse wrong person -> defeat Council"));
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
                Location("N'Erud", "Agronomy Sector"),
                Step("Collect: Head / Arm / Claw / Memory Core"),
                Step("Head: Quiescent Overlook crash site"),
                Step("Arm / Claw: Anterior Recess / Withered Necropolis"),
                Step($"{memory} -> {reward}"));
        }

        if (ContainsAll(description, "Ethereal Manor", "Liquid Escape", "must not die"))
        {
            return Lines(
                Location("Losomn", "Ethereal Manor"),
                Step("Get grabbed by Dran -> Liquid Escape"),
                Step("Ward 13: exhaust old Ward dialogue; do not die"),
                Step("Red Throne -> Fractured Ingress -> Tal'Ratha's Refuge"),
                Step("Ashen Wasteland -> Tormented Asylum -> Dran's Dream"));
        }

        if (ContainsAll(description, "Decorum Cipher", "Memory Core II", "Drzyr Replicator"))
        {
            return Lines(
                Location("N'Erud", "Ascension Spire"),
                Step("Decorum Cipher: Terminus Station train roof"),
                Step("Memory Core II: Dormant N'Erudian Facility"),
                Step("Get Biome-Control Glyph -> use Glyph door"),
                Step("Get Memory Core II -> insert cores at fixture"));
        }

        if (ContainsAll(description, "Thaen Seed", "Thaen Tree Fruit Ripening"))
        {
            return Lines(
                Location("Yaesha", "Red Throne route"),
                Step("Widow's Court: Ornate Key + Lockbox"),
                Step("Inspect lockbox -> use key -> Thaen Seed"),
                Step("Ward 13 garden: plant seed; Mature 1d / Elder 2d / Celestial 3d"),
                Step("Fourth Celestial fruit: Ripened Heart"));
        }

        if (ContainsAll(description, "Fortune Hunter", "The Backrooms", "Strange Box"))
        {
            return Lines(
                Location("Labyrinth", "Corrupted door"),
                Step("Equip listed Explorer + Invader skills and gear"),
                Step("Cycling portal: wait about 2 seconds -> enter falling scene"),
                Step("Open Corrupted door -> Backrooms"),
                Step("Get Strange Box"));
        }

        return null;
    }

    private static string? TryBuildStructuredHint(string text)
    {
        if (TryBuildRandomDropHint(text, out var randomDrop))
        {
            return randomDrop;
        }

        var craftedBy = CraftedByRegex.Match(text);
        if (craftedBy.Success)
        {
            return Lines(
                Source("Craft From", craftedBy.Groups["vendor"].Value),
                Step($"Requires: {craftedBy.Groups["material"].Value}"));
        }

        var crafted = CraftedAtRegex.Match(text);
        if (crafted.Success)
        {
            var lines = new List<string> { Source("Craft From", crafted.Groups["station"].Value) };
            AddStep(lines, "Materials", crafted.Groups["materials"].Value);
            return Lines(lines);
        }

        var buy = BuyFromRegex.Match(text);
        if (buy.Success)
        {
            var lines = new List<string> { Source("Buy From", $"{buy.Groups["vendor"].Value} / {buy.Groups["place"].Value}") };
            AddStep(lines, "Requirement", buy.Groups["condition"].Value);
            return Lines(lines);
        }

        var given = GivenByRegex.Match(text);
        if (given.Success)
        {
            return BuildSourceLocationHint("Get From", given.Groups["npc"].Value, given.Groups["world"].Value, given.Groups["location"].Value, text[given.Length..]);
        }

        var drops = DropsFromLocationRegex.Match(text);
        if (drops.Success)
        {
            return BuildSourceLocationHint("Drop From", drops.Groups["enemy"].Value, drops.Groups["world"].Value, drops.Groups["location"].Value, text[drops.Length..]);
        }

        var foundSingle = FoundInLocationRegex.Match(text);
        if (foundSingle.Success)
        {
            return BuildSourceLocationHint(null, null, foundSingle.Groups["world"].Value, foundSingle.Groups["location"].Value, text[foundSingle.Length..]);
        }

        var foundMulti = FoundInLocationsRegex.Match(text);
        if (foundMulti.Success)
        {
            return BuildSourceLocationHint(null, null, foundMulti.Groups["world"].Value, foundMulti.Groups["locations"].Value, text[foundMulti.Length..]);
        }

        var finish = FinishLocationRegex.Match(text);
        return finish.Success
            ? BuildSourceLocationHint(null, null, finish.Groups["world"].Value, finish.Groups["location"].Value, text[finish.Length..])
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
            hint = $"Random drop: [{world.Groups["world"].Value}]";
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

    private static string BuildSourceLocationHint(string? sourceLabel, string? sourceValue, string world, string location, string remainder)
    {
        var lines = new List<string> { Location(world, location) };
        if (!string.IsNullOrWhiteSpace(sourceLabel) && !string.IsNullOrWhiteSpace(sourceValue))
        {
            lines.Add(Source(sourceLabel, sourceValue));
        }

        AddRemainderFacts(lines, remainder);
        return Lines(lines);
    }

    private static string? TryBuildFallbackHint(string description)
    {
        var lines = new List<string>();
        AddRemainderFacts(lines, description);
        return lines.Count > 0 ? Lines(lines) : null;
    }

    private static void AddRemainderFacts(List<string> lines, string text)
    {
        foreach (var sentence in text.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (lines.Count >= MaxLines)
            {
                return;
            }

            var source = TryExtractSource(sentence);
            if (source is not null)
            {
                lines.Add(source);
                continue;
            }

            var step = TryExtractStep(sentence);
            if (step is not null)
            {
                lines.Add(step);
            }
        }
    }

    private static string? TryExtractSource(string sentence)
    {
        var given = Regex.Match(sentence, @"^given by (?<npc>[^,.]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        return given.Success ? Source("Get From", given.Groups["npc"].Value) : null;
    }

    private static string? TryExtractStep(string sentence)
    {
        var text = sentence.Trim();
        if (text.Length == 0)
        {
            return null;
        }

        if (Regex.IsMatch(text, @"^Collect the council keys", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
        {
            return Step("Council seats: collect and reinsert keys");
        }

        var open = Regex.Match(text, @"^(?:This will )?open (?<target>[^,.]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        if (open.Success)
        {
            return Step($"Open: {open.Groups["target"].Value}");
        }

        if (Regex.IsMatch(text, @"^Climb .*throne.*jump", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
        {
            return Step("Behind throne: jump to rear");
        }

        var interact = Regex.Match(text, @"interact with (?:the )?(?<target>[^,.]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        if (interact.Success)
        {
            return Step($"Interact: {interact.Groups["target"].Value}");
        }

        var pick = Regex.Match(text, @"^(?:Pick up|Take) (?:the )?(?<item>[^,.]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        if (pick.Success)
        {
            return Step($"Pick up: {pick.Groups["item"].Value}");
        }

        var defeat = Regex.Match(text, @"^(?:Defeat|Kill) (?:the )?(?<enemy>[^,.]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        if (defeat.Success)
        {
            return Step($"Defeat: {defeat.Groups["enemy"].Value}");
        }

        if (text.Length <= MaxStepLength)
        {
            return Step(text);
        }

        return null;
    }

    private static void AddStep(List<string> lines, string label, string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && lines.Count < MaxLines)
        {
            lines.Add(Step($"{label}: {TrimPeriod(value)}"));
        }
    }

    private static string Location(string world, string location)
    {
        var value = string.IsNullOrWhiteSpace(world) ? location : $"{world} / {location}";
        return Source("Location", value);
    }

    private static string Source(string label, string value) => $"{label}: [{TrimPeriod(value)}]";

    private static string Step(string value) => $"- {TrimPeriod(value)}";

    private static string Lines(params string[] lines) => Lines((IEnumerable<string>)lines);

    private static string Lines(IEnumerable<string> lines) => string.Join("\n", lines);

    private static string TrimPeriod(string text) => text.Trim().TrimEnd('.');

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
