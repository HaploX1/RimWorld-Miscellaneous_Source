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
    public class JobDriver_FillConstructor : JobDriver
    {
        private static TargetIndex IngredientInd = TargetIndex.A;
        private static TargetIndex ConstructorInd = TargetIndex.B;
        private static TargetIndex IngredientPlaceCellInd = TargetIndex.C;

        public JobDriver_FillConstructor() { }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            pawn.Map.pawnDestinationReservationManager.Reserve(pawn, job, job.targetB.Cell);

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Bill giver destroyed (only in bill using phase! Not in carry phase)
            this.AddEndCondition(() =>
            {
                var targ = this.GetActor().jobs.curJob.GetTarget(ConstructorInd).Thing;
                if (targ == null || (targ is Building && !targ.Spawned))
                    return JobCondition.Incompletable;
                return JobCondition.Ongoing;
            });
            this.FailOnBurningImmobile(ConstructorInd);  //Bill giver, or product burning in carry phase

            //Reserve the workbench and the ingredients
            yield return Toils_Reserve.Reserve(ConstructorInd);

            //yield return Toils_Logging("Reserving done.", false);  //-- DEBUG --

            //This toil is yielded later
            Toil gotoBillGiver = Toils_Goto.GotoThing(IngredientInd, PathEndMode.InteractionCell);

            //Jump over ingredient gathering if there are no ingredients needed 
            yield return Toils_Jump.JumpIf(gotoBillGiver, () => job.GetTargetQueue(IngredientInd).NullOrEmpty());

            //Gather ingredients
            {
                //Extract an ingredient into IngredientInd target
                Toil extract = Toils_JobTransforms.ExtractNextTargetFromQueue(IngredientInd);
                yield return extract;

                //Reserve the the ingredient
                yield return Toils_Reserve.Reserve(IngredientInd)
                                        .FailOnDespawnedNullOrForbidden(IngredientInd);
               
                //Get to ingredient and pick it up
                //Note that these fail cases must be on these toils, otherwise the recipe work fails if you stacked
                //   your targetB into another object on the bill giver square.
                var getToHaulTarget = Toils_Goto.GotoThing(IngredientInd, PathEndMode.Touch)
                                        .FailOnDespawnedNullOrForbidden(IngredientInd)
                                        .FailOnSomeonePhysicallyInteracting(IngredientInd);
                yield return getToHaulTarget;

                yield return Toils_Haul.StartCarryThing(IngredientInd);

                //Jump to pick up more in this run if we're collecting from multiple stacks at once
                yield return JumpToCollectNextIntoHands(getToHaulTarget, ConstructorInd);

                //Carry ingredient to the bill giver
                yield return Toils_Goto.GotoThing(ConstructorInd, PathEndMode.InteractionCell)
                                        .FailOnDestroyedOrNull(ConstructorInd);

                //Place ingredient on the appropriate cell
                Toil findPlaceTarget = Toils_JobTransforms.SetTargetToIngredientPlaceCell(ConstructorInd, IngredientInd, IngredientPlaceCellInd);
                yield return findPlaceTarget;

                yield return Toils_FillThingIntoConstructor(this.pawn);

                //yield return Toils_Haul.PlaceHauledThingInCell(IngredientPlaceCellInd,
                //                                                nextToilOnPlaceFailOrIncomplete: findPlaceTarget,
                //                                                storageMode: false);


                //Jump back if another ingredient is queued, or you didn't finish carrying your current ingredient target
                yield return Toils_Jump.JumpIfHaveTargetInQueue(IngredientInd, extract);
            }

            yield return gotoBillGiver;
            
        }

        private static Toil Toils_FillThingIntoConstructor(Pawn actor)
        {
            Thing carryThing = actor.jobs.curJob.GetTarget(IngredientInd).Thing;
            Building_AIPawnConstructionStation constructorThing = actor.jobs.curJob.GetTarget(ConstructorInd).Thing as Building_AIPawnConstructionStation;
            
            if (constructorThing == null)
                return null;

            Toil toil = new Toil();

            Action action = () =>
            {
                if (actor.carryTracker.TryDropCarriedThing(constructorThing.RefillPosition, ThingPlaceMode.Direct, out carryThing, null))
                    constructorThing.ReceiveThing(carryThing);
            };

            toil.finishActions = new List<Action>();
            toil.finishActions.Add(action);

            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = 30;

            return toil;
        }



        private static Toil JumpToCollectNextIntoHands(Toil gotoGetTargetToil, TargetIndex ind)
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
                    if (curJob.countQueue[i] <= 0)
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




        private static Toil Toils_Logging(string text, bool onlyWarning = false)
        {

            Toil toil = new Toil();
            toil.initAction = () =>
            {
                if (onlyWarning)
                    Log.Warning(text);
                else
                    Log.Error(text);
            };

            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            toil.defaultDuration = 0;

            return toil;
        }
    }
}
