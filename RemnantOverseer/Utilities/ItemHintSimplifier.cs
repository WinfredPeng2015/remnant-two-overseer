using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RemnantOverseer.Utilities;

internal static class ItemHintSimplifier
{
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

        var traditionalChinese = IsTraditionalChinese();
        var knownHint = KnownHint(itemName, itemId, description, traditionalChinese);
        if (!string.IsNullOrWhiteSpace(knownHint))
        {
            return knownHint;
        }

        if (traditionalChinese)
        {
            return TryBuildStructuredHint(description, true)
                ?? TryBuildFallbackHint(description, true)
                ?? description;
        }

        if (description.Length < 260)
        {
            return description;
        }

        return TryBuildStructuredHint(description, false)
            ?? TryBuildFallbackHint(description, false)
            ?? description;
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

    private static string? KnownHint(string itemName, string itemId, string description, bool zh)
    {
        if (ContainsAll(description, "Council Chamber location on Losomn", "accuse the wrong person"))
        {
            return zh
                ? Lines(
                    "地點：洛斯曼 / 議會廳",
                    "- 接議會抓叛徒",
                    "- 通關到鏡子，進議會法庭",
                    "- 拿 3 把議會鑰匙",
                    "- 座位順序：青綠 / 紅 / 紫",
                    "- 王座後方拿刺客匕首",
                    "- 看匕首寶石顏色",
                    "- 回議會，故意指控錯人",
                    "- 打三議員，拿獎勵")
                : Lines(
                    "Location: Losomn / Council Chamber",
                    "- Council: accept traitor task",
                    "- Finish area -> mirror -> Council Tribunal",
                    "- Tribunal seats: take 3 keys",
                    "- Seat order: teal / red / purple",
                    "- Throne room: grab Assassin Dagger",
                    "- Inspect dagger jewel",
                    "- Back to Council: accuse wrong person",
                    "- Kill council, get reward");
        }

        if (ContainsAll(description, "The Prototype", "Prototype Claw", "Prototype Memory Core"))
        {
            var reward = description.StartsWith("Given by", StringComparison.OrdinalIgnoreCase)
                ? zh ? "交付原型機 NPC" : "Return to Prototype NPC"
                : zh ? "打原型機畸變體" : "Defeat Prototype aberration";
            var memory = description.Contains("blue state", StringComparison.OrdinalIgnoreCase)
                ? zh ? "記憶核心：藍色" : "Memory Core: blue"
                : zh ? "記憶核心：紅色預設" : "Memory Core: default red";

            return zh
                ? Lines(
                    "地點：涅魯德 / 農學區",
                    "- 收集：頭 / 手臂 / 爪 / 記憶核心",
                    "- 頭：靜謐眺望點旁墜車處",
                    "- 手臂：前方休息站滑翔，右側洞穴",
                    "- 爪：枯萎墓地，園丁左路 -> 滑翔 -> 橋",
                    "- 記憶核心：枯萎墓地滑翔，下峽谷小平台",
                    $"- {memory}",
                    $"- {reward}")
                : Lines(
                    "Location: N'Erud / Agronomy Sector",
                    "- Collect: Head / Arm / Claw / Memory Core",
                    "- Head: crash site near Quiescent Overlook",
                    "- Arm: Anterior Recess glider -> right cave",
                    "- Claw: Withered Necropolis -> Gardener left path -> bridge",
                    "- Memory Core: Withered Necropolis glider -> canyon platform",
                    $"- {memory}",
                    $"- {reward}");
        }

        if (ContainsAll(description, "Ethereal Manor", "Liquid Escape", "must not die"))
        {
            return zh
                ? Lines(
                    "前置：洛斯曼 / 虛幻莊園",
                    "- 被 Dran 抓醒",
                    "- 用 Liquid Escape 死亡，回 13 區",
                    "- 舊 Ward 大門找 Dran，對話講完",
                    "- 之後不能死",
                    "- 路線：紅王座 -> 破碎入口 -> 塔爾拉薩避難所",
                    "- 再去：灰燼荒原 -> 痛苦療養院",
                    "- 拿 Dran's Dream，使用進材料區")
                : Lines(
                    "Start: Losomn / Ethereal Manor",
                    "- Get grabbed by Dran",
                    "- Liquid Escape -> Ward 13",
                    "- Old Ward big door: exhaust Dran dialogue",
                    "- Do not die after this",
                    "- Route: Red Throne -> Fractured Ingress -> Tal'Ratha's Refuge",
                    "- Then: Ashen Wasteland -> Tormented Asylum",
                    "- Get Dran's Dream, use it for material area");
        }

        if (ContainsAll(description, "Decorum Cipher", "Memory Core II", "Drzyr Replicator"))
        {
            var both = description.Contains("Insert both", StringComparison.OrdinalIgnoreCase);
            return zh
                ? Lines(
                    "地點：涅魯德 / Ascension Spire 或農學區機關",
                    both ? "- 插入：Decorum Cipher + Memory Core II" : "- 插入：Decorum Cipher 或 Memory Core II",
                    "- Decorum Cipher：Terminus Station 火車事件後，爬梯進車頂",
                    "- Memory Core II：Dormant N'Erudian Facility",
                    "- 找缺口跳下 -> 電梯 -> 控制室拿 Biome-Control Glyph",
                    "- 回入口用 Glyph 開門，拿 Memory Core II",
                    "- 回機關開門，拿物品")
                : Lines(
                    "Location: N'Erud / Ascension Spire or Agronomy fixture",
                    both ? "- Insert: Decorum Cipher + Memory Core II" : "- Insert: Decorum Cipher or Memory Core II",
                    "- Decorum Cipher: Terminus Station train event -> ladder -> train roof",
                    "- Memory Core II: Dormant N'Erudian Facility",
                    "- Railing gap -> elevator -> control room: Biome-Control Glyph",
                    "- Entrance door with Glyph -> Memory Core II",
                    "- Back to fixture, open item room");
        }

        if (ContainsAll(description, "Thaen Seed", "Thaen Tree Fruit Ripening"))
        {
            return zh
                ? Lines(
                    "地點：耶莎 / 紅王座線",
                    "- 寡婦王庭：拿 Ornate Key + Ornate Lockbox",
                    "- 檢查盒子，用鑰匙打開，拿 Thaen Seed",
                    "- 回 13 區花園種下",
                    "- 等 2 天長樹",
                    "- 果實成熟：1 天 Mature / 2 天 Elder / 3 天 Celestial",
                    "- 第 4 次 Celestial 改長 Ripened Heart")
                : Lines(
                    "Location: Yaesha / Red Throne route",
                    "- Widow's Court: Ornate Key + Ornate Lockbox",
                    "- Inspect box, open with key -> Thaen Seed",
                    "- Plant in Ward 13 garden",
                    "- Wait 2 days for tree",
                    "- Fruit stages: 1 day Mature / 2 Elder / 3 Celestial",
                    "- 4th Celestial becomes Ripened Heart");
        }

        if (ContainsAll(description, "Fortune Hunter", "The Backrooms", "Strange Box"))
        {
            return zh
                ? Lines(
                    "地點：迷宮 / 腐化門",
                    "- 裝指定全套：探索者 Fortune Hunter、入侵者 Wormhole",
                    "- Realmwalker、Void Heart、Ford's Scattergun、Cube Gun、Labyrinth Staff",
                    "- Leto's Amulet、Amber Moonstone、Black Cat Band",
                    "- Anastasija's Inspiration、Zania's Malice",
                    "- 循環傳送門：看到墜落畫面，等約 2 秒進入",
                    "- 開腐化門 -> Backrooms",
                    "- 找 Strange Box")
                : Lines(
                    "Location: Labyrinth / Corrupted door",
                    "- Equip full required loadout",
                    "- Explorer Fortune Hunter + Invader Wormhole",
                    "- Realmwalker, Void Heart, Ford's Scattergun, Cube Gun, Labyrinth Staff",
                    "- Leto's Amulet, Amber Moonstone, Black Cat Band",
                    "- Anastasija's Inspiration, Zania's Malice",
                    "- Cycling portal: drop scene, wait ~2 sec, enter",
                    "- Open Corrupted door -> Backrooms -> Strange Box");
        }

        if (ContainsAll(description, "purple pillars", "Agronomy Sector", "Withered Necropolis"))
        {
            return zh
                ? Lines(
                    "地點：涅魯德 / 農學區 + 枯萎墓地",
                    "- 啟動 3 根紫色柱旁控制台",
                    "- 農學區 2 根，枯萎墓地 1 根",
                    "- 前方休息站：出門右路 -> 滑翔下去",
                    "- Propagation Field：左轉穿玉米田 -> 滑翔到對面",
                    "- Waylaid Conservatory：滑翔下峽谷，連吃風柱到小平台")
                : Lines(
                    "Location: N'Erud / Agronomy Sector + Withered Necropolis",
                    "- Activate consoles at 3 purple pillars",
                    "- 2 in Agronomy Sector, 1 in Withered Necropolis",
                    "- Anterior Recess: outside right path -> glide down",
                    "- Propagation Field: left through field -> glide across",
                    "- Waylaid Conservatory: glide canyon, use wind plumes to small platform");
        }

        return null;
    }

    private static string? TryBuildStructuredHint(string description, bool zh)
    {
        var text = description.Trim();

        if (TryBuildRandomDropHint(text, zh, out var randomDrop))
        {
            return randomDrop;
        }

        var buy = BuyFromRegex.Match(text);
        if (buy.Success)
        {
            var lines = new List<string>
            {
                zh
                    ? $"購買：{LocalizeTerms(buy.Groups["vendor"].Value)} / {LocalizeTerms(buy.Groups["place"].Value)}"
                    : $"Buy: {buy.Groups["vendor"].Value} / {buy.Groups["place"].Value}"
            };

            var condition = buy.Groups["condition"].Value;
            if (!string.IsNullOrWhiteSpace(condition))
            {
                lines.Add(zh ? $"- 條件：{LocalizeTerms(TrimPeriod(condition))}" : $"- Requirement: {TrimPeriod(condition)}");
            }

            return Lines(lines);
        }

        var crafted = CraftedAtRegex.Match(text);
        if (crafted.Success)
        {
            var lines = new List<string>
            {
                zh
                    ? $"製作：{LocalizeTerms(crafted.Groups["station"].Value)}"
                    : $"Craft: {crafted.Groups["station"].Value}"
            };

            var materials = crafted.Groups["materials"].Value;
            if (!string.IsNullOrWhiteSpace(materials))
            {
                lines.Add(zh ? $"- 材料：{LocalizeTerms(TrimPeriod(materials))}" : $"- Materials: {TrimPeriod(materials)}");
            }

            return Lines(lines);
        }

        var given = GivenByRegex.Match(text);
        if (given.Success)
        {
            return BuildSourceLocationHint(
                zh ? $"取得：{LocalizeTerms(given.Groups["npc"].Value)}" : $"Get from: {given.Groups["npc"].Value}",
                given.Groups["world"].Value,
                given.Groups["location"].Value,
                text[given.Length..],
                zh);
        }

        var drops = DropsFromLocationRegex.Match(text);
        if (drops.Success)
        {
            return BuildSourceLocationHint(
                zh ? $"掉落：{LocalizeTerms(drops.Groups["enemy"].Value)}" : $"Drop: {drops.Groups["enemy"].Value}",
                drops.Groups["world"].Value,
                drops.Groups["location"].Value,
                text[drops.Length..],
                zh);
        }

        var foundSingle = FoundInLocationRegex.Match(text);
        if (foundSingle.Success)
        {
            return BuildSourceLocationHint(
                null,
                foundSingle.Groups["world"].Value,
                foundSingle.Groups["location"].Value,
                text[foundSingle.Length..],
                zh);
        }

        var foundMulti = FoundInLocationsRegex.Match(text);
        if (foundMulti.Success)
        {
            return BuildSourceLocationHint(
                null,
                foundMulti.Groups["world"].Value,
                foundMulti.Groups["locations"].Value,
                text[foundMulti.Length..],
                zh);
        }

        return null;
    }

    private static bool TryBuildRandomDropHint(string text, bool zh, out string hint)
    {
        var normalized = TrimPeriod(text);
        if (normalized.Equals("Random drop", StringComparison.OrdinalIgnoreCase))
        {
            hint = zh ? "隨機掉落" : "Random drop";
            return true;
        }

        var randomWorld = Regex.Match(
            normalized,
            @"^Random (?<world>N'Erud|Yaesha|Losomn) drop$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        if (randomWorld.Success)
        {
            var world = LocalizeWorld(randomWorld.Groups["world"].Value, zh);
            hint = zh ? $"隨機掉落：{world}" : $"Random drop: {world}";
            return true;
        }

        if (normalized.Equals("Drops from a random aberration on a random world", StringComparison.OrdinalIgnoreCase))
        {
            hint = zh ? "隨機世界：擊敗隨機畸變體" : "Random world: defeat random aberration";
            return true;
        }

        hint = string.Empty;
        return false;
    }

    private static string BuildSourceLocationHint(string? sourceLine, string world, string location, string remaining, bool zh)
    {
        var lines = new List<string>
        {
            zh
                ? $"地點：{LocalizeWorld(world, true)} / {LocalizeTerms(location)}"
                : $"Location: {world} / {location}"
        };

        if (!string.IsNullOrWhiteSpace(sourceLine))
        {
            lines.Add("- " + sourceLine);
        }

        lines.AddRange(BuildKeywordSteps(remaining, zh));
        return Lines(lines);
    }

    private static string? TryBuildFallbackHint(string description, bool zh)
    {
        var steps = BuildKeywordSteps(description, zh);
        if (steps.Count == 0)
        {
            return null;
        }

        return Lines(steps);
    }

    private static List<string> BuildKeywordSteps(string text, bool zh)
    {
        var steps = new List<string>();
        foreach (var sentence in text.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var step = zh ? SimplifyZhSentence(sentence) : SimplifyEnglishSentence(sentence);
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

    private static string SimplifyZhSentence(string sentence)
    {
        var step = sentence.Trim();
        if (step.Length == 0)
        {
            return string.Empty;
        }

        step = Regex.Replace(step, @"^Afterwards,?\s+", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        step = Regex.Replace(step, @"^There,?\s+", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        step = Regex.Replace(step, @"^This will\s+", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        step = Regex.Replace(step, @"^You need to\s+", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        step = Regex.Replace(step, @"^You will need to\s+", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        var action = TryMatchAction(step);
        if (action is not null)
        {
            return Truncate(LocalizeTerms(action), 92);
        }

        return "線索：" + Truncate(LocalizeTerms(step), 86);
    }

    private static string SimplifyEnglishSentence(string sentence)
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

        return Truncate(step, 110);
    }

    private static string? TryMatchAction(string step)
    {
        var patterns = new (string Pattern, string Label)[]
        {
            (@"^Talk to (?<value>.+)$", "對話"),
            (@"^Speak to (?<value>.+)$", "對話"),
            (@"^Agree to (?<value>.+)$", "答應"),
            (@"^Defeat (?<value>.+)$", "擊敗"),
            (@"^Kill (?<value>.+)$", "擊敗"),
            (@"^Drops? from (?<value>.+)$", "掉落"),
            (@"^Complete (?<value>.+)$", "完成"),
            (@"^Finish (?<value>.+)$", "完成"),
            (@"^Use (?<value>.+)$", "使用"),
            (@"^Interact with (?<value>.+)$", "互動"),
            (@"^Pick up (?<value>.+)$", "拿取"),
            (@"^Take (?<value>.+)$", "拿取"),
            (@"^Return to (?<value>.+)$", "返回"),
            (@"^Go to (?<value>.+)$", "前往"),
            (@"^Go (?<value>.+)$", "前往"),
            (@"^Head (?<value>.+)$", "前往"),
            (@"^Insert (?<value>.+)$", "插入"),
            (@"^Put (?<value>.+)$", "放置"),
            (@"^Open (?<value>.+)$", "開啟"),
            (@"^Crawl (?<value>.+)$", "移動"),
            (@"^Drop (?<value>.+)$", "跳下"),
            (@"^Equip: (?<value>.+)$", "裝備")
        };

        foreach (var (pattern, label) in patterns)
        {
            var match = Regex.Match(step, pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return $"{label}：{match.Groups["value"].Value}";
            }
        }

        var give = Regex.Match(step, @"^Give (?<item>.+?) to (?<target>.+)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        if (give.Success)
        {
            return $"交付：{give.Groups["item"].Value} -> {give.Groups["target"].Value}";
        }

        return null;
    }

    private static string LocalizeTerms(string text)
    {
        var result = text.Trim();
        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Ward 13"] = "13 區",
            ["The Labyrinth"] = "迷宮",
            ["Labyrinth"] = "迷宮",
            ["Root Earth"] = "根蔓地球",
            ["Ashen Wasteland"] = "灰燼荒原",
            ["Forlorn Coast"] = "荒寂海岸",
            ["Council Chamber"] = "議會廳",
            ["Council Tribunal"] = "議會法庭",
            ["The Lament"] = "挽歌",
            ["Endaira's End"] = "恩戴拉終結",
            ["Agronomy Sector"] = "農學區",
            ["Withered Necropolis"] = "枯萎墓地",
            ["Quiescent Overlook"] = "靜謐眺望點",
            ["Anterior Recess"] = "前方休息站",
            ["Waylaid Conservatory"] = "滯留溫室",
            ["Dormant N'Erudian Facility"] = "休眠涅魯德設施",
            ["Terminus Station"] = "終點站",
            ["Red Throne"] = "紅王座",
            ["The Widow's Court"] = "寡婦王庭",
            ["Tormented Asylum"] = "痛苦療養院",
            ["Fractured Ingress"] = "破碎入口",
            ["Tal'Ratha's Refuge"] = "塔爾拉薩避難所",
            ["world stone"] = "世界石",
            ["checkpoint"] = "檢查點",
            ["glider"] = "滑翔翼",
            ["elevator"] = "電梯",
            ["lift"] = "電梯",
            ["quest item"] = "任務道具",
            ["aberration"] = "畸變體",
            ["random world"] = "隨機世界",
            ["random drop"] = "隨機掉落",
            ["completing the campaign on Veteran difficulty"] = "Veteran 難度通關戰役",
            ["finishing the campaign on any difficulty"] = "任意難度通關戰役",
            ["finishing the campaign"] = "通關戰役",
            ["completing the campaign"] = "通關戰役",
            ["Hardcore Veteran"] = "專家 Veteran",
            ["Veteran difficulty"] = "Veteran 難度",
            ["Nightmare difficulty"] = "Nightmare 難度",
            ["Apocalypse difficulty"] = "Apocalypse 難度"
        };

        foreach (var (source, target) in replacements)
        {
            result = Regex.Replace(result, Regex.Escape(source), target, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        }

        result = result.Replace("N'Erud", "涅魯德", StringComparison.OrdinalIgnoreCase)
            .Replace("Yaesha", "耶莎", StringComparison.OrdinalIgnoreCase)
            .Replace("Losomn", "洛斯曼", StringComparison.OrdinalIgnoreCase);

        return TrimPeriod(result);
    }

    private static string LocalizeWorld(string world, bool zh)
    {
        if (!zh)
        {
            return world;
        }

        return world.Trim() switch
        {
            "Losomn" => "洛斯曼",
            "Yaesha" => "耶莎",
            "N'Erud" => "涅魯德",
            "Root Earth" => "根蔓地球",
            "Ward 13" => "13 區",
            "The Labyrinth" => "迷宮",
            "Labyrinth" => "迷宮",
            _ => LocalizeTerms(world)
        };
    }

    private static string TrimPeriod(string text)
    {
        return text.Trim().TrimEnd('.');
    }

    private static string Truncate(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }

        return text[..Math.Max(0, maxLength - 3)].TrimEnd() + "...";
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

    private static string Lines(params string[] lines)
    {
        return string.Join(Environment.NewLine, lines);
    }

    private static string Lines(IEnumerable<string> lines)
    {
        return string.Join(Environment.NewLine, lines);
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
