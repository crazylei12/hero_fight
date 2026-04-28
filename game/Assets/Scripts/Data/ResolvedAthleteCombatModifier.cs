using UnityEngine;

namespace Fight.Data
{
    public readonly struct ResolvedAthleteCombatModifier
    {
        public static readonly ResolvedAthleteCombatModifier None = new ResolvedAthleteCombatModifier(
            null,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            string.Empty,
            string.Empty,
            0,
            string.Empty);

        public ResolvedAthleteCombatModifier(
            AthleteDefinition athlete,
            float masteryScore,
            float effectiveAttackScore,
            float effectiveDefenseScore,
            float traitAttackScoreModifier,
            float traitDefenseScoreModifier,
            float attackPowerModifier,
            float maxHealthModifier,
            float attackSpeedModifier,
            float moveSpeedModifier,
            float finalAttackDefenseInitialModifier,
            float finalAttackDefenseModifierPerSecond,
            string traitSummary,
            string traitDescriptionSummary,
            int bpFitScore,
            string debugBreakdown)
        {
            Athlete = athlete;
            MasteryScore = Mathf.Max(0f, masteryScore);
            EffectiveAttackScore = Mathf.Max(0f, effectiveAttackScore);
            EffectiveDefenseScore = Mathf.Max(0f, effectiveDefenseScore);
            TraitAttackScoreModifier = traitAttackScoreModifier;
            TraitDefenseScoreModifier = traitDefenseScoreModifier;
            AttackPowerModifier = Mathf.Clamp(attackPowerModifier, 0f, 0.5f);
            MaxHealthModifier = Mathf.Clamp(maxHealthModifier, 0f, 0.5f);
            AttackSpeedModifier = Mathf.Clamp(attackSpeedModifier, -0.15f, 0.2f);
            MoveSpeedModifier = Mathf.Clamp(moveSpeedModifier, -0.2f, 0.2f);
            FinalAttackDefenseInitialModifier = finalAttackDefenseInitialModifier;
            FinalAttackDefenseModifierPerSecond = finalAttackDefenseModifierPerSecond;
            TraitSummary = traitSummary ?? string.Empty;
            TraitDescriptionSummary = traitDescriptionSummary ?? string.Empty;
            BpFitScore = Mathf.Clamp(bpFitScore, 0, 100);
            DebugBreakdown = debugBreakdown ?? string.Empty;
        }

        public AthleteDefinition Athlete { get; }

        public float MasteryScore { get; }

        public float EffectiveAttackScore { get; }

        public float EffectiveDefenseScore { get; }

        public float TraitAttackScoreModifier { get; }

        public float TraitDefenseScoreModifier { get; }

        public float AttackPowerModifier { get; }

        public float MaxHealthModifier { get; }

        public float AttackSpeedModifier { get; }

        public float MoveSpeedModifier { get; }

        public float FinalAttackDefenseInitialModifier { get; }

        public float FinalAttackDefenseModifierPerSecond { get; }

        public string TraitSummary { get; }

        public string TraitDescriptionSummary { get; }

        public int BpFitScore { get; }

        public string DebugBreakdown { get; }

        public bool HasAthlete => Athlete != null;

        public float AttackPowerMultiplier => 1f + AttackPowerModifier;

        public float MaxHealthMultiplier => 1f + MaxHealthModifier;

        public float AttackSpeedMultiplier => Mathf.Max(0.1f, 1f + AttackSpeedModifier);

        public float MoveSpeedMultiplier => Mathf.Max(0.1f, 1f + MoveSpeedModifier);

        public bool HasDynamicFinalAttackDefenseModifier =>
            Mathf.Abs(FinalAttackDefenseInitialModifier) > Mathf.Epsilon
            || Mathf.Abs(FinalAttackDefenseModifierPerSecond) > Mathf.Epsilon;

        public float ResolveFinalAttackDefenseMultiplier(float battleTimeSeconds)
        {
            var modifier = FinalAttackDefenseInitialModifier
                + (Mathf.Max(0f, battleTimeSeconds) * FinalAttackDefenseModifierPerSecond);
            return Mathf.Max(0.1f, 1f + modifier);
        }
    }
}
