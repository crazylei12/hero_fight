using UnityEngine;

namespace Fight.UI
{
    [CreateAssetMenu(fileName = "BattleHudTheme", menuName = "Fight/UI/Battle HUD Theme")]
    public sealed class BattleHudTheme : ScriptableObject
    {
        [Header("Top Scoreboard")]
        public Sprite topFrame;
        public Sprite topBanner;
        public Sprite topLineLeft;
        public Sprite topLineRight;

        [Header("Sidebars")]
        public Sprite sidebarFrame;
        public Sprite cardBackground;
        public Sprite cardBorder;
        public Sprite portraitFrame;

        [Header("World Nameplate")]
        public Sprite nameplateBackground;
        public Sprite nameplateLine;

        [Header("Icons")]
        public Sprite deadIcon;
    }
}
