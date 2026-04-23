using Fight.Battle;
using UnityEngine;

namespace Fight.UI
{
    [RequireComponent(typeof(BattleManager))]
    public class BattleDebugHud : MonoBehaviour
    {
        [SerializeField] private Vector2 buttonOffset = new Vector2(390f, 108f);
        [SerializeField] private Vector2 statusOffset = new Vector2(560f, 114f);
        [SerializeField] private bool autoExportOnBattleEnd = true;
        [SerializeField] private string exportFolderName = BattleLogExportUtility.DefaultExportFolderName;

        private BattleManager battleManager;
        private BattleEventBus boundEventBus;
        private readonly BattleLogSession logSession = new BattleLogSession();
        private GUIStyle bodyStyle;
        private string exportStatusMessage = "No log exported yet.";
        private bool exportRequested;

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
            if (battleManager != null)
            {
                battleManager.ContextInitialized += OnContextInitialized;
            }
        }

        private void Update()
        {
            BindToBattleEvents(battleManager != null ? battleManager.Context?.EventBus : null);
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawExportControls();
            GUI.Label(new Rect(statusOffset.x, statusOffset.y, 620f, 28f), exportStatusMessage, bodyStyle);
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

        private void OnDisable()
        {
            if (boundEventBus != null)
            {
                boundEventBus.Published -= OnBattleEvent;
                boundEventBus = null;
            }

            if (battleManager != null)
            {
                battleManager.ContextInitialized -= OnContextInitialized;
            }
        }

        private void OnContextInitialized(BattleContext context)
        {
            logSession.SetTimeProvider(() => context?.Clock?.ElapsedTimeSeconds ?? 0f);
            BindToBattleEvents(context?.EventBus);
        }

        private void DrawExportControls()
        {
            var buttonRect = new Rect(buttonOffset.x, buttonOffset.y, 162f, 32f);
            if (GUI.Button(buttonRect, "Export Battle Log"))
            {
                exportRequested = true;
            }
        }

        private void BindToBattleEvents(BattleEventBus eventBus)
        {
            if (eventBus == null || ReferenceEquals(boundEventBus, eventBus))
            {
                return;
            }

            if (boundEventBus != null)
            {
                boundEventBus.Published -= OnBattleEvent;
            }

            boundEventBus = eventBus;
            boundEventBus.Published += OnBattleEvent;
        }

        private void OnBattleEvent(IBattleEvent battleEvent)
        {
            logSession.HandleBattleEvent(battleEvent);

            if (battleEvent is BattleStartedEvent)
            {
                exportStatusMessage = $"Log session ready: {logSession.CurrentBattleLogId}";
                return;
            }

            if (battleEvent is BattleEndedEvent && autoExportOnBattleEnd)
            {
                exportRequested = true;
            }
        }

        private void ExportBattleLog()
        {
            if (!logSession.HasEvents)
            {
                exportStatusMessage = "No events yet. Start a battle before exporting.";
                return;
            }

            if (BattleLogExportUtility.TryExport(logSession.BuildExportText(), logSession.CurrentBattleLogId, out var path, out var errorMessage, exportFolderName))
            {
                exportStatusMessage = $"Exported to: {path}";
                Debug.Log($"[BattleLog] Exported battle log to {path}");
            }
            else
            {
                exportStatusMessage = $"Export failed: {errorMessage}";
                Debug.LogError($"[BattleLog] Failed to export battle log. {errorMessage}");
            }
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
