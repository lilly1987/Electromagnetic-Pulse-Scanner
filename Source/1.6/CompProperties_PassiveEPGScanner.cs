using Verse;

namespace ElectromagneticPulseGenerator
{
    public class CompProperties_PassiveEPGScanner : CompProperties
    {
        public string requiredUpgradeKey = "EPGPassiveScanner";
        public float meanTimeBetweenFindsDays = 1.0f;
        public int maxScanTimeTicks = 90000;

        public CompProperties_PassiveEPGScanner()
        {
            compClass = typeof(CompPassiveEPGScanner);
        }
    }
}
