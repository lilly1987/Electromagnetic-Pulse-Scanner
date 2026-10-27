using Verse;

namespace ElectromagneticPulseGenerator
{
    public class CompProperties_PassiveEPGScanner : CompProperties
    {
        public string requiredUpgradeKey = "EPGPassiveScanner";
        public float meanTimeBetweenFindsDays = 0.5f;
        public int maxScanTimeTicks = 45000;

        public CompProperties_PassiveEPGScanner()
        {
            compClass = typeof(CompPassiveEPGScanner);
        }
    }
}
