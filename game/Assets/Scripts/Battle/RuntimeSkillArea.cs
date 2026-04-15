using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public class RuntimeSkillArea
    {
        private static int nextAreaId;

        public RuntimeSkillArea(RuntimeHero caster, SkillData skill, SkillEffectData effect, Vector3 initialCenter)
        {
            Caster = caster;
            Skill = skill;
            Effect = effect;
            InitialCenter = initialCenter;
            TotalDurationSeconds = Mathf.Max(0f, effect != null ? effect.durationSeconds : 0f);
            RemainingDurationSeconds = TotalDurationSeconds;
            TickIntervalSeconds = Mathf.Max(0.1f, effect != null ? effect.tickIntervalSeconds : 1f);
            TimeUntilNextTickSeconds = 0f;
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

        public bool Tick(float deltaTime)
        {
            if (IsExpired)
            {
                return false;
            }

            RemainingDurationSeconds = Mathf.Max(0f, RemainingDurationSeconds - deltaTime);

            if (Effect != null && Effect.followCaster && (Caster == null || Caster.IsDead))
            {
                RemainingDurationSeconds = 0f;
                return false;
            }

            TimeUntilNextTickSeconds -= deltaTime;
            if (TimeUntilNextTickSeconds > 0f)
            {
                return false;
            }

            TimeUntilNextTickSeconds = TickIntervalSeconds;
            return true;
        }
    }
}
