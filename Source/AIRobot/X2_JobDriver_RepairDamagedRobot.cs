using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using UnityEngine;

namespace AIRobot
{
    public class X2_JobDriver_RepairDamagedRobot : JobDriver
    {
        //This jobdriver takes the robot and the resources and activates the station
        
        public const TargetIndex StationIndex = TargetIndex.A;
        public const TargetIndex IngredientIndex = TargetIndex.B;
        public const TargetIndex RobotIndex = TargetIndex.C;
        
        private List<Thing> ingredients = new List<Thing>();

        public X2_JobDriver_RepairDamagedRobot() {  }

        public override bool TryMakePreToilReservations()
        {
            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(IngredientIndex), this.job, 1, -1, null);
            return pawn.Reserve(job.targetA, job, 1, -1, null) && pawn.Reserve(job.GetTarget(IngredientIndex), job) && pawn.Reserve(job.targetC, job, 1, 1, null);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Workbench giver destroyed (only in bill using phase! Not in carry phase)
            this.AddEndCondition(() =>
            {
                var targ = GetActor().jobs.curJob.GetTarget(StationIndex).Thing;
                if (targ == null || ( targ is Building && !targ.Spawned ))
                    return JobCondition.Incompletable;
                return JobCondition.Ongoing;
            });
            this.FailOnBurningImmobile(StationIndex);  //Rechargestation is burning

            this.FailOn(() =>
            {
                X2_Building_AIRobotRechargeStation workbench = GetActor().jobs.curJob.GetTarget(StationIndex).Thing as X2_Building_AIRobotRechargeStation;

                //conditions only apply during the billgiver-use phase
                if (workbench != null)
                {
                    return true;
                }

                return false;
            });

            //Reserve the workbench and the ingredients
            yield return Toils_Reserve.Reserve(StationIndex);
            yield return Toils_Reserve.Reserve(RobotIndex);
            yield return Toils_Reserve.ReserveQueue(IngredientIndex);

            // Goto Target
            //yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);

            //Go to the recharge station, this is returned later, but needed here!
            Toil gotoStation = Toils_Goto.GotoThing(StationIndex, PathEndMode.Touch);

            {
                //Get to robot and pick it up
                //Note that these fail cases must be on these toils, otherwise the recipe work fails if you stacked
                //   your targetB into another object on the bill giver square.
                var getToHaulTarget = Toils_Goto.GotoThing(RobotIndex, PathEndMode.Touch)
                            .FailOnDespawnedNullOrForbidden(RobotIndex)
                            .FailOnSomeonePhysicallyInteracting(RobotIndex);
                yield return getToHaulTarget;

                //// save the robot
                //Toil saveRobot = new Toil();
                //saveRobot.initAction = delegate { this.robot = GetActor().jobs.curJob.GetTarget(RobotIndex).Thing as X2_AIRobot_disabled; };
                //saveRobot.defaultCompleteMode = ToilCompleteMode.Instant;
                //yield return saveRobot;

                //Carry ingredient to the workbench
                yield return Toils_Haul.StartCarryThing(RobotIndex);

                //Carry ingredient to the workbench
                yield return Toils_Goto.GotoThing(StationIndex, PathEndMode.Touch)
                                        .FailOnDestroyedOrNull(RobotIndex);

                //Place ingredient on the appropriate cell
                Toil findPlaceTarget = Toils_JobTransforms.SetTargetToIngredientPlaceCell(StationIndex, RobotIndex, RobotIndex);
                yield return findPlaceTarget;
                yield return Toils_Haul.PlaceHauledThingInCell(RobotIndex,
                                                                nextToilOnPlaceFailOrIncomplete: findPlaceTarget,
                                                                storageMode: false);
            }

            //Jump over ingredient gathering if there are no ingredients needed 
            yield return Toils_Jump.JumpIf(gotoStation, () => this.job.GetTargetQueue(IngredientIndex).NullOrEmpty());


            {

                //Get to ingredient weapon and pick it up
                //Note that these fail cases must be on these toils, otherwise the recipe work fails if you stacked
                //   your targetB into another object on the bill giver square.

                Toil extract = Toils_JobTransforms.ExtractNextTargetFromQueue(IngredientIndex);
                yield return extract;

                var getToHaulTarget2 = Toils_Goto.GotoThing(IngredientIndex, PathEndMode.Touch)
                            .FailOnDespawnedNullOrForbidden(IngredientIndex)
                            .FailOnSomeonePhysicallyInteracting(IngredientIndex);
                yield return getToHaulTarget2;

                // save the robot
                Toil saveIngredient = new Toil();
                saveIngredient.initAction = delegate { this.ingredients.Add(GetActor().jobs.curJob.GetTarget(IngredientIndex).Thing); };
                saveIngredient.defaultCompleteMode = ToilCompleteMode.Instant;
                yield return saveIngredient;

                //Carry ingredient to the workbench
                yield return Toils_Haul.StartCarryThing(IngredientIndex, true);
                
                //Jump to pick up more in this run if we're collecting from multiple stacks at once
                yield return JumpToAlsoCollectTargetInQueue(getToHaulTarget2, IngredientIndex);

                //Carry ingredient to the workbench
                yield return Toils_Goto.GotoThing(StationIndex, PathEndMode.Touch)
                                        .FailOnDestroyedOrNull(IngredientIndex);

                //Place ingredient on the appropriate cell
                Toil findPlaceTarget2 = Toils_JobTransforms.SetTargetToIngredientPlaceCell(StationIndex, IngredientIndex, IngredientIndex);
                yield return findPlaceTarget2;
                yield return Toils_Haul.PlaceHauledThingInCell(IngredientIndex,
                                                                nextToilOnPlaceFailOrIncomplete: findPlaceTarget2,
                                                                storageMode: false);

                //// reset the weapon as index
                //Toil setWeapon2 = new Toil();
                //setWeapon2.initAction = delegate { GetActor().jobs.curJob.SetTarget(IngredientsIndex, this.weaponIngredient); };
                //setWeapon2.defaultCompleteMode = ToilCompleteMode.Instant;
                //yield return setWeapon2;

                //Jump back if another ingredient is queued, or you didn't finish carrying your current ingredient target
                yield return Toils_Jump.JumpIfHaveTargetInQueue(IngredientIndex, extract);
            }

            //Go to the recharge station
            yield return gotoStation;

            
            //Do the repair work
            yield return DoRepairWork(500, "Interact_ConstructMetal")
                                     .FailOnDespawnedNullOrForbiddenPlacedThings()
                                     .FailOnCannotTouch(StationIndex, PathEndMode.Touch);

            
        }




        public Toil DoRepairWork(int duration, string soundDefName)
        {
            float expPerSecond = 25f;
            SkillDef skillDef = SkillDefOf.Crafting;

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

                // Get the partitians
                X2_Building_AIRobotRechargeStation station = curJob.GetTarget(StationIndex).Thing as X2_Building_AIRobotRechargeStation;
                X2_AIRobot_disabled robot = curJob.GetTarget(RobotIndex).Thing as X2_AIRobot_disabled;
                
                // vanish ...
                for (int i = this.ingredients.Count - 1; i >= 0; i--)
                {
                    Thing ingredient = ingredients[i];
                    ingredient.Destroy(DestroyMode.Vanish);
                }
                ingredients.Clear();
                ingredients = null;

                robot.Destroy(DestroyMode.Vanish);

                actor.Map.resourceCounter.UpdateResourceCounts();

                // repair ...
                station.Notify_RobotRepaired();
            });

            toil.handlingFacing = true;
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = duration;
            toil.WithProgressBarToilDelay(StationIndex, false, -0.4f);
            toil.PlaySustainerOrSound(() => SoundDef.Named(soundDefName));
            
            return toil;
        }


        private static Toil JumpToAlsoCollectTargetInQueue(Toil gotoGetTargetToil, TargetIndex ind)
        {
            const float MaxDist = 8;

            Toil toil = new Toil();
            toil.initAction = () =>
            {
                Pawn actor = toil.actor;

                if (actor.carryTracker.CarriedThing == null)
                {
                    Log.Error("JumpToAlsoCollectTargetInQueue run on " + actor + " who is not carrying something.");
                    return;
                }

                //Early-out
                if (actor.carryTracker.Full)
                    return;

                Job curJob = actor.jobs.curJob;
                var targetQueue = curJob.GetTargetQueue(ind);

                if (targetQueue.NullOrEmpty())
                    return;

                //Find an item in the queue matching what you're carrying
                for (int i = 0; i < targetQueue.Count; i++)
                {
                    //Can't use item - skip
                    if (!GenAI.CanUseItemForWork(actor, targetQueue[i].Thing))
                        continue;

                    //Cannot stack with thing in hands - skip
                    if (!targetQueue[i].Thing.CanStackWith(actor.carryTracker.CarriedThing))
                        continue;

                    //Too far away - skip
                    if ((actor.Position - targetQueue[i].Thing.Position).LengthHorizontalSquared > MaxDist * MaxDist)
                        continue;

                    //Determine num in hands
                    int numInHands = (actor.carryTracker.CarriedThing == null) ? 0 : actor.carryTracker.CarriedThing.stackCount;

                    //Determine num to take
                    int numToTake = curJob.countQueue[i];
                    numToTake = Mathf.Min(numToTake, targetQueue[i].Thing.def.stackLimit - numInHands);
                    numToTake = Mathf.Min(numToTake, actor.carryTracker.AvailableStackSpace(targetQueue[i].Thing.def));

                    //Won't take any - skip
                    if (numToTake <= 0)
                        continue;

                    //Set me to go get it
                    curJob.count = numToTake;
                    curJob.SetTarget(ind, targetQueue[i].Thing);

                    //Remove the amount to take from the num to bring list
                    //Remove from queue if I'm going to take all
                    curJob.countQueue[i] -= numToTake;
                    if (curJob.countQueue[i] == 0)
                    {
                        curJob.countQueue.RemoveAt(i);
                        targetQueue.RemoveAt(i);
                    }

                    //Jump to toil
                    actor.jobs.curDriver.JumpToToil(gotoGetTargetToil);
                    return;
                }

            };

            return toil;
        }

    }
}
