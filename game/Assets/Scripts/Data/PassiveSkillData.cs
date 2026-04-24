using System;
using UnityEngine;

namespace Fight.Data
{
    [Serializable]
    public class PassiveSkillData
    {
        [Min(0f)] public float missingHealthAttackPowerRatio = 0f;
        [Min(0f)] public float maxAttackPowerBonus = 0f;
        [Range(0f, 1f)] public float lowHealthLifestealThreshold = 0f;
        [Min(0f)] public float lowHealthLifestealRatio = 0f;
        [Min(0f)] public float recentDirectHostileSourceWindowSeconds = 0f;
        [Min(0f)] public float recentDirectHostileSourceDefenseBonusPerSource = 0f;
        [Min(0f)] public float maxDefenseBonus = 0f;
        [Min(0f)] public float periodicSelfHealIntervalSeconds = 0f;
        [Range(0f, 1f)] public float periodicSelfHealMidHealthThreshold = 0f;
        [Range(0f, 1f)] public float periodicSelfHealLowHealthThreshold = 0f;
        [Range(0f, 1f)] public float periodicSelfHealHighHealthPercentMaxHealth = 0f;
        [Range(0f, 1f)] public float periodicSelfHealMidHealthPercentMaxHealth = 0f;
        [Range(0f, 1f)] public float periodicSelfHealLowHealthPercentMaxHealth = 0f;
        public bool rejectExternalPositiveEffects;
        [Min(0f)] public float killParticipationAttackPowerBonusPerStack = 0f;
        [Min(0f)] public float killParticipationAttackSpeedBonusPerStack = 0f;
        [Range(0f, 1f)] public float killParticipationHealPercentMaxHealth = 0f;
        [Min(0)] public int killParticipationMaxStacks = 0;

        public bool HasMissingHealthAttackPowerBonus =>
            missingHealthAttackPowerRatio > Mathf.Epsilon
            && maxAttackPowerBonus > Mathf.Epsilon;

        public bool HasLowHealthLifesteal =>
            lowHealthLifestealThreshold > Mathf.Epsilon
            && lowHealthLifestealRatio > Mathf.Epsilon;

        public bool HasRecentDirectHostileSourceDefenseBonus =>
            recentDirectHostileSourceWindowSeconds > Mathf.Epsilon
            && recentDirectHostileSourceDefenseBonusPerSource > Mathf.Epsilon
            && maxDefenseBonus > Mathf.Epsilon;

        public bool HasPeriodicSelfHeal =>
            periodicSelfHealIntervalSeconds > Mathf.Epsilon
            && (periodicSelfHealHighHealthPercentMaxHealth > Mathf.Epsilon
                || periodicSelfHealMidHealthPercentMaxHealth > Mathf.Epsilon
                || periodicSelfHealLowHealthPercentMaxHealth > Mathf.Epsilon);

        public bool HasKillParticipationTrigger =>
            killParticipationMaxStacks > 0
            && (killParticipationAttackPowerBonusPerStack > Mathf.Epsilon
                || killParticipationAttackSpeedBonusPerStack > Mathf.Epsilon
                || killParticipationHealPercentMaxHealth > Mathf.Epsilon);

        public float ResolvePeriodicSelfHealPercentMaxHealth(float currentHealthRatio)
        {
            currentHealthRatio = Mathf.Clamp01(currentHealthRatio);
            var lowThreshold = Mathf.Clamp01(periodicSelfHealLowHealthThreshold);
            var midThreshold = Mathf.Clamp(periodicSelfHealMidHealthThreshold, lowThreshold, 1f);
            if (currentHealthRatio <= lowThreshold)
            {
                return Mathf.Max(0f, periodicSelfHealLowHealthPercentMaxHealth);
            }

            if (currentHealthRatio <= midThreshold)
            {
                return Mathf.Max(0f, periodicSelfHealMidHealthPercentMaxHealth);
            }

            return Mathf.Max(0f, periodicSelfHealHighHealthPercentMaxHealth);
        }
    }
}
