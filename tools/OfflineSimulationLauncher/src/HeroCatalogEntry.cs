namespace Fight.Tools.OfflineSimulationLauncher
{
    internal sealed class HeroCatalogEntry
    {
        public string HeroId { get; set; }

        public string DisplayName { get; set; }

        public string HeroClass { get; set; }

        public string DisplayText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(HeroId))
                {
                    return "(随机补位)";
                }

                return string.IsNullOrWhiteSpace(HeroClass)
                    ? DisplayName + " [" + HeroId + "]"
                    : DisplayName + " (" + HeroClass + ") [" + HeroId + "]";
            }
        }

        public override string ToString()
        {
            return DisplayText;
        }
    }
}
