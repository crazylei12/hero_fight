using Fight.UI;
using UnityEditor;
using UnityEngine;

namespace Fight.Editor
{
    public static class BattleHudThemeGenerator
    {
        private const string ThemeAssetPath = "Assets/Resources/UI/BattleHudTheme.asset";

        [MenuItem("Fight/Dev/Refresh Battle HUD Theme")]
        public static void GenerateDefaultTheme()
        {
            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "UI");

            var theme = AssetDatabase.LoadAssetAtPath<BattleHudTheme>(ThemeAssetPath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<BattleHudTheme>();
                AssetDatabase.CreateAsset(theme, ThemeAssetPath);
            }

            theme.topFrame = LoadSprite("Assets/Synty/InterfaceFantasyWarriorHUD/Sprites/HUD/SPR_HUD_FantasyWarrior_Frame_Box_Medium_05.png");
            theme.topBanner = LoadSprite("Assets/Synty/InterfaceFantasyWarriorHUD/Sprites/HUD/SPR_HUD_FantasyWarrior_Banner_08_Fill_01.png");
            theme.topLineLeft = LoadSprite("Assets/Synty/InterfaceFantasyWarriorHUD/Sprites/HUD/SPR_HUD_FantasyWarrior_Line_03_Left.png");
            theme.topLineRight = LoadSprite("Assets/Synty/InterfaceFantasyWarriorHUD/Sprites/HUD/SPR_HUD_FantasyWarrior_Line_03_Right.png");
            theme.sidebarFrame = LoadSprite("Assets/Synty/InterfaceFantasyWarriorHUD/Sprites/HUD/SPR_HUD_FantasyWarrior_Tracery_Box_01.png");
            theme.cardBackground = LoadSprite("Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Frame/CardFrame_02_BgGradient.png");
            theme.cardBorder = LoadSprite("Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Frame/CardFrame_02_Border.png");
            theme.portraitFrame = LoadSprite("Assets/Layer Lab/GUI-CasualFantasy/ResourcesData/Sprites/Components/Frame/ProfileFrame01_White.png");
            theme.nameplateBackground = LoadSprite("Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Frame/LineTextFrame_05_Bg.png");
            theme.nameplateLine = LoadSprite("Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Frame/LineTextFrame_05_BgLine.png");
            theme.deadIcon = LoadSprite("Assets/Synty/InterfaceFantasyWarriorHUD/Sprites/Icons_Status/ICON_FantasyWarrior_Status_Dead_01_Clean.png");

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[BattleHudThemeGenerator] Refreshed Battle HUD theme asset.");
        }

        public static void GenerateDefaultThemeBatchmode()
        {
            try
            {
                GenerateDefaultTheme();
                EditorApplication.Exit(0);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureFolder(string parent, string child)
        {
            var fullPath = $"{parent}/{child}";
            if (AssetDatabase.IsValidFolder(fullPath))
            {
                return;
            }

            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
