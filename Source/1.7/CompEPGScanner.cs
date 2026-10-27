using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using UnityEngine;

namespace ElectromagneticPulseGenerator
{
    public class CompEPGScanner : ThingComp
    {
        public CompProperties_EPGScanner Props => (CompProperties_EPGScanner)props;
        public int MaxScanTimeTicks => EPGMod.Settings.maxScanTimeTicks;
        private int scanTicks = 0;
        private Building_ElectromagneticPulseGenerator EPG => parent as Building_ElectromagneticPulseGenerator;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref scanTicks, "scanTicks", 0);
        }

        public float GetScanProgress()
        {
            return (float)scanTicks / MaxScanTimeTicks;
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.Spawned) return;
            // Run at a rare cadence to keep perf low
            if (!parent.IsHashIntervalTick(250)) return;
            // Only add automatic progress if powered
            var power = parent.TryGetComp<CompPowerTrader>();
            if (power != null && power.PowerOn)
            {
                AddProgress(250);
            }
        }

        public void AddProgress(int amount)
        {
            scanTicks += amount;
            // MTB logic: chance to find each call
            float mtb = EPGMod.Settings.meanTimeBetweenFindsDays * 60000f; // convert days to ticks
            if (Rand.MTBEventOccurs(mtb, 60000f, amount) || scanTicks >= MaxScanTimeTicks)
            {
                scanTicks = 0;
                EPG?.RevealRandomOreDeposit(EPG.Map);
            }
        }

        public override string CompInspectStringExtra()
        {
            var power = parent.TryGetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
            {
                return "Scanner: Unpowered";
            }
            float progress = Mathf.Clamp01((float)scanTicks / MaxScanTimeTicks);
            int remainingTicks = Mathf.Max(MaxScanTimeTicks - scanTicks, 0);
            float remainingDays = remainingTicks / 60000f;
            return $"Next guaranteed find: {progress:P0} ({remainingDays:0.0} days)";
        }
    }
}
