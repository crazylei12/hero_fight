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
        private const float FavoriteSidePreferredScoreModifier = 20f;
        private const float FavoriteSideOppositeScoreModifier = -5f;
        private const float WindStepMoveSpeedModifier = 0.1f;
        private const float FastHandsAttackSpeedModifier = 0.1f;
        private const float HeavyShieldDefenseScoreModifier = 20f;
        private const float HeavyShieldMoveSpeedModifier = -0.1f;
        private const float MediumShieldDefenseScoreModifier = 15f;
        private const float MediumShieldMoveSpeedModifier = -0.05f;
        private const float LightShieldDefenseScoreModifier = 10f;

        public static ResolvedAthleteCombatModifier Resolve(AthleteDefinition athlete, HeroDefinition hero, TeamSide side = TeamSide.None)
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

            var traitAttackScoreModifier = 0f;
            var traitDefenseScoreModifier = 0f;
            var traitAttackSpeedModifier = 0f;
            var traitMoveSpeedModifier = 0f;

            ApplySidePreferenceTrait(
                athlete,
                side,
                AthleteTraitCatalog.FavoriteBlueTraitId,
                TeamSide.Blue,
                ref traitAttackScoreModifier,
                ref traitDefenseScoreModifier);
            ApplySidePreferenceTrait(
                athlete,
                side,
                AthleteTraitCatalog.FavoriteRedTraitId,
                TeamSide.Red,
                ref traitAttackScoreModifier,
                ref traitDefenseScoreModifier);

            if (AthleteTraitCatalog.HasTrait(athlete, AthleteTraitCatalog.WindStepTraitId))
            {
                traitMoveSpeedModifier += WindStepMoveSpeedModifier;
            }

            if (AthleteTraitCatalog.HasTrait(athlete, AthleteTraitCatalog.FastHandsTraitId))
            {
                traitAttackSpeedModifier += FastHandsAttackSpeedModifier;
            }

            if (AthleteTraitCatalog.HasTrait(athlete, AthleteTraitCatalog.HeavyShieldTraitId))
            {
                traitDefenseScoreModifier += HeavyShieldDefenseScoreModifier;
                traitMoveSpeedModifier += HeavyShieldMoveSpeedModifier;
            }

            if (AthleteTraitCatalog.HasTrait(athlete, AthleteTraitCatalog.MediumShieldTraitId))
            {
                traitDefenseScoreModifier += MediumShieldDefenseScoreModifier;
                traitMoveSpeedModifier += MediumShieldMoveSpeedModifier;
            }

            if (AthleteTraitCatalog.HasTrait(athlete, AthleteTraitCatalog.LightShieldTraitId))
            {
                traitDefenseScoreModifier += LightShieldDefenseScoreModifier;
            }

            var effectiveAttackScore = Mathf.Clamp(baseAttackScore + traitAttackScoreModifier + masteryScore, 0f, 100f);
            var effectiveDefenseScore = Mathf.Clamp(baseDefenseScore + traitDefenseScoreModifier + masteryScore, 0f, 100f);

            var attackPowerModifier = Mathf.Clamp(effectiveAttackScore * AttackScoreToModifier, 0f, 0.5f);
            var maxHealthModifier = Mathf.Clamp(effectiveDefenseScore * DefenseScoreToHealthModifier, 0f, 0.5f);
            var attackSpeedModifier = Mathf.Clamp(
                (conditionScore * ConditionToAttackSpeedModifier) + traitAttackSpeedModifier,
                -0.15f,
                0.2f);
            var moveSpeedModifier = Mathf.Clamp(
                (conditionScore * ConditionToMoveSpeedModifier) + traitMoveSpeedModifier,
                -0.2f,
                0.2f);
            var hasLateBlooming = AthleteTraitCatalog.HasTrait(athlete, AthleteTraitCatalog.LateBloomingTraitId);
            var finalAttackDefenseInitialModifier = hasLateBlooming ? LateBloomingInitialFinalAttackDefenseModifier : 0f;
            var finalAttackDefenseModifierPerSecond = hasLateBlooming ? LateBloomingFinalAttackDefenseModifierPerSecond : 0f;
            var traitSummary = AthleteTraitCatalog.BuildDisplayNameSummary(athlete);
            var traitDescriptionSummary = AthleteTraitCatalog.BuildDescriptionSummary(athlete);
            var bpFitScore = Mathf.Clamp(Mathf.RoundToInt(30f + (effectiveAttackScore * 0.45f) + (effectiveDefenseScore * 0.35f)), 0, 100);

            var debugBreakdown =
                $"athlete={athlete.displayName}, side={side}, baseAtk={baseAttackScore:0.#}, baseDef={baseDefenseScore:0.#}, mastery={masteryScore:0.#}, " +
                $"traitAtkScore={traitAttackScoreModifier:+0.#;-0.#;0}, traitDefScore={traitDefenseScoreModifier:+0.#;-0.#;0}, " +
                $"effAtk={effectiveAttackScore:0.#}, effDef={effectiveDefenseScore:0.#}, cond={conditionScore:0.#}, " +
                $"atkMod={attackPowerModifier:P0}, hpMod={maxHealthModifier:P0}, asMod={attackSpeedModifier:P0}, moveMod={moveSpeedModifier:P0}, " +
                $"trait={traitSummary}, traitDesc={traitDescriptionSummary}, finalAtkDefStart={finalAttackDefenseInitialModifier:P0}, " +
                $"finalAtkDefPerSec={finalAttackDefenseModifierPerSecond:P0}, fit={bpFitScore}";

            return new ResolvedAthleteCombatModifier(
                athlete,
                masteryScore,
                effectiveAttackScore,
                effectiveDefenseScore,
                traitAttackScoreModifier,
                traitDefenseScoreModifier,
                attackPowerModifier,
                maxHealthModifier,
                attackSpeedModifier,
                moveSpeedModifier,
                finalAttackDefenseInitialModifier,
                finalAttackDefenseModifierPerSecond,
                traitSummary,
                traitDescriptionSummary,
                bpFitScore,
                debugBreakdown);
        }

        private static void ApplySidePreferenceTrait(
            AthleteDefinition athlete,
            TeamSide side,
            string traitId,
            TeamSide preferredSide,
            ref float traitAttackScoreModifier,
            ref float traitDefenseScoreModifier)
        {
            if (!AthleteTraitCatalog.HasTrait(athlete, traitId))
            {
                return;
            }

            var scoreModifier = ResolveSidePreferenceScoreModifier(side, preferredSide);
            traitAttackScoreModifier += scoreModifier;
            traitDefenseScoreModifier += scoreModifier;
        }

        private static float ResolveSidePreferenceScoreModifier(TeamSide side, TeamSide preferredSide)
        {
            if (side == preferredSide)
            {
                return FavoriteSidePreferredScoreModifier;
            }

            return side == TeamSide.Blue || side == TeamSide.Red
                ? FavoriteSideOppositeScoreModifier
                : 0f;
        }
    }
}
