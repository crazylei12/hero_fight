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

        private string trackedBlueWarriorHeroId;
        private string trackedBlueWarriorDisplayName;
        private int trackedBlueWarriorSlotIndex = -1;
        private int blueWarriorActiveSkillCastCount;
        private int blueWarriorUltimateCastCount;
        private int blueWarriorKnockUpAppliedCount;

        public BattleLogSession()
        {
            CurrentBattleLogId = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        public string CurrentBattleLogId { get; private set; }

        public bool HasEvents => fullLogEntries.Count > 0;

        public void HandleBattleEvent(IBattleEvent battleEvent)
        {
            switch (battleEvent)
            {
                case BattleStartedEvent started:
                    fullLogEntries.Clear();
                    CurrentBattleLogId = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    ResetBlueWarriorSpotlight();
                    CaptureTrackedBlueWarrior(started.Input);
                    AddLog($"Battle started. Session {CurrentBattleLogId}.");
                    break;
                case UnitSpawnedEvent spawned:
                    AddLog($"{FormatHeroLabel(spawned.Hero)} spawned for {spawned.Hero.Side}.");
                    break;
                case TargetChangedEvent targetChanged:
                    AddLog($"{FormatHeroLabel(targetChanged.Hero)} targets {FormatHeroLabel(targetChanged.Target)}.");
                    break;
                case AttackPerformedEvent attackPerformed:
                    AddLog($"{FormatHeroLabel(attackPerformed.Attacker)} started a basic attack on {FormatHeroLabel(attackPerformed.Target)}.");
                    break;
                case BasicAttackProjectileLaunchedEvent projectileLaunched:
                    AddLog($"{FormatHeroLabel(projectileLaunched.Projectile.Attacker)} fired a projectile at {FormatHeroLabel(projectileLaunched.Projectile.Target)}.");
                    break;
                case SkillCastEvent skillCast:
                    AddLog($"{FormatHeroLabel(skillCast.Caster)} started casting {skillCast.Skill.displayName} on {FormatHeroLabel(skillCast.PrimaryTarget, "area")} ({skillCast.AffectedTargetCount} target(s)).");
                    TryAddBlueWarriorSkillCast(skillCast);
                    break;
                case SkillAreaCreatedEvent areaCreated:
                    var areaDuration = areaCreated.Area?.Effect != null ? areaCreated.Area.Effect.durationSeconds : 0f;
                    AddLog($"{FormatHeroLabel(areaCreated.Caster)} created {areaCreated.Skill.displayName} area {areaCreated.Area?.AreaId ?? "unknown"} for {areaDuration:0.0}s.");
                    break;
                case SkillAreaPulseEvent areaPulse:
                    var center = areaPulse.Area != null ? areaPulse.Area.CurrentCenter : Vector3.zero;
                    AddLog($"{FormatHeroLabel(areaPulse.Caster)}'s {areaPulse.Skill.displayName} pulse affected {areaPulse.AffectedTargetCount} target(s) at ({center.x:0.0}, {center.z:0.0}), area {areaPulse.Area?.AreaId ?? "unknown"}.");
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
            fullLogEntries.Add($"[{Time.timeSinceLevelLoad:0.0}] {message}");
        }

        private void AddBlueWarriorSpotlightLog(string message)
        {
            blueWarriorSpotlightEntries.Add($"[{Time.timeSinceLevelLoad:0.0}] {message}");
        }

        private static string FormatDamageLog(DamageAppliedEvent damageApplied)
        {
            var attackerName = FormatHeroLabel(damageApplied.Attacker, "Unknown");
            var skillName = damageApplied.SourceSkill != null ? damageApplied.SourceSkill.displayName : "Basic Attack";
            var sourceKindLabel = damageApplied.SourceKind == DamageSourceKind.DamageShare
                ? "DamageShare"
                : damageApplied.SourceKind.ToString();
            return $"{attackerName} dealt {damageApplied.DamageAmount:0.0} to {FormatHeroLabel(damageApplied.Target)} via {skillName} [{sourceKindLabel}], target HP {Mathf.Max(0f, damageApplied.RemainingHealth):0.0}.";
        }

        private static string FormatHealLog(HealAppliedEvent healApplied)
        {
            var casterName = FormatHeroLabel(healApplied.Caster, "Unknown");
            var skillName = healApplied.SourceSkill != null ? healApplied.SourceSkill.displayName : "Basic Attack";
            return $"{casterName} healed {FormatHeroLabel(healApplied.Target)} for {healApplied.HealAmount:0.0} via {skillName}, target HP {Mathf.Max(0f, healApplied.ResultingHealth):0.0}.";
        }

        private static string FormatStatusLog(StatusAppliedEvent statusApplied)
        {
            var sourceName = FormatHeroLabel(statusApplied.Source, "Unknown");
            var skillName = statusApplied.SourceSkill != null ? statusApplied.SourceSkill.displayName : "Basic Attack";
            return $"{sourceName} applied {statusApplied.EffectType} to {FormatHeroLabel(statusApplied.Target)} via {skillName}, duration {statusApplied.DurationSeconds:0.0}s, magnitude {FormatStatusMagnitude(statusApplied.EffectType, statusApplied.Magnitude)}.";
        }

        private static string FormatStatusRemovedLog(StatusRemovedEvent statusRemoved)
        {
            var sourceName = FormatHeroLabel(statusRemoved.Source, "Unknown");
            var skillName = statusRemoved.SourceSkill != null ? statusRemoved.SourceSkill.displayName : "Basic Attack";
            return $"{statusRemoved.EffectType} on {FormatHeroLabel(statusRemoved.Target)} expired from {sourceName} via {skillName}.";
        }

        private static string FormatForcedMovementLog(ForcedMovementAppliedEvent forcedMovementApplied)
        {
            var sourceName = FormatHeroLabel(forcedMovementApplied.Source, "Unknown");
            var skillName = forcedMovementApplied.SourceSkill != null ? forcedMovementApplied.SourceSkill.displayName : "Unknown Effect";
            return $"{sourceName} displaced {FormatHeroLabel(forcedMovementApplied.Target)} via {skillName} from ({forcedMovementApplied.StartPosition.x:0.0}, {forcedMovementApplied.StartPosition.z:0.0}) to ({forcedMovementApplied.Destination.x:0.0}, {forcedMovementApplied.Destination.z:0.0}), duration {forcedMovementApplied.DurationSeconds:0.00}s, peak height {forcedMovementApplied.PeakHeight:0.##}.";
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
                ? $"{magnitude * 100f:0.#}%"
                : magnitude.ToString("0.##");
        }
    }
}
