using System;
using System.Collections.Generic;
using System.IO;

namespace Fight.Tools.OfflineSimulationLauncher
{
    internal static class HeroCatalogLoader
    {
        public static List<HeroCatalogEntry> LoadHeroEntries(string csvPath)
        {
            List<HeroCatalogEntry> entries = new List<HeroCatalogEntry>();
            if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
            {
                return entries;
            }

            string[] lines = File.ReadAllLines(csvPath);
            bool headerSeen = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!headerSeen)
                {
                    headerSeen = true;
                    continue;
                }

                string[] columns = SplitCsvLine(line);
                if (columns.Length < 3)
                {
                    continue;
                }

                string heroId = columns[0].Trim();
                string displayName = columns[1].Trim();
                string heroClass = ExtractHeroClass(columns[2]);

                if (string.IsNullOrWhiteSpace(heroId))
                {
                    continue;
                }

                entries.Add(new HeroCatalogEntry
                {
                    HeroId = heroId,
                    DisplayName = string.IsNullOrWhiteSpace(displayName) ? heroId : displayName,
                    HeroClass = heroClass,
                });
            }

            entries.Sort(CompareHeroEntries);
            return entries;
        }

        private static string[] SplitCsvLine(string line)
        {
            List<string> parts = new List<string>();
            if (line == null)
            {
                return parts.ToArray();
            }

            bool inQuotes = false;
            int segmentStart = 0;
            for (int i = 0; i < line.Length; i++)
            {
                char currentChar = line[i];
                if (currentChar == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (currentChar == ',' && !inQuotes)
                {
                    parts.Add(UnwrapCsvValue(line.Substring(segmentStart, i - segmentStart)));
                    segmentStart = i + 1;
                }
            }

            parts.Add(UnwrapCsvValue(line.Substring(segmentStart)));
            return parts.ToArray();
        }

        private static string UnwrapCsvValue(string value)
        {
            string trimmed = value == null ? string.Empty : value.Trim();
            if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[trimmed.Length - 1] == '"')
            {
                trimmed = trimmed.Substring(1, trimmed.Length - 2);
            }

            return trimmed.Replace("\"\"", "\"");
        }

        private static string ExtractHeroClass(string rawHeroClass)
        {
            if (string.IsNullOrWhiteSpace(rawHeroClass))
            {
                return string.Empty;
            }

            string[] splitValues = rawHeroClass.Split('|');
            return splitValues.Length > 0 ? splitValues[0].Trim() : rawHeroClass.Trim();
        }

        private static int CompareHeroEntries(HeroCatalogEntry left, HeroCatalogEntry right)
        {
            string leftClass = left != null ? left.HeroClass : string.Empty;
            string rightClass = right != null ? right.HeroClass : string.Empty;
            int classComparison = string.Compare(leftClass, rightClass, StringComparison.OrdinalIgnoreCase);
            if (classComparison != 0)
            {
                return classComparison;
            }

            string leftHeroId = left != null ? left.HeroId : string.Empty;
            string rightHeroId = right != null ? right.HeroId : string.Empty;
            return string.Compare(leftHeroId, rightHeroId, StringComparison.OrdinalIgnoreCase);
        }
    }
}
