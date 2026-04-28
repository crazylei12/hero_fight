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
}
