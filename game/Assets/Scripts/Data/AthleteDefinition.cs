using System.Collections.Generic;
using UnityEngine;

namespace Fight.Data
{
    [System.Serializable]
    public class AthleteDefinition
    {
        public string athleteId;
        public string displayName;
        public string teamName;
        public Sprite portrait;

        [Range(0f, 50f)] public float attack;
        [Range(0f, 50f)] public float defense;
        [Range(-50f, 50f)] public float condition;

        public List<HeroMasteryEntry> heroMasteries = new List<HeroMasteryEntry>();
        public List<string> traitIds = new List<string>();

        public bool TryGetMastery(HeroDefinition heroDefinition, out float mastery)
        {
            var heroId = heroDefinition != null ? heroDefinition.heroId : string.Empty;
            return TryGetMastery(heroId, out mastery);
        }

        public bool TryGetMastery(string heroId, out float mastery)
        {
            mastery = 0f;
            if (string.IsNullOrWhiteSpace(heroId) || heroMasteries == null)
            {
                return false;
            }

            for (var i = 0; i < heroMasteries.Count; i++)
            {
                var entry = heroMasteries[i];
                if (entry == null || !string.Equals(entry.heroId, heroId, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                mastery = Mathf.Clamp(entry.mastery, 0f, 50f);
                return true;
            }

            return false;
        }
    }

    public static class AthleteTraitCatalog
    {
        public const string LateBloomingTraitId = "late_blooming";
        public const string FavoriteBlueTraitId = "favorite_blue";
        public const string FavoriteRedTraitId = "favorite_red";
        public const string WindStepTraitId = "wind_step";
        public const string FastHandsTraitId = "fast_hands";
        public const string HeavyShieldTraitId = "heavy_shield";
        public const string MediumShieldTraitId = "medium_shield";
        public const string LightShieldTraitId = "light_shield";

        public static bool HasTrait(AthleteDefinition athlete, string traitId)
        {
            if (athlete?.traitIds == null || string.IsNullOrWhiteSpace(traitId))
            {
                return false;
            }

            for (var i = 0; i < athlete.traitIds.Count; i++)
            {
                if (string.Equals(athlete.traitIds[i], traitId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static string GetDisplayName(string traitId)
        {
            if (string.IsNullOrWhiteSpace(traitId))
            {
                return string.Empty;
            }

            return traitId.ToLowerInvariant() switch
            {
                LateBloomingTraitId => "大器晚成",
                FavoriteBlueTraitId => "钟爱蓝色",
                FavoriteRedTraitId => "钟爱红色",
                WindStepTraitId => "疾风步",
                FastHandsTraitId => "快手",
                HeavyShieldTraitId => "重盾",
                MediumShieldTraitId => "中盾",
                LightShieldTraitId => "轻盾",
                _ => traitId,
            };
        }

        public static string GetDescription(string traitId)
        {
            if (string.IsNullOrWhiteSpace(traitId))
            {
                return string.Empty;
            }

            return traitId.ToLowerInvariant() switch
            {
                LateBloomingTraitId => "战斗开始时最终攻击力和防御力 -20%，之后每秒提升 1%。",
                FavoriteBlueTraitId => "蓝色方时选手攻击和防御数值 +20，红色方时 -5。",
                FavoriteRedTraitId => "红色方时选手攻击和防御数值 +20，蓝色方时 -5。",
                WindStepTraitId => "英雄移动速度 +10%。",
                FastHandsTraitId => "英雄攻击速度 +10%。",
                HeavyShieldTraitId => "选手防御数值 +20，英雄移动速度 -10%。",
                MediumShieldTraitId => "选手防御数值 +15，英雄移动速度 -5%。",
                LightShieldTraitId => "选手防御数值 +10。",
                _ => string.Empty,
            };
        }

        public static string GetCombatDescription(string traitId)
        {
            return GetDescription(traitId);
        }

        public static string BuildDisplayNameSummary(AthleteDefinition athlete, int maxCount = int.MaxValue)
        {
            return BuildTraitSummary(athlete, maxCount, false);
        }

        public static string BuildDescriptionSummary(AthleteDefinition athlete, int maxCount = int.MaxValue)
        {
            return BuildTraitSummary(athlete, maxCount, true);
        }

        private static string BuildTraitSummary(AthleteDefinition athlete, int maxCount, bool useDescription)
        {
            if (athlete?.traitIds == null || athlete.traitIds.Count == 0 || maxCount <= 0)
            {
                return string.Empty;
            }

            var labels = new List<string>();
            for (var i = 0; i < athlete.traitIds.Count && labels.Count < maxCount; i++)
            {
                var label = useDescription
                    ? GetDescription(athlete.traitIds[i])
                    : GetDisplayName(athlete.traitIds[i]);
                if (!string.IsNullOrWhiteSpace(label))
                {
                    labels.Add(label);
                }
            }

            return labels.Count > 0 ? string.Join(" / ", labels) : string.Empty;
        }
    }
}
