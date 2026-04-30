using Fight.Data;
using Fight.Heroes;

namespace Fight.Battle
{
    public static class BattlePassiveSpawnTriggerSystem
    {
        public static void ApplyInitialSpawn(BattleContext context, RuntimeHero source)
        {
            ApplySpawnTrigger(context, source, isInitialSpawn: true);
        }

        public static void ApplyRevive(BattleContext context, RuntimeHero source)
        {
            ApplySpawnTrigger(context, source, isInitialSpawn: false);
        }

        private static void ApplySpawnTrigger(BattleContext context, RuntimeHero source, bool isInitialSpawn)
        {
            if (context?.Heroes == null
                || source?.Definition == null
                || source.IsClone
                || source.IsDead)
            {
                return;
            }

            ApplySkillSpawnTrigger(context, source, source.Definition.activeSkill, isInitialSpawn);
            ApplySkillSpawnTrigger(context, source, source.Definition.ultimateSkill, isInitialSpawn);
        }

        private static void ApplySkillSpawnTrigger(
            BattleContext context,
            RuntimeHero source,
            SkillData skill,
            bool isInitialSpawn)
        {
            var passiveSkill = skill?.passiveSkill;
            if (passiveSkill == null
                || !passiveSkill.HasSpawnTriggerStatusEffects
                || (isInitialSpawn && !passiveSkill.triggerStatusEffectsOnInitialSpawn)
                || (!isInitialSpawn && !passiveSkill.triggerStatusEffectsOnRevive))
            {
                return;
            }

            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var target = context.Heroes[i];
                if (!IsValidEnemyHeroTarget(source, target))
                {
                    continue;
                }

                ApplyStatuses(context, source, target, skill, passiveSkill.spawnTriggerStatusEffects);
            }
        }

        private static void ApplyStatuses(
            BattleContext context,
            RuntimeHero source,
            RuntimeHero target,
            SkillData skill,
            System.Collections.Generic.IReadOnlyList<StatusEffectData> statusEffects)
        {
            if (statusEffects == null)
            {
                return;
            }

            for (var i = 0; i < statusEffects.Count; i++)
            {
                var status = statusEffects[i];
                if (status == null)
                {
                    continue;
                }

                var previousShield = status.effectType == StatusEffectType.Shield
                    ? StatusEffectSystem.GetTotalShield(target)
                    : 0f;
                if (!target.ApplyStatusEffect(status, source, skill, source, out var appliedStatus))
                {
                    continue;
                }

                var appliedSource = appliedStatus?.Source ?? source;
                BattleStatsSystem.RecordStatusContribution(context, appliedSource, target, status);
                if (status.effectType == StatusEffectType.Shield)
                {
                    var shieldDelta = UnityEngine.Mathf.Max(0f, StatusEffectSystem.GetTotalShield(target) - previousShield);
                    BattleStatsSystem.RecordShieldContribution(context, appliedSource, target, shieldDelta);
                }

                context.EventBus?.Publish(new StatusAppliedEvent(
                    appliedSource,
                    target,
                    status.effectType,
                    status.durationSeconds,
                    appliedStatus?.Magnitude ?? status.magnitude,
                    skill,
                    appliedStatus?.AppliedBy ?? source));
            }
        }

        private static bool IsValidEnemyHeroTarget(RuntimeHero source, RuntimeHero target)
        {
            return source != null
                && target != null
                && !target.IsClone
                && !target.IsDead
                && target.Side != source.Side
                && target.CanBeDirectTargeted;
        }
    }
}
