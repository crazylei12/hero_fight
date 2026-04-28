using Fight.Data;
using UnityEngine;

namespace Fight.Core
{
    public static class AthleteCombatModifierResolver
    {
        private const float AttackScoreToModifier = 0.005f;
        private const float DefenseScoreToHealthModifier = 0.005f;
        private const float ConditionToAttackSpeedModifier = 0.002f;
        private const float ConditionToMoveSpeedModifier = 0.001f;
        private const float LateBloomingInitialFinalAttackDefenseModifier = -0.2f;
        private const float LateBloomingFinalAttackDefenseModifierPerSecond = 0.01f;

        public static ResolvedAthleteCombatModifier Resolve(AthleteDefinition athlete, HeroDefinition hero)
        {
            if (athlete == null)
            {
                return ResolvedAthleteCombatModifier.None;
            }

            var baseAttackScore = Mathf.Clamp(athlete.attack, 0f, 50f);
            var baseDefenseScore = Mathf.Clamp(athlete.defense, 0f, 50f);
            var conditionScore = Mathf.Clamp(athlete.condition, -50f, 50f);
            var masteryScore = athlete.TryGetMastery(hero, out var matchedMastery)
                ? Mathf.Clamp(matchedMastery, 0f, 50f)
                : 0f;

            var effectiveAttackScore = Mathf.Clamp(baseAttackScore + masteryScore, 0f, 100f);
            var effectiveDefenseScore = Mathf.Clamp(baseDefenseScore + masteryScore, 0f, 100f);

            var attackPowerModifier = Mathf.Clamp(effectiveAttackScore * AttackScoreToModifier, 0f, 0.5f);
            var maxHealthModifier = Mathf.Clamp(effectiveDefenseScore * DefenseScoreToHealthModifier, 0f, 0.5f);
            var attackSpeedModifier = Mathf.Clamp(conditionScore * ConditionToAttackSpeedModifier, -0.15f, 0.2f);
            var moveSpeedModifier = Mathf.Clamp(conditionScore * ConditionToMoveSpeedModifier, -0.08f, 0.08f);
            var hasLateBlooming = AthleteTraitCatalog.HasTrait(athlete, AthleteTraitCatalog.LateBloomingTraitId);
            var finalAttackDefenseInitialModifier = hasLateBlooming ? LateBloomingInitialFinalAttackDefenseModifier : 0f;
            var finalAttackDefenseModifierPerSecond = hasLateBlooming ? LateBloomingFinalAttackDefenseModifierPerSecond : 0f;
            var traitSummary = hasLateBlooming ? AthleteTraitCatalog.GetDisplayName(AthleteTraitCatalog.LateBloomingTraitId) : string.Empty;
            var bpFitScore = Mathf.Clamp(Mathf.RoundToInt(30f + (effectiveAttackScore * 0.45f) + (effectiveDefenseScore * 0.35f)), 0, 100);

            var debugBreakdown =
                $"athlete={athlete.displayName}, baseAtk={baseAttackScore:0.#}, baseDef={baseDefenseScore:0.#}, mastery={masteryScore:0.#}, " +
                $"effAtk={effectiveAttackScore:0.#}, effDef={effectiveDefenseScore:0.#}, cond={conditionScore:0.#}, " +
                $"atkMod={attackPowerModifier:P0}, hpMod={maxHealthModifier:P0}, asMod={attackSpeedModifier:P0}, moveMod={moveSpeedModifier:P0}, " +
                $"trait={traitSummary}, finalAtkDefStart={finalAttackDefenseInitialModifier:P0}, finalAtkDefPerSec={finalAttackDefenseModifierPerSecond:P0}, fit={bpFitScore}";

            return new ResolvedAthleteCombatModifier(
                athlete,
                masteryScore,
                effectiveAttackScore,
                effectiveDefenseScore,
                attackPowerModifier,
                maxHealthModifier,
                attackSpeedModifier,
                moveSpeedModifier,
                finalAttackDefenseInitialModifier,
                finalAttackDefenseModifierPerSecond,
                traitSummary,
                bpFitScore,
                debugBreakdown);
        }
    }
}
