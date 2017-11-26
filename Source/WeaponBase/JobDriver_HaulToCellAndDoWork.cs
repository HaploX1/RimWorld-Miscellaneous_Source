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
        //Constants
        private const TargetIndex HaulableInd = TargetIndex.A;
        private const TargetIndex CellInd = TargetIndex.B;

        public JobDriver_HaulToCellAndDoWork() { }

        public override string GetReport()
        {
            IntVec3 destCell = pawn.jobs.curJob.targetB.Cell;

            Thing hauledThing = null;
            if (pawn.carryTracker.CarriedThing != null)
                hauledThing = pawn.carryTracker.CarriedThing;
            else
                hauledThing = pawn.jobs.curJob.GetTarget(HaulableInd).Thing;

            string destName = null;
            SlotGroup destGroup = StoreUtility.GetSlotGroup(destCell, Map);
            if (destGroup != null)
                destName = destGroup.parent.SlotYielderLabel();

            string repString;
            if (destName != null)
                repString = "ReportHaulingTo".Translate(hauledThing.Label, destName);
            else
                repString = "ReportHauling".Translate(hauledThing.Label);

            if (!pawn.jobs.curJob.def.reportString.NullOrEmpty())
                repString = pawn.jobs.curJob.def.reportString;

            return repString;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Set fail conditions
            this.FailOnDestroyedNullOrForbidden(HaulableInd);
            this.FailOnBurningImmobile(CellInd);
            //Note we only fail on forbidden if the target doesn't start that way
            //This helps haul-aside jobs on forbidden items
            if (!TargetThingA.IsForbidden(pawn.Faction))
                this.FailOnForbidden(HaulableInd);


            //Reserve target storage cell, if it is a storage
            bool targetIsStorage = StoreUtility.GetSlotGroup(pawn.jobs.curJob.GetTarget(CellInd).Cell, Map) != null;
            if (targetIsStorage)
                yield return Toils_Reserve.Reserve(CellInd, 1);

            //Reserve thing to be stored
            Toil reserveTargetA = Toils_Reserve.Reserve(HaulableInd, 1);
            yield return reserveTargetA;

            // Goto object
            //Toil toilGoto = null;
            //toilGoto = Toils_Goto.GotoThing(HaulableInd, PathEndMode.ClosestTouch)
            //    .FailOn(() =>
            //    {
            //        //Note we don't fail on losing hauling designation
            //        //Because that's a special case anyway

            //        //While hauling to cell storage, ensure storage dest is still valid
            //        Pawn actor = toilGoto.actor;
            //        Job curJob = actor.jobs.curJob;
            //        if (curJob.haulMode == HaulMode.ToCellStorage)
            //        {
            //            Thing haulThing = curJob.GetTarget(HaulableInd).Thing;

            //            IntVec3 destLoc = actor.jobs.curJob.GetTarget(CellInd).Cell;
            //            if (!destLoc.IsValidStorageFor(Map, haulThing))
            //                return true;
            //        }

            //        return false;
            //    });
            //yield return toilGoto;
            yield return Toils_Goto.GotoThing(HaulableInd, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(HaulableInd);

            // Start hauling to 
            yield return Toils_Haul.StartCarryThing(HaulableInd, false, false);

            if (CurJob.haulOpportunisticDuplicates)
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
