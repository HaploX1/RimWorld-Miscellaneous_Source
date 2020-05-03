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
    public class X2_JobDriver_RepairStationRobot : JobDriver
    {
        //This jobdriver takes the robot and the resources and activates the station

        public const TargetIndex StationIndex = TargetIndex.A;
        public const TargetIndex IngredientIndex = TargetIndex.B;
        public const TargetIndex IngredientPlaceCellIndex = TargetIndex.C;
        public const PathEndMode GotoIngredientPathEndMode = PathEndMode.ClosestTouch;
        

        private List<Thing> ingredients = new List<Thing>();

        public X2_JobDriver_RepairStationRobot() {  }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(IngredientIndex), this.job);
            bool doneA = pawn.Reserve(job.targetA, job, 1, -1, null);
            bool doneC = pawn.Reserve(job.targetC, job, 1, -1, null);
            //Log.Error("Reserving is done: " + doneA.ToString() + " " + doneC.ToString());
            return doneA & doneC;
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
                X2_Building_AIRobotRechargeStation workbench = job.GetTarget(StationIndex).Thing as X2_Building_AIRobotRechargeStation;

                //conditions only apply during the billgiver-use phase
                if (workbench == null)
                    return true;
                return false;
            });

            //Go to the recharge station, this is yielded later, but needed here!
            Toil gotoStation = Toils_Goto.GotoThing(StationIndex, PathEndMode.Touch);

            //Jump over ingredient gathering if there are no ingredients needed 
            yield return Toils_Jump.JumpIf(gotoStation, () => this.job.GetTargetQueue(IngredientIndex).NullOrEmpty());

            {
                //Get to ingredient and pick it up
                //Note that these fail cases must be on these toils, otherwise the recipe work fails if you stacked
                //   your targetB into another object on the bill giver square.

                Toil extract = Toils_JobTransforms.ExtractNextTargetFromQueue(IngredientIndex);
                yield return extract;
                
                //Note that these fail cases must be on these toils, otherwise the recipe work fails if you stacked
                //   your targetB into another object on the bill giver square.
                var getToHaulTarget2 = Toils_Goto.GotoThing(IngredientIndex, PathEndMode.Touch)
                            .FailOnDespawnedNullOrForbidden(IngredientIndex)
                            .FailOnSomeonePhysicallyInteracting(IngredientIndex);
                yield return getToHaulTarget2;
                
                //Carry ingredient to the workbench
                yield return Toils_Haul.StartCarryThing(IngredientIndex, true);

                //Jump to pick up more in this run if we're collecting from multiple stacks at once
                yield return JumpToAlsoCollectTargetInQueue(getToHaulTarget2, IngredientIndex);

                //Carry ingredient to the workbench
                yield return Toils_Goto.GotoThing(StationIndex, PathEndMode.Touch)
                                        .FailOnDestroyedOrNull(IngredientIndex);

                //Place ingredient on the appropriate cell
                Toil findPlaceTarget2 = Toils_JobTransforms.SetTargetToIngredientPlaceCell(StationIndex, IngredientIndex, IngredientPlaceCellIndex);
                yield return findPlaceTarget2;
                yield return Toils_Haul.PlaceHauledThingInCell(IngredientPlaceCellIndex, findPlaceTarget2, false, false);

                // save the ingredient, so that it can be deleted later on!
                Toil saveIngredient = new Toil();
                saveIngredient.initAction = delegate { this.ingredients.Add(GetActor().jobs.curJob.GetTarget(IngredientIndex).Thing); };
                saveIngredient.defaultCompleteMode = ToilCompleteMode.Instant;
                yield return saveIngredient;

                //Jump back if another ingredient is queued, or you didn't finish carrying your current ingredient target
                yield return Toils_Jump.JumpIfHaveTargetInQueue(IngredientIndex, extract);

                extract = null;
                getToHaulTarget2 = null;
                findPlaceTarget2 = null;
            }

            //yield return GetDebugToil("goto station", true);

            //Go to the recharge station
            yield return gotoStation;

            
            //Do the repair work
            yield return DoRepairWork(500, "Interact_ConstructMetal")
                                     .FailOnDespawnedNullOrForbiddenPlacedThings()
                                     .FailOnCannotTouch(StationIndex, PathEndMode.Touch);

            yield break;
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
                //X2_AIRobot_disabled robot = curJob.GetTarget(RobotIndex).Thing as X2_AIRobot_disabled;
                
                // vanish ...
                for (int i = this.ingredients.Count - 1; i >= 0; i--)
                {
                    Thing ingredient = ingredients[i];
                    ingredient.Destroy(DestroyMode.Vanish);
                }
                ingredients.Clear();
                ingredients = null;

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




        //public static Toil SetTargetToIngredientPlaceCell(TargetIndex targetInd, TargetIndex carryItemInd, TargetIndex cellTargetInd)
        //{
        //    Toil toil = new Toil();
        //    toil.initAction = delegate
        //    {
        //        Pawn actor = toil.actor;
        //        Job curJob = actor.jobs.curJob;
        //        Thing carryThing = curJob.GetTarget(carryItemInd).Thing;
        //        Thing targetThing = curJob.GetTarget(targetInd).Thing;
        //        IntVec3 c = FindPlaceSpotAtOrNear(targetThing.Position, targetThing.Map, carryThing);

        //        curJob.SetTarget(cellTargetInd, c);
        //    };
        //    return toil;
        //}

        public static Toil GetDebugToil(string msg, bool isError = true)
        {
            Toil toil = new Toil();
            toil.initAction = delegate
                {
                    if (isError)
                        Log.Error(msg);
                    else
                        Log.Warning(msg);
                };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private static IntVec3 FindPlaceSpotAtOrNear(IntVec3 center, Map map, Thing thing)
        {
            bool placePossible = false;
            IntVec3 bestSpot = IntVec3.Invalid;
            for (int i = 0; i < 9; i++)
            {
                IntVec3 intVec = center + GenRadial.RadialPattern[i];
                placePossible = PossiblePlacementSpot(intVec, map, center, thing);
                if (placePossible)
                {
                    bestSpot = intVec;
                    break;
                }
            }
            return bestSpot;
        }
        private static bool PossiblePlacementSpot(IntVec3 c, Map map, IntVec3 center, Thing thing)
        {
            if (!c.InBounds(map) || !c.Walkable(map))
                return false;

            List<Thing> list = map.thingGrid.ThingsListAt(c);
            for (int i = 0; i < list.Count; i++)
            {
                Thing thing2 = list[i];
                if (thing.def.saveCompressible && thing2.def.saveCompressible)
                    return false;

                if (thing.def.category == ThingCategory.Item && thing2.def.category == ThingCategory.Item && (!thing2.CanStackWith(thing) || thing2.stackCount >= thing.def.stackLimit))
                    return false;
            }

            if (!map.reachability.CanReach(center, c, PathEndMode.OnCell, TraverseMode.PassDoors, Danger.Deadly))
                return false;

            return true;
        }

    }
}
