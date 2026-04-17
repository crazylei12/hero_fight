using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Fight.Tools.BalanceEditor
{
    internal static class HeroCatalogBuilder
    {
        private static readonly Regex OwnerHeroIdRegex = new Regex(@"\[(?<heroId>[^\]]+)\]", RegexOptions.Compiled);

        public static List<HeroEntryViewModel> Build(CsvTable heroesTable, CsvTable skillsTable, string balanceFolder)
        {
            Dictionary<string, string> chineseNames = HeroDocumentationCatalog.LoadChineseNames(balanceFolder);
            Dictionary<string, List<HeroSkillEntryViewModel>> skillsByHeroId = BuildSkillsByHeroId(skillsTable);

            List<HeroEntryViewModel> heroes = new List<HeroEntryViewModel>();
            foreach (CsvDataRow heroRow in heroesTable.DataRows)
            {
                string heroId = heroesTable.GetValue(heroRow, "heroId");
                if (string.IsNullOrWhiteSpace(heroId))
                {
                    continue;
                }

                string englishName = heroesTable.GetValue(heroRow, "displayName");
                string chineseName;
                if (!chineseNames.TryGetValue(heroId, out chineseName))
                {
                    chineseName = GetFallbackChineseName(heroesTable.GetValue(heroRow, "heroClass"), englishName);
                }

                List<HeroSkillEntryViewModel> skills;
                if (!skillsByHeroId.TryGetValue(heroId, out skills))
                {
                    skills = new List<HeroSkillEntryViewModel>();
                }

                skills.Sort(CompareSkillEntries);
                heroes.Add(new HeroEntryViewModel(heroRow, heroId, englishName, chineseName, skills));
            }

            heroes.Sort(delegate(HeroEntryViewModel left, HeroEntryViewModel right)
            {
                return string.Compare(left.DisplayLabel, right.DisplayLabel, StringComparison.OrdinalIgnoreCase);
            });

            return heroes;
        }

        private static string GetFallbackChineseName(string heroClassValue, string englishName)
        {
            if (!string.IsNullOrWhiteSpace(heroClassValue))
            {
                int separatorIndex = heroClassValue.IndexOf('|');
                if (separatorIndex >= 0 && separatorIndex + 1 < heroClassValue.Length)
                {
                    string chineseClass = heroClassValue.Substring(separatorIndex + 1).Trim();
                    if (!string.IsNullOrWhiteSpace(chineseClass))
                    {
                        return chineseClass;
                    }
                }
            }

            return englishName;
        }

        private static int CompareSkillEntries(HeroSkillEntryViewModel left, HeroSkillEntryViewModel right)
        {
            int slotComparison = GetSlotOrder(left.SlotTypeValue).CompareTo(GetSlotOrder(right.SlotTypeValue));
            if (slotComparison != 0)
            {
                return slotComparison;
            }

            return string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
        }

        private static int GetSlotOrder(string slotTypeValue)
        {
            if (string.IsNullOrWhiteSpace(slotTypeValue))
            {
                return 9;
            }

            string normalized = slotTypeValue.ToLowerInvariant();
            if (normalized.Contains("activeskill") || normalized.Contains("小技能"))
            {
                return 0;
            }

            if (normalized.Contains("ultimate") || normalized.Contains("大招"))
            {
                return 1;
            }

            return 9;
        }

        private static Dictionary<string, List<HeroSkillEntryViewModel>> BuildSkillsByHeroId(CsvTable skillsTable)
        {
            Dictionary<string, List<HeroSkillEntryViewModel>> skillsByHeroId =
                new Dictionary<string, List<HeroSkillEntryViewModel>>(StringComparer.OrdinalIgnoreCase);

            foreach (CsvDataRow skillRow in skillsTable.DataRows)
            {
                string ownerText = skillsTable.GetValue(skillRow, "ownerHeroes");
                if (string.IsNullOrWhiteSpace(ownerText))
                {
                    continue;
                }

                HeroSkillEntryViewModel skillEntry = new HeroSkillEntryViewModel(
                    skillRow,
                    skillsTable.GetValue(skillRow, "skillId"),
                    skillsTable.GetValue(skillRow, "displayName"),
                    skillsTable.GetValue(skillRow, "slotType"),
                    ownerText);

                MatchCollection matches = OwnerHeroIdRegex.Matches(ownerText);
                for (int matchIndex = 0; matchIndex < matches.Count; matchIndex++)
                {
                    string heroId = matches[matchIndex].Groups["heroId"].Value;
                    if (string.IsNullOrWhiteSpace(heroId))
                    {
                        continue;
                    }

                    List<HeroSkillEntryViewModel> entries;
                    if (!skillsByHeroId.TryGetValue(heroId, out entries))
                    {
                        entries = new List<HeroSkillEntryViewModel>();
                        skillsByHeroId.Add(heroId, entries);
                    }

                    entries.Add(skillEntry);
                }
            }

            return skillsByHeroId;
        }
    }

    internal static class HeroDocumentationCatalog
    {
        public static Dictionary<string, string> LoadChineseNames(string balanceFolder)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string docsHeroesFolder = FindDocsHeroesFolder(balanceFolder);
            if (string.IsNullOrWhiteSpace(docsHeroesFolder))
            {
                return result;
            }

            string[] files = Directory.GetFiles(docsHeroesFolder, "*.md", SearchOption.AllDirectories);
            for (int fileIndex = 0; fileIndex < files.Length; fileIndex++)
            {
                string heroId;
                string chineseName;
                if (!TryParseHeroDocument(files[fileIndex], out heroId, out chineseName))
                {
                    continue;
                }

                if (!result.ContainsKey(heroId))
                {
                    result.Add(heroId, chineseName);
                }
            }

            return result;
        }

        private static string FindDocsHeroesFolder(string balanceFolder)
        {
            if (string.IsNullOrWhiteSpace(balanceFolder))
            {
                return string.Empty;
            }

            string currentDirectory = Path.GetFullPath(balanceFolder);
            for (int depth = 0; depth < 8; depth++)
            {
                string candidate = Path.Combine(currentDirectory, "docs", "heroes");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                DirectoryInfo parent = Directory.GetParent(currentDirectory);
                if (parent == null)
                {
                    break;
                }

                currentDirectory = parent.FullName;
            }

            return string.Empty;
        }

        private static bool TryParseHeroDocument(string filePath, out string heroId, out string chineseName)
        {
            heroId = string.Empty;
            chineseName = string.Empty;

            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex].Trim();
                if (line.StartsWith("- 英雄 ID：", StringComparison.Ordinal))
                {
                    heroId = CleanupMarkdownValue(line.Substring("- 英雄 ID：".Length));
                }
                else if (line.StartsWith("- 中文名：", StringComparison.Ordinal))
                {
                    chineseName = CleanupMarkdownValue(line.Substring("- 中文名：".Length));
                }
            }

            return !string.IsNullOrWhiteSpace(heroId) && !string.IsNullOrWhiteSpace(chineseName);
        }

        private static string CleanupMarkdownValue(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return string.Empty;
            }

            return rawValue.Trim().Trim('`').Trim();
        }
    }

    internal sealed class HeroEntryViewModel
    {
        public HeroEntryViewModel(
            CsvDataRow heroRow,
            string heroId,
            string englishName,
            string chineseName,
            List<HeroSkillEntryViewModel> skills)
        {
            HeroRow = heroRow;
            HeroId = heroId ?? string.Empty;
            EnglishName = englishName ?? string.Empty;
            ChineseName = string.IsNullOrWhiteSpace(chineseName) ? EnglishName : chineseName;
            Skills = skills ?? new List<HeroSkillEntryViewModel>();
        }

        public CsvDataRow HeroRow { get; private set; }

        public string HeroId { get; private set; }

        public string EnglishName { get; private set; }

        public string ChineseName { get; private set; }

        public List<HeroSkillEntryViewModel> Skills { get; private set; }

        public string DisplayLabel
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(ChineseName) &&
                    !string.Equals(ChineseName, EnglishName, StringComparison.OrdinalIgnoreCase))
                {
                    return string.Format("{0} ({1})", ChineseName, EnglishName);
                }

                return string.IsNullOrWhiteSpace(ChineseName) ? HeroId : ChineseName;
            }
        }

        public string SearchText
        {
            get
            {
                return string.Format("{0} {1} {2}", HeroId, EnglishName, ChineseName).ToLowerInvariant();
            }
        }

        public override string ToString()
        {
            return DisplayLabel;
        }
    }

    internal sealed class HeroSkillEntryViewModel
    {
        public HeroSkillEntryViewModel(
            CsvDataRow skillRow,
            string skillId,
            string displayName,
            string slotTypeValue,
            string ownerSummary)
        {
            SkillRow = skillRow;
            SkillId = skillId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            SlotTypeValue = slotTypeValue ?? string.Empty;
            OwnerSummary = ownerSummary ?? string.Empty;
        }

        public CsvDataRow SkillRow { get; private set; }

        public string SkillId { get; private set; }

        public string DisplayName { get; private set; }

        public string SlotTypeValue { get; private set; }

        public string OwnerSummary { get; private set; }

        public string SlotLabel
        {
            get
            {
                int separatorIndex = SlotTypeValue.IndexOf('|');
                return separatorIndex >= 0
                    ? SlotTypeValue.Substring(separatorIndex + 1).Trim()
                    : SlotTypeValue;
            }
        }
    }
}
