using UnityEngine;
using Verse;

namespace ElectromagneticPulseGenerator
{
    public class EPGMod : Mod
    {
        public static EPGSettings Settings { get; private set; }

        public EPGMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<EPGSettings>();
        }

        public override string SettingsCategory()
        {
            return "Electromagnetic Pulse Scanner";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label($"Mean time between finds: {Settings.meanTimeBetweenFindsDays:0.0} days");
            Settings.meanTimeBetweenFindsDays = listing.Slider(
                Settings.meanTimeBetweenFindsDays,
                EPGSettings.MinDays,
                EPGSettings.MaxDays);

            listing.Gap();

            float maxScanDays = Settings.maxScanTimeTicks / 60000f;
            listing.Label($"Guaranteed scan limit: {maxScanDays:0.0} days ({Settings.maxScanTimeTicks} ticks)");
            maxScanDays = listing.Slider(maxScanDays, EPGSettings.MinDays, EPGSettings.MaxDays);
            Settings.maxScanTimeTicks = Mathf.RoundToInt(maxScanDays * 60000f);

            listing.End();
        }
    }

    public class EPGSettings : ModSettings
    {
        public const float MinDays = 0.1f;
        public const float MaxDays = 10f;

        public float meanTimeBetweenFindsDays = 0.5f;
        public int maxScanTimeTicks = 45000;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref meanTimeBetweenFindsDays, "meanTimeBetweenFindsDays", 0.5f);
            Scribe_Values.Look(ref maxScanTimeTicks, "maxScanTimeTicks", 45000);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                meanTimeBetweenFindsDays = Mathf.Clamp(meanTimeBetweenFindsDays, MinDays, MaxDays);
                maxScanTimeTicks = Mathf.Clamp(maxScanTimeTicks, 6000, 600000);
            }
        }
    }
}
