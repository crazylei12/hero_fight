using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public sealed class RuntimeDeployableProxy
    {
        private bool isExpired;

        public RuntimeDeployableProxy(
            RuntimeHero owner,
            SkillData sourceSkill,
            SkillEffectData sourceEffect,
            Vector3 currentPosition,
            int spawnSequence)
        {
            Owner = owner;
            SourceSkill = sourceSkill;
            SourceEffect = sourceEffect;
            CurrentPosition = Stage01ArenaSpec.ClampPosition(currentPosition);
            CurrentPosition = new Vector3(CurrentPosition.x, 0f, CurrentPosition.z);
            SpawnSequence = spawnSequence;
            ProxyId = $"deployable_proxy_{spawnSequence:D4}";
            TotalDurationSeconds = Mathf.Max(0f, sourceEffect != null ? sourceEffect.durationSeconds : 0f);
            RemainingDurationSeconds = TotalDurationSeconds;
            CurrentBasicAttackVariantIndex = GetClampedStartingVariantIndex();
            RemainingAttackCooldownSeconds = GetInitialPeriodicTriggerCooldownSeconds();
        }

        public string ProxyId { get; }

        public RuntimeHero Owner { get; }

        public SkillData SourceSkill { get; }

        public SkillEffectData SourceEffect { get; }

        public Vector3 CurrentPosition { get; }

        public int SpawnSequence { get; }

        public float TotalDurationSeconds { get; }

        public float RemainingDurationSeconds { get; private set; }

        public float RemainingAttackCooldownSeconds { get; private set; }

        public int CurrentBasicAttackVariantIndex { get; private set; }

        public bool PersistsUntilTriggered => SourceEffect != null && SourceEffect.deployableProxyPersistUntilTriggered;

        public bool HasFiniteDuration => !PersistsUntilTriggered;

        public bool IsExpired => isExpired || (HasFiniteDuration && RemainingDurationSeconds <= 0f);

        public float StrikeRadius => Mathf.Max(0f, SourceEffect != null ? SourceEffect.deployableProxyStrikeRadius : 0f);

        public float TriggerRadius => Mathf.Max(
            0f,
            SourceEffect != null && SourceEffect.deployableProxyTriggerRadius > Mathf.Epsilon
                ? SourceEffect.deployableProxyTriggerRadius
                : StrikeRadius);

        public float EffectRadius => Mathf.Max(
            0f,
            SourceEffect != null && SourceEffect.deployableProxyEffectRadius > Mathf.Epsilon
                ? SourceEffect.deployableProxyEffectRadius
                : SourceEffect != null && SourceEffect.radiusOverride > Mathf.Epsilon
                    ? SourceEffect.radiusOverride
                    : StrikeRadius);

        public float StrikePowerMultiplier => Mathf.Max(0f, SourceEffect != null ? SourceEffect.powerMultiplier : 0f);

        public float PowerMultiplierScale => Mathf.Max(
            0f,
            SourceEffect != null && SourceEffect.deployableProxyPowerMultiplierScale > Mathf.Epsilon
                ? SourceEffect.deployableProxyPowerMultiplierScale
                : 1f);

        public float AttackIntervalSeconds => Mathf.Max(0f, SourceEffect != null ? SourceEffect.deployableProxyAttackIntervalSeconds : 0f);

        public float AttackRange => Mathf.Max(
            0f,
            SourceEffect != null && SourceEffect.deployableProxyAttackRange > Mathf.Epsilon
                ? SourceEffect.deployableProxyAttackRange
                : Owner != null
                    ? Owner.AttackRange
                    : 0f);

        public float ProjectileSpeed => Mathf.Max(
            0f,
            SourceEffect != null && SourceEffect.deployableProxyProjectileSpeedOverride > Mathf.Epsilon
                ? SourceEffect.deployableProxyProjectileSpeedOverride
                : Owner?.Definition?.basicAttack != null
                    ? Owner.Definition.basicAttack.projectileSpeed
                    : 0f);

        public DeployableProxyTriggerMode TriggerMode => SourceEffect != null
            ? SourceEffect.deployableProxyTriggerMode
            : DeployableProxyTriggerMode.None;

        public void Tick(float deltaTime)
        {
            if (HasFiniteDuration)
            {
                RemainingDurationSeconds = Mathf.Max(0f, RemainingDurationSeconds - Mathf.Max(0f, deltaTime));
            }

            RemainingAttackCooldownSeconds = Mathf.Max(0f, RemainingAttackCooldownSeconds - Mathf.Max(0f, deltaTime));
        }

        public void ExpireImmediately()
        {
            isExpired = true;
            RemainingDurationSeconds = 0f;
            RemainingAttackCooldownSeconds = 0f;
        }

        public bool TryConsumeReadyAttack()
        {
            return TryConsumeReadyPeriodicTrigger(DeployableProxyTriggerMode.PeriodicBasicAttackSequence);
        }

        public bool TryConsumeReadyEffectPulse()
        {
            return TryConsumeReadyPeriodicTrigger(DeployableProxyTriggerMode.PeriodicEffectPulse);
        }

        public int GetClampedBasicAttackVariantIndex()
        {
            var variants = Owner?.Definition?.basicAttack?.variants;
            if (variants == null || variants.Count <= 0)
            {
                return 0;
            }

            return Mathf.Clamp(CurrentBasicAttackVariantIndex, 0, variants.Count - 1);
        }

        public void AdvanceBasicAttackVariantIndex()
        {
            var variants = Owner?.Definition?.basicAttack?.variants;
            if (variants == null || variants.Count <= 0)
            {
                CurrentBasicAttackVariantIndex = 0;
                return;
            }

            CurrentBasicAttackVariantIndex = (GetClampedBasicAttackVariantIndex() + 1) % variants.Count;
        }

        private int GetClampedStartingVariantIndex()
        {
            var variants = Owner?.Definition?.basicAttack?.variants;
            if (variants == null || variants.Count <= 0)
            {
                return 0;
            }

            var configuredIndex = SourceEffect != null ? SourceEffect.deployableProxyStartingVariantIndex : 0;
            return Mathf.Clamp(configuredIndex, 0, variants.Count - 1);
        }

        private bool TryConsumeReadyPeriodicTrigger(DeployableProxyTriggerMode requiredMode)
        {
            if (TriggerMode != requiredMode
                || AttackIntervalSeconds <= Mathf.Epsilon
                || RemainingAttackCooldownSeconds > Mathf.Epsilon)
            {
                return false;
            }

            RemainingAttackCooldownSeconds = AttackIntervalSeconds;
            return true;
        }

        private float GetInitialPeriodicTriggerCooldownSeconds()
        {
            return TriggerMode == DeployableProxyTriggerMode.PeriodicBasicAttackSequence
                || TriggerMode == DeployableProxyTriggerMode.PeriodicEffectPulse
                ? AttackIntervalSeconds
                : 0f;
        }
    }
}
