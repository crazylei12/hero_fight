using System;
using System.Collections.Generic;
using System.Text;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public class BattleLogSession
    {
        private readonly List<string> fullLogEntries = new List<string>();
        private readonly List<string> blueWarriorSpotlightEntries = new List<string>();
        private readonly List<string> ultimateDecisionEntries = new List<string>();

        private string trackedBlueWarriorHeroId;
        private string trackedBlueWarriorDisplayName;
        private int trackedBlueWarriorSlotIndex = -1;
        private int blueWarriorActiveSkillCastCount;
        private int blueWarriorUltimateCastCount;
        private int blueWarriorKnockUpAppliedCount;
        private Func<float> timeProvider;

        public BattleLogSession()
        {
            CurrentBattleLogId = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        public string CurrentBattleLogId { get; private set; }

        public bool HasEvents => fullLogEntries.Count > 0;

        public void SetTimeProvider(Func<float> provider)
        {
            timeProvider = provider;
        }

        public void HandleBattleEvent(IBattleEvent battleEvent)
        {
            switch (battleEvent)
            {
                case BattleStartedEvent started:
                    fullLogEntries.Clear();
                    ultimateDecisionEntries.Clear();
                    CurrentBattleLogId = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    ResetBlueWarriorSpotlight();
                    CaptureTrackedBlueWarrior(started.Input);
                    AddLog($"Battle started. Session {CurrentBattleLogId}.");
                    AddLog(
                        $"Team ultimate strategy: Blue timing={started.Input?.blueTeam?.ultimateTimingStrategy} combo={started.Input?.blueTeam?.ultimateComboStrategy}; " +
                        $"Red timing={started.Input?.redTeam?.ultimateTimingStrategy} combo={started.Input?.redTeam?.ultimateComboStrategy}.");
                    break;
                case UnitSpawnedEvent spawned:
                    AddLog($"{FormatHeroLabel(spawned.Hero)} spawned for {spawned.Hero.Side}.");
                    break;
                case TargetChangedEvent targetChanged:
                    AddLog($"{FormatHeroLabel(targetChanged.Hero)} targets {FormatHeroLabel(targetChanged.Target)}.");
                    break;
                case AttackPerformedEvent attackPerformed:
                    AddLog($"{FormatHeroLabel(attackPerformed.Attacker)} started basic attack [{FormatBasicAttackVariantLabel(attackPerformed.VariantKey)}] on {FormatHeroLabel(attackPerformed.Target)}.");
                    break;
                case BasicAttackProjectileLaunchedEvent projectileLaunched:
                    AddLog(FormatBasicAttackProjectileLog(projectileLaunched.Projectile));
                    break;
                case SkillCastEvent skillCast:
                    AddLog($"{FormatHeroLabel(skillCast.Caster)} started casting {skillCast.Skill.displayName} on {FormatHeroLabel(skillCast.PrimaryTarget, "area")} ({skillCast.AffectedTargetCount} target(s)).");
                    TryAddBlueWarriorSkillCast(skillCast);
                    break;
                case UltimateDecisionEvaluatedEvent ultimateDecision:
                    var decisionLog = FormatUltimateDecisionLog(ultimateDecision);
                    AddLog(decisionLog);
                    AddUltimateDecisionLog(decisionLog);
                    break;
                case SkillAreaCreatedEvent areaCreated:
                    var areaDuration = areaCreated.Area?.Effect != null ? areaCreated.Area.Effect.durationSeconds : 0f;
                    AddLog($"{FormatHeroLabel(areaCreated.Caster)} created {areaCreated.Skill.displayName} area {areaCreated.Area?.AreaId ?? "unknown"} for {areaDuration:0.0}s.");
                    break;
                case SkillAreaPulseEvent areaPulse:
                    var center = areaPulse.Area != null ? areaPulse.Area.CurrentCenter : Vector3.zero;
                    AddLog($"{FormatHeroLabel(areaPulse.Caster)}'s {areaPulse.Skill.displayName} pulse affected {areaPulse.AffectedTargetCount} target(s) at ({center.x:0.0}, {center.z:0.0}), area {areaPulse.Area?.AreaId ?? "unknown"}.");
                    break;
                case RadialSweepResolvedEvent radialSweepResolved:
                    AddLog(FormatRadialSweepResolvedLog(radialSweepResolved));
                    break;
                case DamageAppliedEvent damageApplied:
                    AddLog(FormatDamageLog(damageApplied));
                    break;
                case HealAppliedEvent healApplied:
                    AddLog(FormatHealLog(healApplied));
                    break;
                case StatusAppliedEvent statusApplied:
                    AddLog(FormatStatusLog(statusApplied));
                    TryAddBlueWarriorKnockUp(statusApplied);
                    break;
                case StatusRemovedEvent statusRemoved:
                    AddLog(FormatStatusRemovedLog(statusRemoved));
                    break;
                case ForcedMovementAppliedEvent forcedMovementApplied:
                    AddLog(FormatForcedMovementLog(forcedMovementApplied));
                    break;
                case PassiveSkillValueChangedEvent passiveSkillChanged:
                    AddLog(FormatPassiveSkillValueChangedLog(passiveSkillChanged));
                    break;
                case PassiveStackChangedEvent passiveStackChanged:
                    AddLog(FormatPassiveStackChangedLog(passiveStackChanged));
                    break;
                case PositiveEffectRejectedEvent positiveEffectRejected:
                    AddLog(FormatPositiveEffectRejectedLog(positiveEffectRejected));
                    break;
                case SkillTemporaryOverrideChangedEvent temporaryOverrideChanged:
                    AddLog(FormatSkillTemporaryOverrideChangedLog(temporaryOverrideChanged));
                    break;
                case ReactiveGuardTriggeredEvent reactiveGuardTriggered:
                    AddLog(FormatReactiveGuardLog(reactiveGuardTriggered));
                    break;
                case DeployableProxySpawnedEvent deployableProxySpawned:
                    AddLog(FormatDeployableProxySpawnedLog(deployableProxySpawned));
                    break;
                case DeployableProxyRemovedEvent deployableProxyRemoved:
                    AddLog(FormatDeployableProxyRemovedLog(deployableProxyRemoved));
                    break;
                case UnitDiedEvent died:
                    AddLog($"{FormatHeroLabel(died.Victim)} was killed by {FormatHeroLabel(died.Killer)}.");
                    break;
                case UnitRevivedEvent revived:
                    AddLog($"{FormatHeroLabel(revived.Hero)} revived.");
                    break;
                case ScoreChangedEvent scoreChanged:
                    AddLog($"Score update: Blue {scoreChanged.BlueKills} - {scoreChanged.RedKills} Red.");
                    break;
                case OvertimeStartedEvent _:
                    AddLog("Overtime started. Next kill wins.");
                    break;
                case BattleEndedEvent ended:
                    AddLog($"Battle ended. Winner: {ended.Result.winner}, reason: {ended.Result.endReason}.");
                    break;
            }
        }

        public string BuildExportText()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Battle Debug Log Export");
            builder.AppendLine($"Session: {CurrentBattleLogId}");
            builder.AppendLine($"Exported At: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine();
            AppendBlueWarriorSpotlight(builder);
            builder.AppendLine();
            AppendUltimateDecisionTrace(builder);
            builder.AppendLine();
            builder.AppendLine("Full Event Log");
            builder.AppendLine();

            for (var i = 0; i < fullLogEntries.Count; i++)
            {
                builder.AppendLine(fullLogEntries[i]);
            }

            return builder.ToString();
        }

        private void AddLog(string message)
        {
            fullLogEntries.Add($"[{GetCurrentTimeSeconds():0.0}] {message}");
        }

        private void AddBlueWarriorSpotlightLog(string message)
        {
            blueWarriorSpotlightEntries.Add($"[{GetCurrentTimeSeconds():0.0}] {message}");
        }

        private void AddUltimateDecisionLog(string message)
        {
            ultimateDecisionEntries.Add($"[{GetCurrentTimeSeconds():0.0}] {message}");
        }

        private float GetCurrentTimeSeconds()
        {
            return Mathf.Max(0f, timeProvider != null ? timeProvider() : Time.timeSinceLevelLoad);
        }

        private static string FormatDamageLog(DamageAppliedEvent damageApplied)
        {
            var attackerName = FormatHeroLabel(damageApplied.Attacker, "Unknown");
            var skillName = damageApplied.SourceSkill != null ? damageApplied.SourceSkill.displayName : "Basic Attack";
            var sourceKindLabel = damageApplied.SourceKind == DamageSourceKind.DamageShare
                ? "DamageShare"
                : damageApplied.SourceKind.ToString();
            var proxySuffix = damageApplied.SourceProxy != null ? $" via proxy {damageApplied.SourceProxy.ProxyId}" : string.Empty;
            var variantSuffix = !string.IsNullOrWhiteSpace(damageApplied.SourceBasicAttackVariantKey)
                ? $" variant={damageApplied.SourceBasicAttackVariantKey}"
                : string.Empty;
            return $"{attackerName} dealt {damageApplied.DamageAmount:0.0} to {FormatHeroLabel(damageApplied.Target)} via {skillName} [{sourceKindLabel}{variantSuffix}]{proxySuffix}, target HP {Mathf.Max(0f, damageApplied.RemainingHealth):0.0}.";
        }

        private static string FormatHealLog(HealAppliedEvent healApplied)
        {
            var casterName = FormatHeroLabel(healApplied.Caster, "Unknown");
            var skillName = healApplied.SourceSkill != null ? healApplied.SourceSkill.displayName : "Basic Attack";
            var proxySuffix = healApplied.SourceProxy != null ? $" via proxy {healApplied.SourceProxy.ProxyId}" : string.Empty;
            var variantSuffix = !string.IsNullOrWhiteSpace(healApplied.SourceBasicAttackVariantKey)
                ? $" variant={healApplied.SourceBasicAttackVariantKey}"
                : string.Empty;
            return $"{casterName} healed {FormatHeroLabel(healApplied.Target)} for {healApplied.HealAmount:0.0} via {skillName}{variantSuffix}{proxySuffix}, target HP {Mathf.Max(0f, healApplied.ResultingHealth):0.0}.";
        }

        private static string FormatBasicAttackProjectileLog(RuntimeBasicAttackProjectile projectile)
        {
            if (projectile == null)
            {
                return "Unknown basic-attack projectile launched.";
            }

            var proxySuffix = projectile.SourceProxy != null
                ? $" from proxy {projectile.SourceProxy.ProxyId}"
                : string.Empty;
            return $"{FormatHeroLabel(projectile.Attacker)} fired [{FormatBasicAttackVariantLabel(projectile.VariantKey)}] projectile at {FormatHeroLabel(projectile.Target)}{proxySuffix}.";
        }

        private static string FormatDeployableProxySpawnedLog(DeployableProxySpawnedEvent deployableProxySpawned)
        {
            var proxy = deployableProxySpawned?.Proxy;
            if (proxy == null)
            {
                return "Deployable proxy spawned.";
            }

            return $"{FormatHeroLabel(proxy.Owner)} spawned deployable proxy {proxy.ProxyId} at ({proxy.CurrentPosition.x:0.0}, {proxy.CurrentPosition.z:0.0}) for {proxy.TotalDurationSeconds:0.0}s.";
        }

        private static string FormatDeployableProxyRemovedLog(DeployableProxyRemovedEvent deployableProxyRemoved)
        {
            var proxy = deployableProxyRemoved?.Proxy;
            if (proxy == null)
            {
                return "Deployable proxy removed.";
            }

            return $"{FormatHeroLabel(proxy.Owner)} lost deployable proxy {proxy.ProxyId} due to {deployableProxyRemoved.Reason}.";
        }

        private static string FormatStatusLog(StatusAppliedEvent statusApplied)
        {
            var sourceName = FormatHeroLabel(statusApplied.Source, "Unknown");
            var appliedByName = FormatHeroLabel(statusApplied.AppliedBy, sourceName);
            var skillName = statusApplied.SourceSkill != null ? statusApplied.SourceSkill.displayName : "Basic Attack";
            var applierSuffix = statusApplied.AppliedBy != null && statusApplied.AppliedBy != statusApplied.Source
                ? $", applied by {appliedByName}"
                : string.Empty;
            return $"{sourceName} applied {statusApplied.EffectType} to {FormatHeroLabel(statusApplied.Target)} via {skillName}, duration {statusApplied.DurationSeconds:0.0}s, magnitude {FormatStatusMagnitude(statusApplied.EffectType, statusApplied.Magnitude)}{applierSuffix}.";
        }

        private static string FormatStatusRemovedLog(StatusRemovedEvent statusRemoved)
        {
            var sourceName = FormatHeroLabel(statusRemoved.Source, "Unknown");
            var appliedByName = FormatHeroLabel(statusRemoved.AppliedBy, sourceName);
            var skillName = statusRemoved.SourceSkill != null ? statusRemoved.SourceSkill.displayName : "Basic Attack";
            var applierSuffix = statusRemoved.AppliedBy != null && statusRemoved.AppliedBy != statusRemoved.Source
                ? $" (applied by {appliedByName})"
                : string.Empty;
            return $"{statusRemoved.EffectType} on {FormatHeroLabel(statusRemoved.Target)} expired from {sourceName} via {skillName}{applierSuffix}.";
        }

        private static string FormatForcedMovementLog(ForcedMovementAppliedEvent forcedMovementApplied)
        {
            var sourceName = FormatHeroLabel(forcedMovementApplied.Source, "Unknown");
            var skillName = forcedMovementApplied.SourceSkill != null ? forcedMovementApplied.SourceSkill.displayName : "Unknown Effect";
            return $"{sourceName} displaced {FormatHeroLabel(forcedMovementApplied.Target)} via {skillName} from ({forcedMovementApplied.StartPosition.x:0.0}, {forcedMovementApplied.StartPosition.z:0.0}) to ({forcedMovementApplied.Destination.x:0.0}, {forcedMovementApplied.Destination.z:0.0}), duration {forcedMovementApplied.DurationSeconds:0.00}s, peak height {forcedMovementApplied.PeakHeight:0.##}.";
        }

        private static string FormatPassiveSkillValueChangedLog(PassiveSkillValueChangedEvent passiveSkillChanged)
        {
            var heroName = FormatHeroLabel(passiveSkillChanged.Hero, "Unknown");
            var skillName = passiveSkillChanged.Skill != null ? passiveSkillChanged.Skill.displayName : "Passive Skill";
            var valueLabel = passiveSkillChanged.ValueType switch
            {
                PassiveSkillValueType.Defense => "defense bonus",
                PassiveSkillValueType.Lifesteal => "lifesteal",
                _ => "attack bonus",
            };
            return $"{heroName}'s {skillName} updated {valueLabel} to {passiveSkillChanged.ModifierMultiplier * 100f:0.#}%.";
        }

        private static string FormatPassiveStackChangedLog(PassiveStackChangedEvent passiveStackChanged)
        {
            var heroName = FormatHeroLabel(passiveStackChanged.Hero, "Unknown");
            var skillName = passiveStackChanged.Skill != null ? passiveStackChanged.Skill.displayName : "Passive Skill";
            var maxStacksLabel = passiveStackChanged.MaxStacks > 0
                ? passiveStackChanged.MaxStacks.ToString()
                : "-";
            var healLabel = passiveStackChanged.HealAmount > Mathf.Epsilon
                ? $"{passiveStackChanged.HealAmount:0.0} heal"
                : "no heal";
            return $"{heroName}'s {skillName} triggered: stacks {passiveStackChanged.PreviousStackCount}->{passiveStackChanged.CurrentStackCount}/{maxStacksLabel}, attack bonus {passiveStackChanged.AttackPowerBonusMultiplier * 100f:0.#}%, attack speed bonus {passiveStackChanged.AttackSpeedBonusMultiplier * 100f:0.#}%, {healLabel}.";
        }

        private static string FormatPositiveEffectRejectedLog(PositiveEffectRejectedEvent positiveEffectRejected)
        {
            var sourceName = FormatHeroLabel(positiveEffectRejected.Source, "Unknown");
            var targetName = FormatHeroLabel(positiveEffectRejected.Target, "Unknown");
            var sourceLabel = positiveEffectRejected.SourceSkill != null
                ? positiveEffectRejected.SourceSkill.displayName
                : $"basic attack [{FormatBasicAttackVariantLabel(positiveEffectRejected.SourceBasicAttackVariantKey)}]";
            var effectLabel = string.IsNullOrWhiteSpace(positiveEffectRejected.EffectLabel)
                ? "positive effect"
                : positiveEffectRejected.EffectLabel;
            return $"{sourceName}'s {sourceLabel} could not apply {effectLabel} to {targetName} because the target rejects allied positive effects.";
        }

        private static string FormatRadialSweepResolvedLog(RadialSweepResolvedEvent radialSweepResolved)
        {
            var casterName = FormatHeroLabel(radialSweepResolved.Caster, "Unknown");
            var skillName = radialSweepResolved.Skill != null ? radialSweepResolved.Skill.displayName : "Radial Sweep";
            var phaseLabel = radialSweepResolved.Direction == RadialSweepDirectionMode.Inward ? "inward" : "outward";
            return $"{casterName}'s {skillName} {phaseLabel} sweep resolved at ({radialSweepResolved.Center.x:0.0}, {radialSweepResolved.Center.z:0.0}), radius {radialSweepResolved.MaxRadius:0.0}, hits {radialSweepResolved.HitCount} target(s), sweep {radialSweepResolved.SweepId}.";
        }

        private static string FormatSkillTemporaryOverrideChangedLog(SkillTemporaryOverrideChangedEvent temporaryOverrideChanged)
        {
            var heroName = FormatHeroLabel(temporaryOverrideChanged.Hero, "Unknown");
            var skillName = temporaryOverrideChanged.Skill != null ? temporaryOverrideChanged.Skill.displayName : "Skill Override";
            if (!temporaryOverrideChanged.IsActive)
            {
                return $"{heroName}'s {skillName} temporary override ended.";
            }

            return $"{heroName}'s {skillName} temporary override active: lifesteal {temporaryOverrideChanged.LifestealRatio * 100f:0.#}%, visual scale {temporaryOverrideChanged.VisualScaleMultiplier:0.##}x, tint {temporaryOverrideChanged.VisualTintStrength * 100f:0.#}%.";
        }

        private static string FormatReactiveGuardLog(ReactiveGuardTriggeredEvent reactiveGuardTriggered)
        {
            var casterName = FormatHeroLabel(reactiveGuardTriggered.Caster, "Unknown");
            var protectedName = FormatHeroLabel(reactiveGuardTriggered.ProtectedHero, "none");
            var skillName = reactiveGuardTriggered.SourceSkill != null ? reactiveGuardTriggered.SourceSkill.displayName : "Reactive Guard";
            return $"{casterName}'s {skillName} triggered around {protectedName}, affecting {reactiveGuardTriggered.AffectedTargetCount} enemy target(s).";
        }

        private static string FormatUltimateDecisionLog(UltimateDecisionEvaluatedEvent ultimateDecision)
        {
            if (ultimateDecision == null)
            {
                return "UltimateDecision <null>";
            }

            var casterLabel = FormatHeroLabel(ultimateDecision.Caster, "Unknown");
            var skillName = ultimateDecision.Skill != null ? ultimateDecision.Skill.displayName : "Unknown Ultimate";
            var pathLabel = ultimateDecision.UsesTemplateDecision ? "template" : "legacy";
            var targetLabel = FormatHeroLabel(ultimateDecision.PrimaryTarget, "none");
            var chanceSummary = ultimateDecision.ChanceEvaluated
                ? ultimateDecision.ChanceSummary
                : "chance=skipped";
            var rollSummary = ultimateDecision.RollEvaluated
                ? $" roll={ultimateDecision.RollValue:0.000} pass={ultimateDecision.RollPassed}"
                : ultimateDecision.ChanceEvaluated
                    ? " roll=auto-pass"
                    : string.Empty;

            return $"UltimateDecision {casterLabel} skill={skillName} path={pathLabel} result={ultimateDecision.Outcome} target={targetLabel} affected={ultimateDecision.AffectedTargetCount} fallbackStage={ultimateDecision.FallbackStage} detail={{ {ultimateDecision.DecisionSummary} }} {chanceSummary}{rollSummary} nextCheck={ultimateDecision.NextDecisionCheckTimeSeconds:0.00}";
        }

        private void AppendBlueWarriorSpotlight(StringBuilder builder)
        {
            builder.AppendLine("Blue Warrior Spotlight (Skybreaker Slot)");
            builder.AppendLine("Alias note: this section tracks the blue-side warrior slot, expected to be Skybreaker in the stage-01 roster.");

            if (string.IsNullOrWhiteSpace(trackedBlueWarriorHeroId))
            {
                builder.AppendLine("Tracked hero: none found on the blue team for this battle.");
                builder.AppendLine("Active skill casts: 0");
                builder.AppendLine("Ultimate casts: 0");
                builder.AppendLine("KnockUp statuses applied: 0");
                builder.AppendLine("Focused events: none");
                return;
            }

            builder.AppendLine($"Tracked hero: {trackedBlueWarriorDisplayName} ({trackedBlueWarriorHeroId})");
            builder.AppendLine($"Active skill casts: {blueWarriorActiveSkillCastCount}");
            builder.AppendLine($"Ultimate casts: {blueWarriorUltimateCastCount}");
            builder.AppendLine($"KnockUp statuses applied: {blueWarriorKnockUpAppliedCount}");

            if (blueWarriorSpotlightEntries.Count == 0)
            {
                builder.AppendLine("Focused events: none recorded in this battle.");
                return;
            }

            builder.AppendLine("Focused events:");
            for (var i = 0; i < blueWarriorSpotlightEntries.Count; i++)
            {
                builder.AppendLine(blueWarriorSpotlightEntries[i]);
            }
        }

        private void AppendUltimateDecisionTrace(StringBuilder builder)
        {
            builder.AppendLine("Ultimate Decision Trace");

            if (ultimateDecisionEntries.Count == 0)
            {
                builder.AppendLine("No ultimate decision entries recorded in this battle.");
                return;
            }

            for (var i = 0; i < ultimateDecisionEntries.Count; i++)
            {
                builder.AppendLine(ultimateDecisionEntries[i]);
            }
        }

        private void ResetBlueWarriorSpotlight()
        {
            blueWarriorSpotlightEntries.Clear();
            trackedBlueWarriorHeroId = null;
            trackedBlueWarriorDisplayName = null;
            trackedBlueWarriorSlotIndex = -1;
            blueWarriorActiveSkillCastCount = 0;
            blueWarriorUltimateCastCount = 0;
            blueWarriorKnockUpAppliedCount = 0;
        }

        private void CaptureTrackedBlueWarrior(BattleInputConfig input)
        {
            var hero = SelectBlueWarriorDefinition(input, out var slotIndex);
            if (hero == null)
            {
                return;
            }

            trackedBlueWarriorHeroId = hero.heroId;
            trackedBlueWarriorDisplayName = string.IsNullOrWhiteSpace(hero.displayName) ? hero.heroId : hero.displayName;
            trackedBlueWarriorSlotIndex = slotIndex;
        }

        private static HeroDefinition SelectBlueWarriorDefinition(BattleInputConfig input, out int slotIndex)
        {
            slotIndex = -1;
            var heroes = input?.blueTeam?.heroes;
            if (heroes == null)
            {
                return null;
            }

            for (var i = 0; i < heroes.Count; i++)
            {
                if (IsMartialArtistDefinition(heroes[i]))
                {
                    slotIndex = i;
                    return heroes[i];
                }
            }

            for (var i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                if (hero != null && hero.heroClass == HeroClass.Warrior)
                {
                    slotIndex = i;
                    return hero;
                }
            }

            return null;
        }

        private static bool IsMartialArtistDefinition(HeroDefinition hero)
        {
            if (hero == null)
            {
                return false;
            }

            return string.Equals(hero.heroId, "warrior_001_skybreaker", System.StringComparison.OrdinalIgnoreCase);
        }

        private void TryAddBlueWarriorSkillCast(SkillCastEvent skillCast)
        {
            if (skillCast == null || !IsTrackedBlueWarrior(skillCast.Caster) || skillCast.Skill == null)
            {
                return;
            }

            var slotLabel = skillCast.Skill.slotType == SkillSlotType.Ultimate ? "Ultimate" : "ActiveSkill";
            if (skillCast.Skill.slotType == SkillSlotType.Ultimate)
            {
                blueWarriorUltimateCastCount++;
            }
            else
            {
                blueWarriorActiveSkillCastCount++;
            }

            AddBlueWarriorSpotlightLog($"[{slotLabel}] {FormatHeroLabel(skillCast.Caster)} cast {skillCast.Skill.displayName} on {FormatHeroLabel(skillCast.PrimaryTarget, "area")} affecting {skillCast.AffectedTargetCount} target(s).");
        }

        private void TryAddBlueWarriorKnockUp(StatusAppliedEvent statusApplied)
        {
            if (statusApplied == null
                || statusApplied.EffectType != StatusEffectType.KnockUp
                || !IsTrackedBlueWarrior(statusApplied.Source))
            {
                return;
            }

            blueWarriorKnockUpAppliedCount++;
            var skillName = statusApplied.SourceSkill != null ? statusApplied.SourceSkill.displayName : "Unknown Effect";
            AddBlueWarriorSpotlightLog($"[KnockUp] {FormatHeroLabel(statusApplied.Source)} applied KnockUp to {FormatHeroLabel(statusApplied.Target)} via {skillName}, duration {statusApplied.DurationSeconds:0.0}s, magnitude {statusApplied.Magnitude:0.##}.");
        }

        private bool IsTrackedBlueWarrior(RuntimeHero hero)
        {
            if (hero == null || hero.Side != TeamSide.Blue || hero.Definition == null)
            {
                return false;
            }

            if (trackedBlueWarriorSlotIndex >= 0 && hero.SlotIndex != trackedBlueWarriorSlotIndex)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(trackedBlueWarriorHeroId))
            {
                return string.Equals(hero.Definition.heroId, trackedBlueWarriorHeroId, System.StringComparison.OrdinalIgnoreCase);
            }

            return IsMartialArtistDefinition(hero.Definition) || hero.Definition.heroClass == HeroClass.Warrior;
        }

        private static string FormatHeroLabel(RuntimeHero hero, string fallback = "none")
        {
            if (hero == null)
            {
                return fallback;
            }

            var displayName = hero.Definition != null && !string.IsNullOrWhiteSpace(hero.Definition.displayName)
                ? hero.Definition.displayName
                : "UnknownHero";
            return $"{displayName}[{hero.Side}|{hero.RuntimeId}]";
        }

        private static string FormatStatusMagnitude(StatusEffectType effectType, float magnitude)
        {
            return effectType == StatusEffectType.DamageShare
                || effectType == StatusEffectType.HealTakenModifier
                ? $"{magnitude * 100f:0.#}%"
                : magnitude.ToString("0.##");
        }

        private static string FormatBasicAttackVariantLabel(string variantKey)
        {
            return string.IsNullOrWhiteSpace(variantKey) ? "default" : variantKey;
        }
    }
}
