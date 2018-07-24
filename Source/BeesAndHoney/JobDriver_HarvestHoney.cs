using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed
using Verse.AI;

namespace BeeAndHoney
{
    // Extracted from JobDriver_GatherAnimalBodyResources and JobDriver_Shear 

    public class JobDriver_HarvestHoney : JobDriver
    {

        private int duration = 700;
        protected int Duration
        {
            get
            {
                return duration;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.job.GetTarget(TargetIndex.A), this.job, 1, -1, null, errorOnFailed);
        }

        protected CompHasGatherableBodyResource GetComp(Thing thing)
        {
            return thing.TryGetComp<CompBeeHive>();
        }

        protected const TargetIndex ThingInd = TargetIndex.A;
        
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            //this.FailOnDowned(TargetIndex.A);
            //this.FailOnNotCasualInterruptible(TargetIndex.A);
            yield return Toils_Reserve.Reserve(TargetIndex.A, 1);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil toil = new Toil();
            //toil.initAction = delegate
            //{
            //    Thing thing = this.CurJob.GetTarget(TargetIndex.A).Thing;
            //    if (thing != null)
            //    {
            //        PawnUtility.ForceWait(thing, this.Duration, null);
            //    }
            //};
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = this.Duration;
            toil.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            yield return toil;

            Toil toilGather = new Toil();
            toilGather.defaultCompleteMode = ToilCompleteMode.Instant;
            toilGather.initAction = delegate
            {
                CompHasGatherableBodyResource comp = this.GetComp(this.job.targetA.Thing);
                comp.Gathered(this.pawn);
            };
            yield return toilGather;

            //yield return new Toil
            //{
            //    initAction = delegate
            //    {
            //        //this.GetComp((Pawn)((Thing)this.CurJob.GetTarget(TargetIndex.A))).Gathered(this.pawn);
            //        this.GetComp((Thing)this.CurJob.GetTarget(TargetIndex.A)).Gathered(this.pawn);
            //    }
            //};
            yield break;
        }







    }
}
