using System;

namespace Fight.Data
{
    // Keep explicit values stable so existing ScriptableObject assets do not remap enum values.
    public enum StatusEffectType
    {
        None = 0,
        Stun = 1,
        AttackPowerModifier = 2,
        DefenseModifier = 3,
        AttackSpeedModifier = 4,
        MoveSpeedModifier = 5,
        HealOverTime = 6,
        MaxHealthModifier = 7,
        CriticalChanceModifier = 8,
        CriticalDamageModifier = 9,
        AttackRangeModifier = 10,
        KnockUp = 11,
        Invulnerable = 12,
        Untargetable = 13,
        DamageOverTime = 14,
        Shield = 15,
        DamageShare = 16,
        Taunt = 17,
        HealTakenModifier = 18,
    }

    [Flags]
    public enum StatusBehaviorFlags
    {
        None = 0,
        BlocksMovement = 1 << 0,
        BlocksBasicAttacks = 1 << 1,
        BlocksSkillCasts = 1 << 2,
        BlocksDirectTargeting = 1 << 3,
        PreventsDamage = 1 << 4,
        Periodic = 1 << 5,
        StatModifier = 1 << 6,
        ForcesEnemyTarget = 1 << 7,
        HardControl = 1 << 8,
    }

    public readonly struct StatusEffectDefinition
    {
        public StatusEffectDefinition(StatusEffectType effectType, StatusBehaviorFlags behaviorFlags)
        {
            EffectType = effectType;
            BehaviorFlags = behaviorFlags;
        }

        public StatusEffectType EffectType { get; }

        public StatusBehaviorFlags BehaviorFlags { get; }

        public bool BlocksMovement => (BehaviorFlags & StatusBehaviorFlags.BlocksMovement) != 0;

        public bool BlocksBasicAttacks => (BehaviorFlags & StatusBehaviorFlags.BlocksBasicAttacks) != 0;

        public bool BlocksSkillCasts => (BehaviorFlags & StatusBehaviorFlags.BlocksSkillCasts) != 0;

        public bool BlocksDirectTargeting => (BehaviorFlags & StatusBehaviorFlags.BlocksDirectTargeting) != 0;

        public bool PreventsDamage => (BehaviorFlags & StatusBehaviorFlags.PreventsDamage) != 0;

        public bool IsPeriodic => (BehaviorFlags & StatusBehaviorFlags.Periodic) != 0;

        public bool IsStatModifier => (BehaviorFlags & StatusBehaviorFlags.StatModifier) != 0;

        public bool ForcesEnemyTarget => (BehaviorFlags & StatusBehaviorFlags.ForcesEnemyTarget) != 0;

        public bool IsHardControl => (BehaviorFlags & StatusBehaviorFlags.HardControl) != 0;
    }

    public static class StatusEffectCatalog
    {
        private static readonly StatusEffectDefinition None = new StatusEffectDefinition(StatusEffectType.None, StatusBehaviorFlags.None);
        private static readonly StatusEffectDefinition Stun = new StatusEffectDefinition(
            StatusEffectType.Stun,
            StatusBehaviorFlags.BlocksMovement | StatusBehaviorFlags.BlocksBasicAttacks | StatusBehaviorFlags.BlocksSkillCasts | StatusBehaviorFlags.HardControl);
        private static readonly StatusEffectDefinition AttackPowerModifier = new StatusEffectDefinition(StatusEffectType.AttackPowerModifier, StatusBehaviorFlags.StatModifier);
        private static readonly StatusEffectDefinition DefenseModifier = new StatusEffectDefinition(StatusEffectType.DefenseModifier, StatusBehaviorFlags.StatModifier);
        private static readonly StatusEffectDefinition AttackSpeedModifier = new StatusEffectDefinition(StatusEffectType.AttackSpeedModifier, StatusBehaviorFlags.StatModifier);
        private static readonly StatusEffectDefinition MoveSpeedModifier = new StatusEffectDefinition(StatusEffectType.MoveSpeedModifier, StatusBehaviorFlags.StatModifier);
        private static readonly StatusEffectDefinition HealOverTime = new StatusEffectDefinition(StatusEffectType.HealOverTime, StatusBehaviorFlags.Periodic);
        private static readonly StatusEffectDefinition MaxHealthModifier = new StatusEffectDefinition(StatusEffectType.MaxHealthModifier, StatusBehaviorFlags.StatModifier);
        private static readonly StatusEffectDefinition CriticalChanceModifier = new StatusEffectDefinition(StatusEffectType.CriticalChanceModifier, StatusBehaviorFlags.StatModifier);
        private static readonly StatusEffectDefinition CriticalDamageModifier = new StatusEffectDefinition(StatusEffectType.CriticalDamageModifier, StatusBehaviorFlags.StatModifier);
        private static readonly StatusEffectDefinition AttackRangeModifier = new StatusEffectDefinition(StatusEffectType.AttackRangeModifier, StatusBehaviorFlags.StatModifier);
        private static readonly StatusEffectDefinition KnockUp = new StatusEffectDefinition(
            StatusEffectType.KnockUp,
            StatusBehaviorFlags.BlocksMovement | StatusBehaviorFlags.BlocksBasicAttacks | StatusBehaviorFlags.BlocksSkillCasts | StatusBehaviorFlags.HardControl);
        private static readonly StatusEffectDefinition Invulnerable = new StatusEffectDefinition(StatusEffectType.Invulnerable, StatusBehaviorFlags.PreventsDamage);
        private static readonly StatusEffectDefinition Untargetable = new StatusEffectDefinition(StatusEffectType.Untargetable, StatusBehaviorFlags.BlocksDirectTargeting);
        private static readonly StatusEffectDefinition DamageOverTime = new StatusEffectDefinition(StatusEffectType.DamageOverTime, StatusBehaviorFlags.Periodic);
        private static readonly StatusEffectDefinition Shield = new StatusEffectDefinition(StatusEffectType.Shield, StatusBehaviorFlags.None);
        private static readonly StatusEffectDefinition DamageShare = new StatusEffectDefinition(StatusEffectType.DamageShare, StatusBehaviorFlags.None);
        private static readonly StatusEffectDefinition HealTakenModifier = new StatusEffectDefinition(StatusEffectType.HealTakenModifier, StatusBehaviorFlags.StatModifier);
        private static readonly StatusEffectDefinition Taunt = new StatusEffectDefinition(
            StatusEffectType.Taunt,
            StatusBehaviorFlags.BlocksSkillCasts | StatusBehaviorFlags.ForcesEnemyTarget);

        public static StatusEffectDefinition Get(StatusEffectType effectType)
        {
            return effectType switch
            {
                StatusEffectType.Stun => Stun,
                StatusEffectType.AttackPowerModifier => AttackPowerModifier,
                StatusEffectType.DefenseModifier => DefenseModifier,
                StatusEffectType.AttackSpeedModifier => AttackSpeedModifier,
                StatusEffectType.MoveSpeedModifier => MoveSpeedModifier,
                StatusEffectType.HealOverTime => HealOverTime,
                StatusEffectType.MaxHealthModifier => MaxHealthModifier,
                StatusEffectType.CriticalChanceModifier => CriticalChanceModifier,
                StatusEffectType.CriticalDamageModifier => CriticalDamageModifier,
                StatusEffectType.AttackRangeModifier => AttackRangeModifier,
                StatusEffectType.KnockUp => KnockUp,
                StatusEffectType.Invulnerable => Invulnerable,
                StatusEffectType.Untargetable => Untargetable,
                StatusEffectType.DamageOverTime => DamageOverTime,
                StatusEffectType.Shield => Shield,
                StatusEffectType.DamageShare => DamageShare,
                StatusEffectType.HealTakenModifier => HealTakenModifier,
                StatusEffectType.Taunt => Taunt,
                _ => None,
            };
        }
    }
}
