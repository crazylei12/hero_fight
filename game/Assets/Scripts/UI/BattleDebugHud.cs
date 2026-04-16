using System.Collections.Generic;
using System.IO;
using System.Text;
using Fight.Battle;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.UI
{
    [RequireComponent(typeof(BattleManager))]
    public class BattleDebugHud : MonoBehaviour
    {
        [SerializeField] private Vector2 buttonOffset = new Vector2(390f, 108f);
        [SerializeField] private Vector2 statusOffset = new Vector2(560f, 114f);
        [SerializeField] private bool autoExportOnBattleEnd = true;
        [SerializeField] private string exportFolderName = "BattleLogs";

        private BattleManager battleManager;
        private readonly List<string> fullLogEntries = new List<string>();
        private readonly List<string> blueWarriorSpotlightEntries = new List<string>();
        private GUIStyle bodyStyle;
        private string currentBattleLogId;
        private string lastExportPath;
        private string exportStatusMessage = "No log exported yet.";
        private bool exportRequested;
        private string trackedBlueWarriorHeroId;
        private string trackedBlueWarriorDisplayName;
        private int trackedBlueWarriorSlotIndex = -1;
        private int blueWarriorActiveSkillCastCount;
        private int blueWarriorUltimateCastCount;
        private int blueWarriorKnockUpAppliedCount;

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
            currentBattleLogId = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        private void Update()
        {
            TryBindToBattleEvents();
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawExportControls();
            GUI.Label(new Rect(statusOffset.x, statusOffset.y, 620f, 28f), exportStatusMessage, bodyStyle);
        }

        private void DrawExportControls()
        {
            var buttonRect = new Rect(buttonOffset.x, buttonOffset.y, 162f, 32f);
            if (GUI.Button(buttonRect, "Export Battle Log"))
            {
                exportRequested = true;
            }
        }

        private void TryBindToBattleEvents()
        {
            var context = battleManager.Context;
            if (context == null || context.EventBus == null)
            {
                return;
            }

            context.EventBus.Published -= OnBattleEvent;
            context.EventBus.Published += OnBattleEvent;
        }

        private void OnDisable()
        {
            var context = battleManager != null ? battleManager.Context : null;
            if (context?.EventBus != null)
            {
                context.EventBus.Published -= OnBattleEvent;
            }
        }

        private void OnBattleEvent(IBattleEvent battleEvent)
        {
            switch (battleEvent)
            {
                case BattleStartedEvent _:
                    fullLogEntries.Clear();
                    currentBattleLogId = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    exportStatusMessage = $"Log session ready: {currentBattleLogId}";
                    ResetBlueWarriorSpotlight();
                    CaptureTrackedBlueWarrior();
                    AddLog($"Battle started. Session {currentBattleLogId}.");
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
                    if (autoExportOnBattleEnd)
                    {
                        exportRequested = true;
                    }
                    break;
            }
        }

        private void AddLog(string message)
        {
            var formatted = $"[{Time.timeSinceLevelLoad:0.0}] {message}";
            fullLogEntries.Add(formatted);
        }

        private void AddBlueWarriorSpotlightLog(string message)
        {
            blueWarriorSpotlightEntries.Add($"[{Time.timeSinceLevelLoad:0.0}] {message}");
        }

        private void LateUpdate()
        {
            if (!exportRequested)
            {
                return;
            }

            exportRequested = false;
            ExportBattleLog();
        }

        private string FormatDamageLog(DamageAppliedEvent damageApplied)
        {
            var attackerName = FormatHeroLabel(damageApplied.Attacker, "Unknown");
            var skillName = damageApplied.SourceSkill != null ? damageApplied.SourceSkill.displayName : "Basic Attack";
            return $"{attackerName} dealt {damageApplied.DamageAmount:0.0} to {FormatHeroLabel(damageApplied.Target)} via {skillName} [{damageApplied.SourceKind}], target HP {Mathf.Max(0f, damageApplied.RemainingHealth):0.0}.";
        }

        private string FormatHealLog(HealAppliedEvent healApplied)
        {
            var casterName = FormatHeroLabel(healApplied.Caster, "Unknown");
            var skillName = healApplied.SourceSkill != null ? healApplied.SourceSkill.displayName : "Basic Attack";
            return $"{casterName} healed {FormatHeroLabel(healApplied.Target)} for {healApplied.HealAmount:0.0} via {skillName}, target HP {Mathf.Max(0f, healApplied.ResultingHealth):0.0}.";
        }

        private string FormatStatusLog(StatusAppliedEvent statusApplied)
        {
            var sourceName = FormatHeroLabel(statusApplied.Source, "Unknown");
            var skillName = statusApplied.SourceSkill != null ? statusApplied.SourceSkill.displayName : "Unknown Effect";
            return $"{sourceName} applied {statusApplied.EffectType} to {FormatHeroLabel(statusApplied.Target)} via {skillName}, duration {statusApplied.DurationSeconds:0.0}s, magnitude {statusApplied.Magnitude:0.##}.";
        }

        private string FormatStatusRemovedLog(StatusRemovedEvent statusRemoved)
        {
            var sourceName = FormatHeroLabel(statusRemoved.Source, "Unknown");
            var skillName = statusRemoved.SourceSkill != null ? statusRemoved.SourceSkill.displayName : "Unknown Effect";
            return $"{statusRemoved.EffectType} on {FormatHeroLabel(statusRemoved.Target)} expired from {sourceName} via {skillName}.";
        }

        private string FormatForcedMovementLog(ForcedMovementAppliedEvent forcedMovementApplied)
        {
            var sourceName = FormatHeroLabel(forcedMovementApplied.Source, "Unknown");
            var skillName = forcedMovementApplied.SourceSkill != null ? forcedMovementApplied.SourceSkill.displayName : "Unknown Effect";
            return $"{sourceName} displaced {FormatHeroLabel(forcedMovementApplied.Target)} via {skillName} from ({forcedMovementApplied.StartPosition.x:0.0}, {forcedMovementApplied.StartPosition.z:0.0}) to ({forcedMovementApplied.Destination.x:0.0}, {forcedMovementApplied.Destination.z:0.0}), duration {forcedMovementApplied.DurationSeconds:0.00}s, peak height {forcedMovementApplied.PeakHeight:0.##}.";
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

        private void ExportBattleLog()
        {
            if (fullLogEntries.Count == 0)
            {
                exportStatusMessage = "No events yet. Start a battle before exporting.";
                return;
            }

            try
            {
                var directory = Path.Combine(Application.persistentDataPath, exportFolderName);
                Directory.CreateDirectory(directory);
                var logId = string.IsNullOrWhiteSpace(currentBattleLogId)
                    ? System.DateTime.Now.ToString("yyyyMMdd_HHmmss")
                    : currentBattleLogId;
                var path = Path.Combine(directory, $"battle_log_{logId}.txt");
                File.WriteAllText(path, BuildExportText());
                lastExportPath = path;
                exportStatusMessage = $"Exported to: {lastExportPath}";
                Debug.Log($"[BattleLog] Exported battle log to {lastExportPath}");
            }
            catch (IOException exception)
            {
                exportStatusMessage = $"Export failed: {exception.Message}";
                Debug.LogError($"[BattleLog] Failed to export battle log. {exception}");
            }
        }

        private string BuildExportText()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Battle Debug Log Export");
            builder.AppendLine($"Session: {currentBattleLogId}");
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

        private void AppendBlueWarriorSpotlight(StringBuilder builder)
        {
            builder.AppendLine("Blue Warrior Spotlight (Martial Artist Slot)");
            builder.AppendLine("Alias note: this section tracks the blue-side warrior hero even if the runtime asset name differs from planning docs.");

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

        private void CaptureTrackedBlueWarrior()
        {
            var input = battleManager != null && battleManager.Context != null ? battleManager.Context.Input : null;
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

            return string.Equals(hero.heroId, "warrior_001_bladeguard", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(hero.heroId, "warrior_001_skybreaker", System.StringComparison.OrdinalIgnoreCase);
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

            AddBlueWarriorSpotlightLog(
                $"[{slotLabel}] {FormatHeroLabel(skillCast.Caster)} cast {skillCast.Skill.displayName} on {FormatHeroLabel(skillCast.PrimaryTarget, "area")} affecting {skillCast.AffectedTargetCount} target(s).");
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
            AddBlueWarriorSpotlightLog(
                $"[KnockUp] {FormatHeroLabel(statusApplied.Source)} applied KnockUp to {FormatHeroLabel(statusApplied.Target)} via {skillName}, duration {statusApplied.DurationSeconds:0.0}s, magnitude {statusApplied.Magnitude:0.##}.");
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

        private void EnsureStyles()
        {
            if (bodyStyle != null)
            {
                return;
            }

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                normal = { textColor = Color.white }
            };
        }
    }
}
