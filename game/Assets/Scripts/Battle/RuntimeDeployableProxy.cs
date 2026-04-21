using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public sealed class RuntimeDeployableProxy
    {
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
        }

        public string ProxyId { get; }

        public RuntimeHero Owner { get; }

        public SkillData SourceSkill { get; }

        public SkillEffectData SourceEffect { get; }

        public Vector3 CurrentPosition { get; }

        public int SpawnSequence { get; }

        public float TotalDurationSeconds { get; }

        public float RemainingDurationSeconds { get; private set; }

        public bool IsExpired => RemainingDurationSeconds <= 0f;

        public float StrikeRadius => Mathf.Max(0f, SourceEffect != null ? SourceEffect.deployableProxyStrikeRadius : 0f);

        public float StrikePowerMultiplier => Mathf.Max(0f, SourceEffect != null ? SourceEffect.powerMultiplier : 0f);

        public DeployableProxyTriggerMode TriggerMode => SourceEffect != null
            ? SourceEffect.deployableProxyTriggerMode
            : DeployableProxyTriggerMode.None;

        public void Tick(float deltaTime)
        {
            RemainingDurationSeconds = Mathf.Max(0f, RemainingDurationSeconds - Mathf.Max(0f, deltaTime));
        }
    }
}
