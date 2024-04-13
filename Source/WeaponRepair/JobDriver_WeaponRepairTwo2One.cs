using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using UnityEngine;

namespace WeaponRepair
{
    public class JobDriver_WeaponRepairTwo2One : JobDriver
    {
        //This jobdriver takes two weapons, destroys the ingredient weapon and improves the master weapon.
        //The work is done on the assigned workbench.


        public const TargetIndex WorkbenchIndex = TargetIndex.A;
        public const TargetIndex WeaponMasterIndex = TargetIndex.B;
        public const TargetIndex WeaponIngredientIndex = TargetIndex.C;

        public const PathEndMode GotoWeaponPathEndMode = PathEndMode.ClosestTouch;

        public static float maxAllowedRepair = 0.95f;

        private Thing weaponMaster;
        private Thing weaponIngredient;


        public JobDriver_WeaponRepairTwo2One()
        {

        }

        //public override string GetReport()
        //{
        //    return ReportStringProcessed("WeaponRepairTwo2One_JobReport".Translate());
        //}

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.job.targetA, this.job, 1, -1, null, errorOnFailed) && this.pawn.Reserve(this.job.targetB, this.job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Workbench giver destroyed (only in bill using phase! Not in carry phase)
            this.AddEndCondition(() =>
            {
                var targ = GetActor().jobs.curJob.GetTarget(WorkbenchIndex).Thing;
                if (targ == null || ( targ is Building && !targ.Spawned ))
                    return JobCondition.Incompletable;
                return JobCondition.Ongoing;
            });
            this.FailOnBurningImmobile(WorkbenchIndex);  //Workbench burning

            this.FailOn(() =>
            {
                Building_WorkTable workbench = GetActor().jobs.curJob.GetTarget(WorkbenchIndex).Thing as Building_WorkTable;

                //conditions only apply during the billgiver-use phase
                if (workbench != null)
                {
                    if (!workbench.CurrentlyUsableForBills())
                        return true;
                }

                return false;
            });

            //Reserve the workbench and the ingredients
            yield return Toils_Reserve.Reserve(WorkbenchIndex);
            yield return Toils_Reserve.Reserve(WeaponMasterIndex);
            yield return Toils_Reserve.Reserve(WeaponIngredientIndex);

            // Goto Target
            //yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);


            //Get to master weapon and pick it up
            //Note that these fail cases must be on these toils, otherwise the recipe work fails if you stacked
            //   your targetB into another object on the bill giver square.
            var getToHaulTarget = Toils_Goto.GotoThing(WeaponMasterIndex, GotoWeaponPathEndMode)
                        .FailOnDespawnedNullOrForbidden(WeaponMasterIndex)
                        .FailOnSomeonePhysicallyInteracting(WeaponMasterIndex);
            yield return getToHaulTarget;

            // save the weapon
            Toil saveWeapon = new Toil();
            saveWeapon.initAction = delegate { this.weaponMaster = GetActor().jobs.curJob.GetTarget(WeaponMasterIndex).Thing; };
            saveWeapon.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return saveWeapon;

            //Carry ingredient to the workbench
            yield return Toils_Haul.StartCarryThing(WeaponMasterIndex);

            //Carry ingredient to the workbench
            yield return Toils_Goto.GotoThing(WorkbenchIndex, PathEndMode.InteractionCell)
                                    .FailOnDestroyedOrNull(WeaponMasterIndex);

            //Place ingredient on the appropriate cell
            Toil findPlaceTarget = Toils_JobTransforms.SetTargetToIngredientPlaceCell(WorkbenchIndex, WeaponMasterIndex, WeaponMasterIndex);
            yield return findPlaceTarget;
            yield return Toils_Haul.PlaceHauledThingInCell(WeaponMasterIndex,
                                                            nextToilOnPlaceFailOrIncomplete: findPlaceTarget,
                                                            storageMode: false);

            // reset the weapon as index
            Toil setWeapon = new Toil();
            setWeapon.initAction = delegate { GetActor().jobs.curJob.SetTarget(WeaponMasterIndex, this.weaponMaster); };
            setWeapon.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return setWeapon;


            //Get to ingredient weapon and pick it up
            //Note that these fail cases must be on these toils, otherwise the recipe work fails if you stacked
            //   your targetB into another object on the bill giver square.
            var getToHaulTarget2 = Toils_Goto.GotoThing(WeaponIngredientIndex, GotoWeaponPathEndMode)
                        .FailOnDespawnedNullOrForbidden(WeaponIngredientIndex)
                        .FailOnSomeonePhysicallyInteracting(WeaponIngredientIndex);
            yield return getToHaulTarget2;

            // save the weapon
            Toil saveWeapon2 = new Toil();
            saveWeapon2.initAction = delegate { this.weaponIngredient = GetActor().jobs.curJob.GetTarget(WeaponIngredientIndex).Thing; };
            saveWeapon2.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return saveWeapon2;

            //Carry ingredient to the workbench
            yield return Toils_Haul.StartCarryThing(WeaponIngredientIndex);

            //Carry ingredient to the workbench
            yield return Toils_Goto.GotoThing(WorkbenchIndex, PathEndMode.InteractionCell)
                                    .FailOnDestroyedOrNull(WeaponIngredientIndex);

            //Place ingredient on the appropriate cell
            Toil findPlaceTarget2 = Toils_JobTransforms.SetTargetToIngredientPlaceCell(WorkbenchIndex, WeaponIngredientIndex, WeaponIngredientIndex);
            yield return findPlaceTarget2;
            yield return Toils_Haul.PlaceHauledThingInCell(WeaponIngredientIndex,
                                                            nextToilOnPlaceFailOrIncomplete: findPlaceTarget2,
                                                            storageMode: false);

            // reset the weapon as index
            Toil setWeapon2 = new Toil();
            setWeapon2.initAction = delegate { GetActor().jobs.curJob.SetTarget(WeaponIngredientIndex, this.weaponIngredient); };
            setWeapon2.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return setWeapon2;


            //Go to the workbench
            yield return Toils_Goto.GotoThing(WorkbenchIndex, PathEndMode.InteractionCell);



            //Do the repair work
            yield return DoWeaponRepairWork(500, "Interact_ConstructMetal", maxAllowedRepair)
                                     .FailOnDespawnedNullOrForbiddenPlacedThings()
                                     .FailOnCannotTouch(WorkbenchIndex, PathEndMode.InteractionCell);

            //yield return GivePawnCarryToStorageJob(WeaponMasterIndex);


        }




        public static Toil DoWeaponRepairWork(int duration, string soundDefName, float maxAllowedRepair)
        {
            float expPerSecond = 25f;
            
            FloatRange gainHitpointRange = new FloatRange(0.6f , 0.95f); // Note: result = gainHitpointRange * skillRecord.Level / 10
            SkillDef skillDef = SkillDefOf.Crafting;
            int maxSkillLevel = 10;

            Toil toil = new Toil();
            //toil.initAction = delegate
            //{

            //};
            toil.tickAction = delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                SkillRecord skillRecord = actor.skills.GetSkill(skillDef);

                // actor learns something
                actor.skills.Learn( SkillDefOf.Crafting, expPerSecond / 60 );
            };

            toil.AddFinishAction(delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;

                // Calculate possible hitpoint gain multiplicator
                SkillRecord skillRecord = actor.skills.GetSkill(skillDef);
                float gainHitpointMulti = gainHitpointRange.RandomInRange * Mathf.Clamp( skillRecord.Level, 1, maxSkillLevel) / maxSkillLevel; //20;
                if (gainHitpointMulti < 0.23f)
                    gainHitpointMulti = 0.23f;

                // Get the target hitpoint to use
                Thing master = curJob.GetTarget(WeaponMasterIndex).Thing;
                int hitpointsMasterMax = master.MaxHitPoints;
                int hitpointsMaster = master.HitPoints;
                Thing ingredient = curJob.GetTarget(WeaponIngredientIndex).Thing;
                int hitpointsIngredient = ingredient.HitPoints;

                // Hitpoints will only be increased to XX% of the ingredient hitpoints
                int newHitpoints = hitpointsMaster + (int)(hitpointsIngredient * gainHitpointMulti);

                if (newHitpoints > hitpointsMasterMax * maxAllowedRepair)
                    newHitpoints = (int)(hitpointsMasterMax * maxAllowedRepair) + 1;

                // repair master
                master.HitPoints = newHitpoints;
                // vanish ingredient
                ingredient.Destroy(DestroyMode.Vanish);
            });

            toil.handlingFacing = true;
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = duration;
            toil.WithProgressBarToilDelay(WorkbenchIndex, false, -0.5f);
            //toil.WithEffect(() => toil.actor.CurJob.bill.recipe.effectWorking, WorkbenchIndex);
            toil.PlaySustainerOrSound(() => SoundDef.Named(soundDefName));
            
            return toil;
        }

        //public static Toil GivePawnCarryToStorageJob(TargetIndex index)
        //{
        //    Toil toil = new Toil();
        //    toil.defaultCompleteMode = ToilCompleteMode.Delay;
        //    toil.defaultDuration = 20;
        //    toil.finishActions.Add(delegate
        //       {
        //           Pawn actor = toil.actor;
        //           Job curJob = actor.jobs.curJob;
        //           Thing thing = curJob.GetTarget(index).Thing;

        //           Job haulJob = HaulAIUtility.HaulToStorageJob(actor, thing);
        //           actor.jobs.jobQueue.EnqueueFirst(haulJob);
        //       }
        //    );

        //    return toil;
        //}
    }
}
