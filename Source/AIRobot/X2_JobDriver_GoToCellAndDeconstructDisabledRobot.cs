using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI; 
using Verse.Sound;
using RimWorld; 
//using RimWorld.Planet;
//using RimWorld.SquadAI;


namespace AIRobot
{

    /// <summary>
    /// The JobDriver to install the weapon into the turret
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>Please check the provided license info for granted permissions.</permission>
    public class X2_JobDriver_GoToCellAndDeconstructDisabledRobot : JobDriver
    {
        private const int workTime = 200; 

        //Constants
        private const TargetIndex RobotDestroyedInd = TargetIndex.A;
        private const TargetIndex RechargeStationInd = TargetIndex.B;

        public X2_JobDriver_GoToCellAndDeconstructDisabledRobot() { }

        public override bool TryMakePreToilReservations()
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null) && pawn.Reserve(job.targetB, job, 1, -1, null);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Set fail conditions
            this.FailOnDestroyedNullOrForbidden(RobotDestroyedInd);
            this.FailOnBurningImmobile(RobotDestroyedInd);
            //Note we only fail on forbidden if the target doesn't start that way
            //This helps haul-aside jobs on forbidden items
            if (!TargetThingA.IsForbidden(pawn.Faction))
                this.FailOnForbidden(RobotDestroyedInd);


            //Reserve target, if it is a storage
            yield return Toils_Reserve.Reserve(RobotDestroyedInd, 1);

            if (pawn.jobs.curJob.GetTarget(RechargeStationInd).Thing != null)
            {
                //yield return toilGoto;
                yield return Toils_Goto.GotoThing(RechargeStationInd, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(RechargeStationInd);

                // start work on target
                yield return Toils_WaitWithSoundAndEffect(workTime, "Interact_ConstructMetal", "ConstructMetal", RechargeStationInd);

                yield return Toils_TryToDeconstructRechargeStation(pawn, RobotDestroyedInd);
            }


            //yield return toilGoto;
            yield return Toils_Goto.GotoThing(RobotDestroyedInd, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(RobotDestroyedInd);

            // start work on target
            yield return Toils_WaitWithSoundAndEffect(workTime, "Interact_ConstructMetal", "ConstructMetal", RobotDestroyedInd);


            yield return Toils_TryToDeconstructRobot(pawn, RobotDestroyedInd);

        }

        //Base: Toils_General.WaitWith
        private Toil Toils_WaitWithSoundAndEffect(int duration, string soundDefName, string effecterDefName, TargetIndex targetIndex)
        {
            Toil toil = new Toil();
            toil.initAction = () =>
            {
                //toil.actor.pather.StopDead();
                //toil.actor.Drawer.rotator.FaceCell(toil.actor.CurJob.GetTarget(targetIndex).Cell);

                Pawn pawn = toil.actor.CurJob.GetTarget(targetIndex).Thing as Pawn;
                if (pawn != null) // If target is a pawn force him to watch me
                    PawnUtility.ForceWait(pawn, duration, null, true);

            };
            toil.handlingFacing = true;
            toil.FailOnDespawnedOrNull(targetIndex);
            toil.FailOnCannotTouch(targetIndex, PathEndMode.Touch);

            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = duration;
            toil.WithProgressBarToilDelay(targetIndex, false, -0.5f);

            toil.PlaySustainerOrSound(() => SoundDef.Named(soundDefName));

            // Throws errors?
            //toil.WithEffect(() => EffecterDef.Named(effecterDefName), targetIndex);

            return toil;
        }

        private Toil Toils_TryToDeconstructRechargeStation(Pawn actor, TargetIndex robotInd)
        {

            X2_AIRobot_disabled target = pawn.jobs.curJob.GetTarget(robotInd).Thing as X2_AIRobot_disabled;
            
            if ( target == null )
            {
                Log.Error("Disabled robot is null!");
                return null;
            }

            Toil toil = new Toil();
            toil.initAction = () =>
            {
                if (target.rechargestation != null)
                    target.rechargestation.Destroy(DestroyMode.Deconstruct);
            };

            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            toil.defaultDuration = 0;

            return toil;
        }

        private Toil Toils_TryToDeconstructRobot(Pawn actor, TargetIndex robotInd)
        {

            Thing target = pawn.jobs.curJob.GetTarget(robotInd).Thing;

            if (target == null)
            {
                Log.Error("Thing (disabled robot) is also null!");
                return null;
            }

            Toil toil = new Toil();
            toil.initAction = () =>
            {
                target.Destroy(DestroyMode.Deconstruct);
            };

            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            toil.defaultDuration = 0;

            return toil;
        }

    }

}
