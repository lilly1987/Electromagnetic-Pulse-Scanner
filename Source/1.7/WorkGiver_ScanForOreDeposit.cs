using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using System.Linq;

namespace ElectromagneticPulseGenerator
{
    public class WorkGiver_ScanForOreDeposit : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamed("ElectromagneticPulseGenerator"));

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            // Only colonist-owned EPGs
            // AllBuildingsColonistOfDef returns List<Building>, which is already a Thing
            return pawn.Map.listerBuildings.AllBuildingsColonistOfDef(DefDatabase<ThingDef>.GetNamed("ElectromagneticPulseGenerator"));
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            // Never skip, always check
            return false;
        }

        public override Danger MaxPathDanger(Pawn pawn)
        {
            // Allow pawns to risk deadly danger if needed
            return Danger.Deadly;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var epg = t as Building_ElectromagneticPulseGenerator;
            if (epg == null || epg.PowerComp == null || !epg.PowerComp.PowerOn)
                return false;
            if (epg.UnrevealedDeposits.Count == 0)
                return false;
            if (t.IsForbidden(pawn))
                return false;
            if (!pawn.CanReserve(t, 1, -1, null, forced))
                return false;
            if (t.Faction != pawn.Faction)
                return false;
            if (t.IsBurning())
                return false;
            if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Research))
                return false;
            // Optionally: check for a CanUseNow property on a Comp if you add one
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("ScanForOreDeposit"), t);
        }
    }
}
