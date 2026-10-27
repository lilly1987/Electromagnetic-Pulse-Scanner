using Verse;

namespace ElectromagneticPulseGenerator
{
    public class CompProperties_EPGScanner : CompProperties
    {
        public CompProperties_EPGScanner()
        {
            this.compClass = typeof(CompEPGScanner);
        }
    // Mean time between finds, in in-game days
    public float meanTimeBetweenFindsDays = 0.5f;
    public int maxScanTimeTicks = 45000;
    }
}
