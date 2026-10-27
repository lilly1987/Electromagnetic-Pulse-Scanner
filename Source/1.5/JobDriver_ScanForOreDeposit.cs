using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;

namespace ElectromagneticPulseGenerator
{
    public class JobDriver_ScanForOreDeposit : JobDriver
    {
        private Building_ElectromagneticPulseGenerator EPG => (Building_ElectromagneticPulseGenerator)job.targetA.Thing;
        private CompEPGScanner ScannerComp => EPG?.GetComp<CompEPGScanner>();

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);
            // Only allow if scanner can be used (add a CanUseNow property if needed)
            this.FailOn(() => ScannerComp == null);
            this.FailOn(() => !pawn.CanReserveAndReach(job.targetA, PathEndMode.InteractionCell, Danger.Some));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            var scan = ToilMaker.MakeToil("ScanForOreDeposit");
            scan.tickAction = () =>
            {
                Pawn actor = scan.actor;
                // Small manning bonus: +1 extra per tick (on top of comp's +1)
                ScannerComp?.AddProgress(1);
                actor.skills?.Learn(SkillDefOf.Intellectual, 0.11f);
                PawnUtility.GainComfortFromCellIfPossible(actor, true);
            };
            scan.defaultCompleteMode = ToilCompleteMode.Delay;
            scan.defaultDuration = 4000; // Same as vanilla research
            scan.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            scan.activeSkill = () => SkillDefOf.Intellectual;
            scan.WithProgressBar(TargetIndex.A, () => ScannerComp != null ? ScannerComp.GetScanProgress() : 0f);
            // Optionally: add a sound or effect here
            // Remove explicit fail for hunger/rest; let AI handle interruptions
            yield return scan;
        }
    }
}
