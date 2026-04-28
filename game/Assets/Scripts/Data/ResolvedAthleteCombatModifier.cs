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
            0,
            string.Empty);

        public ResolvedAthleteCombatModifier(
            AthleteDefinition athlete,
            float masteryScore,
            float effectiveAttackScore,
            float effectiveDefenseScore,
            float attackPowerModifier,
            float maxHealthModifier,
            float attackSpeedModifier,
            float moveSpeedModifier,
            int bpFitScore,
            string debugBreakdown)
        {
            Athlete = athlete;
            MasteryScore = Mathf.Max(0f, masteryScore);
            EffectiveAttackScore = Mathf.Max(0f, effectiveAttackScore);
            EffectiveDefenseScore = Mathf.Max(0f, effectiveDefenseScore);
            AttackPowerModifier = Mathf.Clamp(attackPowerModifier, 0f, 0.5f);
            MaxHealthModifier = Mathf.Clamp(maxHealthModifier, 0f, 0.5f);
            AttackSpeedModifier = Mathf.Clamp(attackSpeedModifier, -0.15f, 0.2f);
            MoveSpeedModifier = Mathf.Clamp(moveSpeedModifier, -0.08f, 0.08f);
            BpFitScore = Mathf.Clamp(bpFitScore, 0, 100);
            DebugBreakdown = debugBreakdown ?? string.Empty;
        }

        public AthleteDefinition Athlete { get; }

        public float MasteryScore { get; }

        public float EffectiveAttackScore { get; }

        public float EffectiveDefenseScore { get; }

        public float AttackPowerModifier { get; }

        public float MaxHealthModifier { get; }

        public float AttackSpeedModifier { get; }

        public float MoveSpeedModifier { get; }

        public int BpFitScore { get; }

        public string DebugBreakdown { get; }

        public bool HasAthlete => Athlete != null;

        public float AttackPowerMultiplier => 1f + AttackPowerModifier;

        public float MaxHealthMultiplier => 1f + MaxHealthModifier;

        public float AttackSpeedMultiplier => Mathf.Max(0.1f, 1f + AttackSpeedModifier);

        public float MoveSpeedMultiplier => Mathf.Max(0.1f, 1f + MoveSpeedModifier);
    }
}
