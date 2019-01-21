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


namespace TurretWeaponBase
{

    /// <summary>
    /// The JobDriver to install the weapon into the turret
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>Please check the provided license info for granted permissions.</permission>
    public class JobDriver_HaulToCellAndDoWork : JobDriver
    {
        private bool forbiddenInitially;
        //Constants
        private const TargetIndex HaulableInd = TargetIndex.A;
        private const TargetIndex CellInd = TargetIndex.B;

        public JobDriver_HaulToCellAndDoWork() { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.forbiddenInitially, "forbiddenInitially", false, false);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Pawn pawn = base.pawn;
            LocalTargetInfo target = base.job.GetTarget(TargetIndex.B);
            Job job = base.job;
            bool errorOnFailed2 = errorOnFailed;
            int result;
            if (pawn.Reserve(target, job, 1, -1, null, errorOnFailed2))
            {
                pawn = base.pawn;
                target = base.job.GetTarget(TargetIndex.A);
                job = base.job;
                errorOnFailed2 = errorOnFailed;
                result = (pawn.Reserve(target, job, 1, -1, null, errorOnFailed2) ? 1 : 0);
            }
            else
            {
                result = 0;
            }
            return (byte)result != 0;
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            if (TargetThingA != null)
                forbiddenInitially = TargetThingA.IsForbidden(pawn);
            else
                forbiddenInitially = false;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Set fail conditions
            this.FailOnDestroyedOrNull(HaulableInd);
            this.FailOnBurningImmobile(CellInd);
            //Note we only fail on forbidden if the target doesn't start that way
            //This helps haul-aside jobs on forbidden items
            if (!this.forbiddenInitially)
                this.FailOnForbidden(HaulableInd);

            //Reserve target storage cell, if it is a storage
            bool targetIsStorage = StoreUtility.GetSlotGroup(pawn.jobs.curJob.GetTarget(CellInd).Cell, Map) != null;
            if (targetIsStorage)
                yield return Toils_Reserve.Reserve(CellInd, 1);

            //Reserve thing to be stored
            Toil reserveTargetA = Toils_Reserve.Reserve(HaulableInd, 1, 1, null);
            yield return reserveTargetA;

            //yield return toilGoto;
            yield return Toils_Goto.GotoThing(HaulableInd, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(HaulableInd);

            // Start hauling to 
            yield return Toils_Haul.StartCarryThing(HaulableInd, false, false);

            if (this.job.haulOpportunisticDuplicates)
                yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveTargetA, HaulableInd, CellInd);

            Toil carryToCell = Toils_Haul.CarryHauledThingToCell(CellInd);
            yield return carryToCell;

            // start work on target
            yield return Toils_WaitWithSoundAndEffect(180, "Interact_ConstructMetal", "ConstructMetal", CellInd);

            
            yield return Toils_TryToAttachToWeaponBase(pawn, HaulableInd, CellInd);

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

        private Toil Toils_TryToAttachToWeaponBase(Pawn actor, TargetIndex thingInd, TargetIndex cellInd)
        {

            IntVec3 targetCell = pawn.jobs.curJob.GetTarget(cellInd).Cell;
            Thing weapon = pawn.jobs.curJob.GetTarget(thingInd).Thing;

            IEnumerable<Building_TurretWeaponBase> foundBuildings = Map.listerBuildings.AllBuildingsColonistOfClass<Building_TurretWeaponBase>();

            if (foundBuildings == null)
                return null;

            Building_TurretWeaponBase weaponBase = foundBuildings.Where(b => b.Position == targetCell).FirstOrDefault();

            if (weapon == null || weaponBase == null)
                return null;

            Toil toil = new Toil();
            toil.initAction = () =>
            {
                weaponBase.TryToInstallWeapon(weapon);

                // TEST Enabled -> Look at target, install weapon
                //IntVec3 lookAtCell = PawnRotator.RotFromAngleBiased((actor.Position - targetCell).AngleFlat).FacingCell;
                if (actor.carryTracker.TryDropCarriedThing(targetCell, ThingPlaceMode.Direct, out weapon))
                {
                    weapon.DeSpawn();
                }
            };

            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            toil.defaultDuration = 0;

            return toil;
        }

    }

}
