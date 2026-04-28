using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public class RuntimeSkillArea
    {
        private static int nextAreaId;
        private int pendingPulseCount;

        public RuntimeSkillArea(RuntimeHero caster, SkillData skill, SkillEffectData effect, Vector3 initialCenter)
        {
            Caster = caster;
            Skill = skill;
            Effect = effect;
            InitialCenter = initialCenter;
            TotalDurationSeconds = Mathf.Max(0f, effect != null ? effect.durationSeconds : 0f);
            RemainingDurationSeconds = TotalDurationSeconds;
            TickIntervalSeconds = Mathf.Max(0.1f, effect != null ? effect.tickIntervalSeconds : 1f);
            TimeUntilNextTickSeconds = TickIntervalSeconds;
            AreaId = $"skill_area_{nextAreaId++:D4}";
        }

        public string AreaId { get; }

        public RuntimeHero Caster { get; }

        public SkillData Skill { get; }

        public SkillEffectData Effect { get; }

        public Vector3 InitialCenter { get; }

        public float TotalDurationSeconds { get; }

        public float RemainingDurationSeconds { get; private set; }

        public float TickIntervalSeconds { get; }

        public float TimeUntilNextTickSeconds { get; private set; }

        public bool IsExpired => RemainingDurationSeconds <= 0f;

        public float Radius
        {
            get
            {
                if (Effect != null && Effect.radiusOverride > 0f)
                {
                    return Effect.radiusOverride;
                }

                return Skill != null ? Skill.areaRadius : 0f;
            }
        }

        public Vector3 CurrentCenter
        {
            get
            {
                if (Effect != null && Effect.followCaster && Caster != null && !Caster.IsDead)
                {
                    return Caster.CurrentPosition;
                }

                return InitialCenter;
            }
        }

        public GameObject AreaVfxPrefab
        {
            get
            {
                if (Effect != null && Effect.areaVfxPrefabOverride != null)
                {
                    return Effect.areaVfxPrefabOverride;
                }

                return Skill != null ? Skill.persistentAreaVfxPrefab : null;
            }
        }

        public float AreaVfxScaleMultiplier
        {
            get
            {
                if (Effect != null && Effect.areaVfxScaleMultiplierOverride > Mathf.Epsilon)
                {
                    return Mathf.Max(0.1f, Effect.areaVfxScaleMultiplierOverride);
                }

                return Skill != null ? Mathf.Max(0.1f, Skill.persistentAreaVfxScaleMultiplier) : 1f;
            }
        }

        public Vector3 AreaVfxEulerAngles
        {
            get
            {
                if (Effect != null && Effect.areaVfxPrefabOverride != null)
                {
                    return Effect.areaVfxEulerAnglesOverride;
                }

                return Skill != null ? Skill.persistentAreaVfxEulerAngles : Vector3.zero;
            }
        }

        public void Tick(float deltaTime)
        {
            if (IsExpired)
            {
                return;
            }

            var elapsedTime = Mathf.Min(deltaTime, RemainingDurationSeconds);
            RemainingDurationSeconds = Mathf.Max(0f, RemainingDurationSeconds - deltaTime);

            if (Effect != null && Effect.followCaster && (Caster == null || Caster.IsDead))
            {
                RemainingDurationSeconds = 0f;
                return;
            }

            if (elapsedTime <= 0f)
            {
                return;
            }

            TimeUntilNextTickSeconds -= elapsedTime;
            while (TimeUntilNextTickSeconds <= 0f && TickIntervalSeconds > 0f)
            {
                pendingPulseCount++;
                TimeUntilNextTickSeconds += TickIntervalSeconds;
            }
        }

        public int ConsumePendingPulseCount()
        {
            var result = pendingPulseCount;
            pendingPulseCount = 0;
            return result;
        }
    }
}
