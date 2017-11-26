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
    public class JobDriver_GotoTarget : JobDriver
    {
        private IntVec3 targetCell;
        public IntVec3 TargetCell
        {
            get
            {
                return targetCell;
            }
            set
            {
                targetCell = value;
            }
        }


        public JobDriver_GotoTarget() { }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Go next to target
            yield return GotoThing(targetCell, PathEndMode.ClosestTouch);
            //// Go directly to target
            //yield return GotoThing(targetCell, PathEndMode.OnSquare);
        }

        public Toil GotoThing(IntVec3 cell, PathEndMode PathEndMode)
        {
            Toil toil = new Toil();
            LocalTargetInfo target = new LocalTargetInfo(cell);
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                actor.pather.StartPath(target, PathEndMode);
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            //toil.AddFinishAction(new Action(DoSleep));
            return toil;
        }

        //public Toil SleepInBed(Thing bed)
        //{
        //    Toil toil = new Toil();
        //    TargetPack target = new TargetPack();
        //    target.Loc = bed.Position;
        //    toil.initAction = () =>
        //    {
        //        Pawn actor = toil.actor;
        //        actor.pather.StartPath(target, PathEndMode.OnSquare);
        //    };
        //    toil.defaultCompleteMode = ToilCompleteMode.FinishedBusy;
        //    //toil.AddFinishAction(new Action(SleepAtPosition));
        //    return toil;
        //}


        //public void DoSleep()
        //{
        //    JobGiver_GetRest getRest = new JobGiver_GetRest();
        //    JobPackage jobPackage = getRest.TryIssueJobPackage(pawn);
        //    pawn.jobs.StartJob(jobPackage.job);
        //}



    }
}
