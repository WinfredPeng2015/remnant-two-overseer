using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RemnantOverseer.Utilities;

internal static class ItemHintSimplifier
{
    private static readonly Regex FoundInLocationRegex = new(
        @"^Found in the (?<location>.+?) location on (?<world>.+?)\.\s*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string Simplify(string itemName, string itemId, string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        var knownHint = KnownHint(itemName, itemId, description);
        if (!string.IsNullOrWhiteSpace(knownHint))
        {
            return knownHint;
        }

        if (description.Length < 260)
        {
            return description;
        }

        return TryBuildLocationHint(description) ?? description;
    }

    public static bool IsLocalizedHint(string text)
    {
        if (!IsTraditionalChinese())
        {
            return false;
        }

        foreach (var ch in text)
        {
            if (ch >= '\u4e00' && ch <= '\u9fff')
            {
                return true;
            }
        }

        return false;
    }

    private static string? KnownHint(string itemName, string itemId, string description)
    {
        if (!ContainsAll(description, "Council Chamber location on Losomn", "accuse the wrong person"))
        {
            return null;
        }

        if (IsTraditionalChinese())
        {
            return string.Join(Environment.NewLine, [
                "\u5730\u9ede\uff1a\u6d1b\u65af\u66fc / \u8b70\u6703\u5ef3",
                "- \u63a5\u8b70\u6703\u6293\u53db\u5f92",
                "- \u901a\u95dc\u5230\u93e1\u5b50\uff0c\u9032\u8b70\u6703\u6cd5\u5ead",
                "- \u62ff 3 \u628a\u8b70\u6703\u9470\u5319",
                "- \u5ea7\u4f4d\u9806\u5e8f\uff1a\u9752\u7da0 / \u7d05 / \u7d2b",
                "- \u738b\u5ea7\u5f8c\u65b9\u62ff\u523a\u5ba2\u5315\u9996",
                "- \u770b\u5315\u9996\u5bf6\u77f3\u984f\u8272",
                "- \u56de\u8b70\u6703\uff0c\u6545\u610f\u6307\u63a7\u932f\u4eba",
                "- \u6253\u4e09\u8b70\u54e1\uff0c\u62ff\u734e\u52f5"
            ]);
        }

        return string.Join(Environment.NewLine, [
            "Location: Losomn / Council Chamber",
            "- Council: accept traitor task",
            "- Finish area -> mirror -> Council Tribunal",
            "- Tribunal seats: take 3 keys",
            "- Seat order: teal / red / purple",
            "- Throne room: grab Assassin Dagger",
            "- Inspect dagger jewel",
            "- Back to Council: accuse wrong person",
            "- Kill council, get reward"
        ]);
    }

    private static string? TryBuildLocationHint(string description)
    {
        var match = FoundInLocationRegex.Match(description);
        if (!match.Success)
        {
            return null;
        }

        var location = match.Groups["location"].Value;
        var world = match.Groups["world"].Value;
        var remaining = description[match.Length..];
        var steps = BuildKeywordSteps(remaining);
        var locationLine = IsTraditionalChinese()
            ? $"\u5730\u9ede\uff1a{world} / {location}"
            : $"Location: {world} / {location}";

        if (steps.Count == 0)
        {
            return locationLine;
        }

        steps.Insert(0, locationLine);
        return string.Join(Environment.NewLine, steps);
    }

    private static List<string> BuildKeywordSteps(string text)
    {
        var steps = new List<string>();
        foreach (var sentence in text.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var step = SimplifySentence(sentence);
            if (string.IsNullOrWhiteSpace(step))
            {
                continue;
            }

            steps.Add("- " + step);
            if (steps.Count >= 6)
            {
                break;
            }
        }

        return steps;
    }

    private static string SimplifySentence(string sentence)
    {
        var step = sentence.Trim();
        if (step.Length == 0)
        {
            return string.Empty;
        }

        step = step.Replace("Afterwards, ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Afterwards ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("There, ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("This will ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("the item will be given as the reward for ", "Reward: ", StringComparison.OrdinalIgnoreCase)
            .Replace("Complete the level and transition to ", "Complete area -> ", StringComparison.OrdinalIgnoreCase)
            .Replace(" by interacting with ", " via ", StringComparison.OrdinalIgnoreCase)
            .Replace("proceed until you reach ", "go to ", StringComparison.OrdinalIgnoreCase)
            .Replace("Put the keys back into the seats in the correct order from left to right: ", "Seat order: ", StringComparison.OrdinalIgnoreCase)
            .Replace("Pick up the ", "Pick: ", StringComparison.OrdinalIgnoreCase)
            .Replace("Return to the ", "Back to ", StringComparison.OrdinalIgnoreCase);

        if (step.Length > 110)
        {
            step = step[..107].TrimEnd() + "...";
        }

        return step;
    }

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

    private static bool IsTraditionalChinese()
    {
        var culture = CultureInfo.CurrentUICulture;
        var name = culture.Name;

        if (name.Contains("Hant", StringComparison.OrdinalIgnoreCase)
            || name.Equals("zh-TW", StringComparison.OrdinalIgnoreCase)
            || name.Equals("zh-HK", StringComparison.OrdinalIgnoreCase)
            || name.Equals("zh-MO", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return culture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase)
            && !name.Contains("Hans", StringComparison.OrdinalIgnoreCase)
            && !name.Equals("zh-CN", StringComparison.OrdinalIgnoreCase)
            && !name.Equals("zh-SG", StringComparison.OrdinalIgnoreCase);
    }
}
