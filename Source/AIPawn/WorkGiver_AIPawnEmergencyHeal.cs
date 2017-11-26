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
    public class WorkGiver_AIPawnDoBill : WorkGiver_DoBill
    {

        public WorkGiver_AIPawnDoBill(WorkGiverDef giverDef)
            : base(giverDef)
        { }


        private Dictionary<Thing, int> chosenIngThings = new Dictionary<Thing, int>();


        public override Job StartingJobForOn(Pawn pawn, Thing thing)
        {
            Job job;
            bool flag;
            BillGiver billGiver = thing as BillGiver;
            Pawn pawn1 = thing as Pawn;

            if (pawn1 == null)
                return null;

            Log.Error("1 - " + pawn.Name + " / " + thing.def.defName);

            if (billGiver == null)
            {
                return null;
            }
            if (thing.def == this.def.billGiverDef)
            {
                flag = true;
            }
            else
            {
                flag = (!this.def.billGiversAllMechanoids || pawn1 == null ? false : pawn1.RaceProps.mechanoid);
            }
            if (!flag)
            {
                return null;
            }
            if (!billGiver.ReadyToHaveBillDone())
            {
                return null;
            }
            if (!billGiver.BillStack.AnyShouldDoNow())
            {
                return null;
            }
            if (!pawn.CanReserve(thing, ReservationType.Use) || thing.IsBurning() || thing.IsForbidden(pawn.Faction))
            {
                return null;
            }
            if (!pawn.CanReach(billGiver.BillInteractionCell, PathMode.OnSquare))
            {
                return null;
            }
            IEnumerator<IntVec3> enumerator = billGiver.IngredientStackCells.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    IntVec3 current = enumerator.Current;
                    Thing thing1 = Find.ThingGrid.ThingAt(current, EntityCategory.Item);
                    if (thing1 == null)
                    {
                        continue;
                    }
                    job = GenHaulAI.HaulAsideJobFor(pawn, thing1);
                    return job;
                }
                goto Label0;
            }
            finally
            {
                if (enumerator == null)
                {
                }
                enumerator.Dispose();
            }
            return job;
        Label0:
            Bill bill = null;
            IEnumerator<Bill> enumerator1 = billGiver.BillStack.GetEnumerator();
            try
            {
                while (enumerator1.MoveNext())
                {
                    Bill current1 = enumerator1.Current;
                    if (current1.ShouldDoNow())
                    {
                        if (current1.recipe.PawnSatisfiesSkillRequirements(pawn))
                        {
                            this.chosenIngThings.Clear();
                            int num = 0;
                            List<IngredientAmount>.Enumerator enumerator2 = current1.recipe.ingredients.GetEnumerator();
                            try
                            {
                                while (enumerator2.MoveNext())
                                {
                                    IngredientAmount ingredientAmount = enumerator2.Current;
                                    int num1 = ingredientAmount.count;
                                    do
                                    {
                                        Predicate<Thing> predicate = (Thing t) => (t.IsForbidden(pawn.Faction) || !pawn.CanReserve(t, ReservationType.Total) || !current1.recipe.fixedIngredientFilter.Allows(t) || !current1.ingredientFilter.Allows(t) || !ingredientAmount.filter.Allows(t) || this.chosenIngThings.ContainsKey(t) ? false : (!current1.CheckIngredientsIfSociallyProper ? true : t.IsSociallyProper(pawn)));
                                        Thing thing2 = GenClosest.ClosestThingReachable(thing.Position, WorkGiver_DoBill.ThingRequestFor(ingredientAmount.filter), PathMode.ClosestTouch, RegionTraverseParameters.For(pawn, true), current1.ingredientSearchRadius, predicate, null);
                                        if (thing2 != null)
                                        {
                                            int num2 = Mathf.Min(num1, thing2.stackCount);
                                            this.chosenIngThings.Add(thing2, num2);
                                            num1 = num1 - num2;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    while (num1 > 0);
                                    if (num1 <= 0)
                                    {
                                        num++;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                            finally
                            {
                                ((IDisposable)(object)enumerator2).Dispose();
                            }
                            if (num != current1.recipe.ingredients.Count)
                            {
                                continue;
                            }
                            bill = current1;
                            break;
                        }
                    }
                }
            }
            finally
            {
                if (enumerator1 == null)
                {
                }
                enumerator1.Dispose();
            }
            if (bill == null)
            {
                return null;
            }
            billGiver.BillStack.RemoveInvalidBills();
            bool flag1 = false;
            IEnumerator<Bill> enumerator3 = billGiver.BillStack.GetEnumerator();
            try
            {
                while (enumerator3.MoveNext())
                {
                    if (enumerator3.Current != bill)
                    {
                        continue;
                    }
                    flag1 = true;
                    break;
                }
            }
            finally
            {
                if (enumerator3 == null)
                {
                }
                enumerator3.Dispose();
            }
            if (!flag1)
            {
                return null;
            }
            Job job1 = new Job(JobDefOf.DoBill, thing)
            {
                targetQueueB = new List<TargetPack>(this.chosenIngThings.Count),
                numToBring = new List<int>(this.chosenIngThings.Count)
            };
            foreach (KeyValuePair<Thing, int> chosenIngThing in this.chosenIngThings)
            {
                job1.targetQueueB.Add(chosenIngThing.Key);
                job1.numToBring.Add(chosenIngThing.Value);
            }
            job1.reportString = bill.recipe.jobString;
            job1.haulMode = HaulMode.ToCellNonStorage;
            job1.bill = bill;
            return job1;
        }


    }
}
