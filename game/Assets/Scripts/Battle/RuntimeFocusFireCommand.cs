using System.Collections.Generic;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public sealed class RuntimeFocusFireCommand
    {
        private readonly List<StatusEffectData> statusEffects = new List<StatusEffectData>();

        public RuntimeFocusFireCommand(
            RuntimeHero source,
            SkillData skill,
            TeamSide sourceSide,
            Vector3 originPosition,
            float selectionRange,
            float durationSeconds,
            IReadOnlyList<StatusEffectData> statusTemplates)
        {
            Source = source;
            Skill = skill;
            SourceSide = sourceSide;
            OriginPosition = originPosition;
            SelectionRange = Mathf.Max(0f, selectionRange);
            RemainingDurationSeconds = Mathf.Max(0f, durationSeconds);

            if (statusTemplates == null)
            {
                return;
            }

            for (var i = 0; i < statusTemplates.Count; i++)
            {
                var status = statusTemplates[i];
                if (status != null && status.effectType != StatusEffectType.None)
                {
                    statusEffects.Add(status);
                }
            }
        }

        public RuntimeHero Source { get; }

        public SkillData Skill { get; }

        public TeamSide SourceSide { get; }

        public Vector3 OriginPosition { get; }

        public float SelectionRange { get; }

        public float RemainingDurationSeconds { get; private set; }

        public RuntimeHero CurrentTarget { get; private set; }

        public IReadOnlyList<StatusEffectData> StatusEffects => statusEffects;

        public bool IsExpired => RemainingDurationSeconds <= Mathf.Epsilon;

        public void Tick(float deltaTime)
        {
            RemainingDurationSeconds = Mathf.Max(0f, RemainingDurationSeconds - Mathf.Max(0f, deltaTime));
        }

        public void SetCurrentTarget(RuntimeHero target)
        {
            CurrentTarget = target;
        }
    }
}
