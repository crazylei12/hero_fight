using System.Collections.Generic;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public readonly struct SkillEffectResolutionState
    {
        public SkillEffectResolutionState(Vector3 dashStartPosition, Vector3 dashDestination, float dashDurationSeconds, bool hasDashPath)
        {
            DashStartPosition = dashStartPosition;
            DashDestination = dashDestination;
            DashDurationSeconds = Mathf.Max(0f, dashDurationSeconds);
            HasDashPath = hasDashPath;
        }

        public Vector3 DashStartPosition { get; }

        public Vector3 DashDestination { get; }

        public float DashDurationSeconds { get; }

        public bool HasDashPath { get; }
    }
    public sealed class RuntimeDelayedSkillEffect
    {
        private readonly List<RuntimeHero> affectedTargets = new List<RuntimeHero>();
        private bool skipFirstTick = true;

        public RuntimeDelayedSkillEffect(
            RuntimeHero caster,
            SkillData skill,
            RuntimeHero primaryTarget,
            IReadOnlyList<RuntimeHero> initialAffectedTargets,
            SkillEffectData effect,
            SkillEffectResolutionState resolutionState,
            float delaySeconds)
        {
            Caster = caster;
            Skill = skill;
            PrimaryTarget = primaryTarget;
            Effect = effect;
            ResolutionState = resolutionState;
            RemainingDelaySeconds = Mathf.Max(0f, delaySeconds);
            if (initialAffectedTargets == null)
            {
                return;
            }

            for (var i = 0; i < initialAffectedTargets.Count; i++)
            {
                var target = initialAffectedTargets[i];
                if (target != null)
                {
                    affectedTargets.Add(target);
                }
            }
        }

        public RuntimeHero Caster { get; }

        public SkillData Skill { get; }

        public RuntimeHero PrimaryTarget { get; }

        public SkillEffectData Effect { get; }

        public SkillEffectResolutionState ResolutionState { get; }

        public IReadOnlyList<RuntimeHero> AffectedTargets => affectedTargets;

        public float RemainingDelaySeconds { get; private set; }

        public bool IsReady => !skipFirstTick && RemainingDelaySeconds <= 0f;

        public void Tick(float deltaTime)
        {
            if (skipFirstTick)
            {
                skipFirstTick = false;
                return;
            }

            RemainingDelaySeconds = Mathf.Max(0f, RemainingDelaySeconds - Mathf.Max(0f, deltaTime));
        }
    }

    public class BattleContext
    {
        private float lastBlueUltimateCastTimeSeconds = float.NegativeInfinity;
        private float lastRedUltimateCastTimeSeconds = float.NegativeInfinity;
        private int nextCloneSequence;

        public BattleContext(BattleInputConfig input, BattleClock clock, BattleScoreSystem scoreSystem, BattleRandomService randomService, BattleEventBus eventBus, List<RuntimeHero> heroes)
        {
            Input = input;
            Clock = clock;
            ScoreSystem = scoreSystem;
            RandomService = randomService;
            EventBus = eventBus;
            Heroes = heroes;
            Projectiles = new List<RuntimeBasicAttackProjectile>();
            SkillAreas = new List<RuntimeSkillArea>();
            RadialSweeps = new List<RuntimeRadialSweep>();
            ReturningPathStrikes = new List<RuntimeReturningPathStrike>();
            ChanneledPathSkills = new List<RuntimeChanneledPathSkill>();
            DeployableProxies = new List<RuntimeDeployableProxy>();
            DelayedSkillEffects = new List<RuntimeDelayedSkillEffect>();
            ReactiveGuards = new List<RuntimeReactiveGuard>();
            FocusFireCommands = new List<RuntimeFocusFireCommand>();
            KnockUpFollowUpTriggers = new List<RuntimeKnockUpFollowUpTrigger>();
        }

        public BattleInputConfig Input { get; }

        public BattleClock Clock { get; }

        public BattleScoreSystem ScoreSystem { get; }

        public BattleRandomService RandomService { get; }

        public BattleEventBus EventBus { get; }

        public List<RuntimeHero> Heroes { get; }

        public List<RuntimeBasicAttackProjectile> Projectiles { get; }

        public List<RuntimeSkillArea> SkillAreas { get; }

        public List<RuntimeRadialSweep> RadialSweeps { get; }

        public List<RuntimeReturningPathStrike> ReturningPathStrikes { get; }

        public List<RuntimeChanneledPathSkill> ChanneledPathSkills { get; }

        public List<RuntimeDeployableProxy> DeployableProxies { get; }

        public List<RuntimeDelayedSkillEffect> DelayedSkillEffects { get; }

        public List<RuntimeReactiveGuard> ReactiveGuards { get; }

        public List<RuntimeFocusFireCommand> FocusFireCommands { get; }

        public List<RuntimeKnockUpFollowUpTrigger> KnockUpFollowUpTriggers { get; }

        public int NextCloneSequence()
        {
            nextCloneSequence++;
            return nextCloneSequence;
        }

        public BattleTeamLoadout GetTeamLoadout(TeamSide side)
        {
            return side switch
            {
                TeamSide.Blue => Input?.blueTeam,
                TeamSide.Red => Input?.redTeam,
                _ => null,
            };
        }

        public BattleUltimateTimingStrategy GetUltimateTimingStrategy(TeamSide side)
        {
            var loadout = GetTeamLoadout(side);
            return loadout != null
                ? loadout.ultimateTimingStrategy
                : BattleUltimateTimingStrategy.Standard;
        }

        public BattleUltimateComboStrategy GetUltimateComboStrategy(TeamSide side)
        {
            var loadout = GetTeamLoadout(side);
            return loadout != null
                ? loadout.ultimateComboStrategy
                : BattleUltimateComboStrategy.Standard;
        }

        public void RecordUltimateCast(TeamSide side)
        {
            var castTimeSeconds = Clock != null ? Mathf.Max(0f, Clock.ElapsedTimeSeconds) : 0f;
            switch (side)
            {
                case TeamSide.Blue:
                    lastBlueUltimateCastTimeSeconds = castTimeSeconds;
                    break;
                case TeamSide.Red:
                    lastRedUltimateCastTimeSeconds = castTimeSeconds;
                    break;
            }
        }

        public float GetLastUltimateCastTimeSeconds(TeamSide side)
        {
            return side switch
            {
                TeamSide.Blue => lastBlueUltimateCastTimeSeconds,
                TeamSide.Red => lastRedUltimateCastTimeSeconds,
                _ => float.NegativeInfinity,
            };
        }
    }
}
