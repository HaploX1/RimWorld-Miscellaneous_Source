using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace AIPawn
{
    public class JobDriver_SedatePawn : JobDriver
    {
        //Shortcut properties
        public Pawn Takee { get { return this.job.targetA.Thing as Pawn; } }

        public JobDriver_SedatePawn() { }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Reserve the takee
            //yield return Toils_Reserve.Reserve(TargetIndex.A, ReservationType.Total);

            //Goto takee
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                                       .FailOnDestroyedNullOrForbidden(TargetIndex.A)
                                       .FailOn(() => this.job.def == JobDefOf.Arrest && !Takee.CanBeArrestedBy(this.pawn));//Abandon arrest if takee is not of a team who is willing to be arrested

            //Apply Anesthetic
            Toil makeUnconscious = new Toil();
            makeUnconscious.initAction = () =>
            {
                Takee.health.forceIncap = true;
                Takee.health.AddHediff(HediffDefOf.Anesthetic, null, null);
                Takee.health.forceIncap = false;
            };
            yield return makeUnconscious;

        }

    }
}
