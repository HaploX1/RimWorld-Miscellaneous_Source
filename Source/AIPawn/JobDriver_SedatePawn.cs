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
        public Pawn Takee { get { return CurJob.targetA.Thing as Pawn; } }

        public JobDriver_SedatePawn() { }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Reserve the takee
            //yield return Toils_Reserve.Reserve(TargetIndex.A, ReservationType.Total);

            //Goto takee
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                                       .FailOnDestroyedNullOrForbidden(TargetIndex.A)
                                       .FailOn(() => CurJob.def == JobDefOf.Arrest && !Takee.CanBeArrested());//Abandon arrest if takee is not of a team who is willing to be arrested

            //Apply Anesthetic
            Toil makeUnconscious = new Toil();
            makeUnconscious.initAction = () =>
            {
                //Log.Error("Applying Anesthetic");
                //Takee.healthTracker.ApplyAnesthetic(); Does not work with AIPawn

                Takee.health.forceIncap = true;
                Takee.health.AddHediff(HediffDefOf.Anesthetic, null, null);
                Takee.health.forceIncap = false;
            };
            yield return makeUnconscious;

        }

    }
}
