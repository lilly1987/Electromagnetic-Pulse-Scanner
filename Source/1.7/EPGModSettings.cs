using UnityEngine;
using Verse;

namespace ElectromagneticPulseGenerator
{
    public class EPGMod : Mod
    {
        public static EPGSettings Settings { get; private set; }

        private string meanTimeBetweenFindsBuffer;
        private string maxScanTimeBuffer;

        public EPGMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<EPGSettings>();
            meanTimeBetweenFindsBuffer = Mathf.RoundToInt(
                Settings.meanTimeBetweenFindsDays * EPGSettings.TicksPerDay).ToString();
            maxScanTimeBuffer = Settings.maxScanTimeTicks.ToString();
        }

        public override string SettingsCategory()
        {
            return "EPG_SettingsCategory".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            int meanTimeBetweenFindsTicks = Mathf.RoundToInt(
                Settings.meanTimeBetweenFindsDays * EPGSettings.TicksPerDay);
            DrawTimeSetting(
                listing,
                "EPG_MeanTimeLabel",
                "EPG_MeanTimeTooltip",
                ref meanTimeBetweenFindsTicks,
                ref meanTimeBetweenFindsBuffer);
            Settings.meanTimeBetweenFindsDays =
                (float)meanTimeBetweenFindsTicks / EPGSettings.TicksPerDay;
            listing.Gap();

            DrawTimeSetting(
                listing,
                "EPG_MaxScanTimeLabel",
                "EPG_MaxScanTimeTooltip",
                ref Settings.maxScanTimeTicks,
                ref maxScanTimeBuffer);

            listing.End();
        }

        private static void DrawTimeSetting(
            Listing_Standard listing,
            string labelKey,
            string tooltipKey,
            ref int ticks,
            ref string editBuffer)
        {
            ticks = Mathf.Clamp(ticks, EPGSettings.MinTicks, EPGSettings.MaxTicks);
            listing.Label(labelKey.Translate(FormatTime(ticks)), tipSignal: tooltipKey.Translate());

            Rect row = listing.GetRect(30f);
            const float inputWidth = 90f;
            const float gap = 8f;
            Rect sliderRect = new Rect(row.x, row.y, row.width - inputWidth - gap, row.height);
            Rect inputRect = new Rect(row.xMax - inputWidth, row.y, inputWidth, row.height);

            int previousTicks = ticks;
            ticks = Mathf.RoundToInt(Widgets.HorizontalSlider(
                sliderRect,
                ticks,
                EPGSettings.MinTicks,
                EPGSettings.MaxTicks,
                roundTo: 1f));
            if (ticks != previousTicks)
            {
                editBuffer = ticks.ToString();
            }

            Listing_Standard input = new Listing_Standard();
            input.Begin(inputRect);
            input.TextFieldNumeric(ref ticks, ref editBuffer, EPGSettings.MinTicks, EPGSettings.MaxTicks);
            input.End();
        }

        private static string FormatTime(int ticks)
        {
            if (ticks < EPGSettings.TicksPerDay)
            {
                return "EPG_TimeSeconds".Translate(ticks / (float)EPGSettings.TicksPerSecond, ticks);
            }

            return "EPG_TimeDays".Translate(ticks / (float)EPGSettings.TicksPerDay, ticks);
        }
    }

    public class EPGSettings : ModSettings
    {
        public const int TicksPerSecond = 60;
        public const int TicksPerDay = 60000;
        public const int MinTicks = TicksPerSecond;
        public const int MaxTicks = 10 * TicksPerDay;

        public float meanTimeBetweenFindsDays = 0.5f;
        public int maxScanTimeTicks = 45000;
        public int MeanTimeBetweenFindsTicks => Mathf.Clamp(
            Mathf.RoundToInt(meanTimeBetweenFindsDays * TicksPerDay),
            MinTicks,
            MaxTicks);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref meanTimeBetweenFindsDays, "meanTimeBetweenFindsDays", 0.5f);
            Scribe_Values.Look(ref maxScanTimeTicks, "maxScanTimeTicks", 45000);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                int meanTimeBetweenFindsTicks = Mathf.Clamp(
                    Mathf.RoundToInt(meanTimeBetweenFindsDays * TicksPerDay),
                    MinTicks,
                    MaxTicks);
                meanTimeBetweenFindsDays = (float)meanTimeBetweenFindsTicks / TicksPerDay;
                maxScanTimeTicks = Mathf.Clamp(maxScanTimeTicks, MinTicks, MaxTicks);
            }
        }
    }
}
