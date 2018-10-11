using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace AIRobot
{
    public class X2_JobDriver_GoDespawning : JobDriver
    {
        public bool despawn = true;
        public int waitTicks = GenDate.TicksPerHour * 12;

        public X2_JobDriver_GoDespawning() { }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Go next to target
            //yield return GotoThing(targetCell, PathEndMode.ClosestTouch);
            //// Go directly to target
            yield return GotoThing(TargetA.Cell, Map, PathEndMode.OnCell)
                                                    .FailOnDespawnedOrNull(TargetIndex.A);

            yield return DespawnIntoContainer(despawn);

            if (!despawn)
                yield return Toil_Wait(waitTicks);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, 0, null, errorOnFailed);
        }

        public Toil Toil_Wait(int ticks)
        {
            Toil toil = new Toil();
            toil.defaultDuration = 50;
            toil.defaultCompleteMode = ToilCompleteMode.Delay;

            toil.AddFinishAction(delegate
            {
                Pawn pawn = this.pawn;
                if (!pawn.Drafted)
                {
                    pawn.jobs.jobQueue.EnqueueFirst(new Job(JobDefOf.Wait, ticks));
                }
            });

            return toil;
        }

        public Toil GotoThing(IntVec3 cell, Map map, PathEndMode PathEndMode)
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

        private Toil DespawnIntoContainer(bool doDespawn)
        {
            Toil toil = new Toil();

            toil.initAction = delegate
            {
                X2_AIRobot robot = toil.actor as X2_AIRobot;
                if (robot != null && robot.rechargeStation != null)
                {
                    if (doDespawn)
                        // Despawn active --> robot into the container
                        robot.rechargeStation.AddRobotToContainer(robot);
                    else
                        // Only recharge --> robot waits and let recharge
                        robot.rechargeStation.isRechargeActive = true;
                }
            };

            toil.defaultCompleteMode = ToilCompleteMode.Instant;

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
