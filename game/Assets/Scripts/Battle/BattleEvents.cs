using System;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public interface IBattleEvent
    {
    }

    public enum DamageSourceKind
    {
        Unknown = 0,
        BasicAttack = 1,
        Skill = 2,
        SkillAreaPulse = 3,
        StatusEffect = 4,
        DamageShare = 5,
    }

    public sealed class BattleStartedEvent : IBattleEvent
    {
        public BattleStartedEvent(BattleInputConfig input)
        {
            Input = input;
        }

        public BattleInputConfig Input { get; }
    }

    public sealed class ScoreChangedEvent : IBattleEvent
    {
        public ScoreChangedEvent(int blueKills, int redKills)
        {
            BlueKills = blueKills;
            RedKills = redKills;
        }

        public int BlueKills { get; }

        public int RedKills { get; }
    }

    public sealed class UnitSpawnedEvent : IBattleEvent
    {
        public UnitSpawnedEvent(RuntimeHero hero)
        {
            Hero = hero;
        }

        public RuntimeHero Hero { get; }
    }

    public sealed class TargetChangedEvent : IBattleEvent
    {
        public TargetChangedEvent(RuntimeHero hero, RuntimeHero target)
        {
            Hero = hero;
            Target = target;
        }

        public RuntimeHero Hero { get; }

        public RuntimeHero Target { get; }
    }

    public sealed class AttackPerformedEvent : IBattleEvent
    {
        public AttackPerformedEvent(RuntimeHero attacker, RuntimeHero target)
        {
            Attacker = attacker;
            Target = target;
        }

        public RuntimeHero Attacker { get; }

        public RuntimeHero Target { get; }
    }

    public sealed class BasicAttackProjectileLaunchedEvent : IBattleEvent
    {
        public BasicAttackProjectileLaunchedEvent(RuntimeBasicAttackProjectile projectile)
        {
            Projectile = projectile;
        }

        public RuntimeBasicAttackProjectile Projectile { get; }
    }

    public sealed class SkillCastEvent : IBattleEvent
    {
        public SkillCastEvent(RuntimeHero caster, SkillData skill, RuntimeHero primaryTarget, int affectedTargetCount)
        {
            Caster = caster;
            Skill = skill;
            PrimaryTarget = primaryTarget;
            AffectedTargetCount = affectedTargetCount;
        }

        public RuntimeHero Caster { get; }

        public SkillData Skill { get; }

        public RuntimeHero PrimaryTarget { get; }

        public int AffectedTargetCount { get; }
    }

    public sealed class SkillAreaCreatedEvent : IBattleEvent
    {
        public SkillAreaCreatedEvent(RuntimeHero caster, SkillData skill, RuntimeSkillArea area)
        {
            Caster = caster;
            Skill = skill;
            Area = area;
        }

        public RuntimeHero Caster { get; }

        public SkillData Skill { get; }

        public RuntimeSkillArea Area { get; }
    }

    public sealed class SkillAreaPulseEvent : IBattleEvent
    {
        public SkillAreaPulseEvent(RuntimeHero caster, SkillData skill, RuntimeSkillArea area, int affectedTargetCount)
        {
            Caster = caster;
            Skill = skill;
            Area = area;
            AffectedTargetCount = affectedTargetCount;
        }

        public RuntimeHero Caster { get; }

        public SkillData Skill { get; }

        public RuntimeSkillArea Area { get; }

        public int AffectedTargetCount { get; }
    }

    public sealed class DamageAppliedEvent : IBattleEvent
    {
        public DamageAppliedEvent(
            RuntimeHero attacker,
            RuntimeHero target,
            float damageAmount,
            DamageSourceKind sourceKind = DamageSourceKind.Unknown,
            SkillData sourceSkill = null,
            float remainingHealth = 0f)
        {
            Attacker = attacker;
            Target = target;
            DamageAmount = damageAmount;
            SourceKind = sourceKind;
            SourceSkill = sourceSkill;
            RemainingHealth = remainingHealth;
        }

        public RuntimeHero Attacker { get; }

        public RuntimeHero Target { get; }

        public float DamageAmount { get; }

        public DamageSourceKind SourceKind { get; }

        public SkillData SourceSkill { get; }

        public float RemainingHealth { get; }
    }

    public sealed class HealAppliedEvent : IBattleEvent
    {
        public HealAppliedEvent(
            RuntimeHero caster,
            RuntimeHero target,
            float healAmount,
            SkillData sourceSkill = null,
            float resultingHealth = 0f)
        {
            Caster = caster;
            Target = target;
            HealAmount = healAmount;
            SourceSkill = sourceSkill;
            ResultingHealth = resultingHealth;
        }

        public RuntimeHero Caster { get; }

        public RuntimeHero Target { get; }

        public float HealAmount { get; }

        public SkillData SourceSkill { get; }

        public float ResultingHealth { get; }
    }

    public sealed class StatusAppliedEvent : IBattleEvent
    {
        public StatusAppliedEvent(
            RuntimeHero source,
            RuntimeHero target,
            StatusEffectType effectType,
            float durationSeconds,
            float magnitude,
            SkillData sourceSkill = null,
            RuntimeHero appliedBy = null)
        {
            Source = source;
            Target = target;
            EffectType = effectType;
            DurationSeconds = durationSeconds;
            Magnitude = magnitude;
            SourceSkill = sourceSkill;
            AppliedBy = appliedBy ?? source;
        }

        public RuntimeHero Source { get; }

        public RuntimeHero AppliedBy { get; }

        public RuntimeHero Target { get; }

        public StatusEffectType EffectType { get; }

        public float DurationSeconds { get; }

        public float Magnitude { get; }

        public SkillData SourceSkill { get; }
    }

    public sealed class StatusRemovedEvent : IBattleEvent
    {
        public StatusRemovedEvent(
            RuntimeHero source,
            RuntimeHero target,
            StatusEffectType effectType,
            SkillData sourceSkill = null,
            RuntimeHero appliedBy = null)
        {
            Source = source;
            Target = target;
            EffectType = effectType;
            SourceSkill = sourceSkill;
            AppliedBy = appliedBy ?? source;
        }

        public RuntimeHero Source { get; }

        public RuntimeHero AppliedBy { get; }

        public RuntimeHero Target { get; }

        public StatusEffectType EffectType { get; }

        public SkillData SourceSkill { get; }
    }

    public sealed class ForcedMovementAppliedEvent : IBattleEvent
    {
        public ForcedMovementAppliedEvent(
            RuntimeHero source,
            RuntimeHero target,
            Vector3 startPosition,
            Vector3 destination,
            float durationSeconds,
            float peakHeight,
            SkillData sourceSkill = null)
        {
            Source = source;
            Target = target;
            StartPosition = startPosition;
            Destination = destination;
            DurationSeconds = durationSeconds;
            PeakHeight = peakHeight;
            SourceSkill = sourceSkill;
        }

        public RuntimeHero Source { get; }

        public RuntimeHero Target { get; }

        public Vector3 StartPosition { get; }

        public Vector3 Destination { get; }

        public float DurationSeconds { get; }

        public float PeakHeight { get; }

        public SkillData SourceSkill { get; }
    }

    public sealed class UnitDiedEvent : IBattleEvent
    {
        public UnitDiedEvent(RuntimeHero victim, RuntimeHero killer)
        {
            Victim = victim;
            Killer = killer;
        }

        public RuntimeHero Victim { get; }

        public RuntimeHero Killer { get; }
    }

    public sealed class UnitRevivedEvent : IBattleEvent
    {
        public UnitRevivedEvent(RuntimeHero hero)
        {
            Hero = hero;
        }

        public RuntimeHero Hero { get; }
    }

    public sealed class OvertimeStartedEvent : IBattleEvent
    {
    }

    public sealed class BattleEndedEvent : IBattleEvent
    {
        public BattleEndedEvent(BattleResultData result)
        {
            Result = result;
        }

        public BattleResultData Result { get; }
    }

    public class BattleEventBus
    {
        public event Action<IBattleEvent> Published;

        public void Publish(IBattleEvent battleEvent)
        {
            Published?.Invoke(battleEvent);
        }
    }
}
