using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace AIRobot
{
    /// <summary>
    /// This is a helper class.
    /// Here will some general functions
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>For usage of this code, please look at the license information.</permission>
    [StaticConstructorOnStartup]
    public class AIRobot_Helper
    {
        public static X2_Building_AIRobotRechargeStation FindRechargeStationFor(X2_AIRobot p)
        {
            return FindRechargeStationFor(p, p, false, false);  //FindBedFor(p, p, p.IsPrisonerOfColony, true);
        }
        public static X2_Building_AIRobotRechargeStation FindMedicalRechargeStationFor(X2_AIRobot p)
        {
            return FindRechargeStationFor(p, p, false, false, true);  //FindBedFor(p, p, p.IsPrisonerOfColony, true);
        }
        public static X2_Building_AIRobotRechargeStation FindRechargeStationFor(X2_AIRobot sleeper, X2_AIRobot traveler, bool sleeperWillBePrisoner, bool checkSocialProperness, bool medicalBedNeeded = false)
        {
            Predicate<Thing> predicate = (Thing t) =>
            {
                if (!traveler.CanReserveAndReach(t, PathEndMode.OnCell, Danger.Some, 1))
                    return false;

                X2_Building_AIRobotRechargeStation foundRechargeStation = t as X2_Building_AIRobotRechargeStation;
                if (foundRechargeStation == null)
                    return false;

                if (foundRechargeStation.robot != null && foundRechargeStation.robot != sleeper)
                    return false;

                if (foundRechargeStation.IsForbidden(traveler))
                    return false;

                if (foundRechargeStation.IsBurning())
                    return false;

                return true;
            };

            if (sleeper.rechargeStation != null && predicate(sleeper.rechargeStation))
            {
                X2_Building_AIRobotRechargeStation rStation = sleeper.rechargeStation;

                if (rStation != null)
                    return rStation;
            }

            return null;
        }

        public static void ReApplyThingToListerThings(IntVec3 cell, Thing thing)
        {
            if (cell == IntVec3.Invalid || thing == null || thing.Map == null || !thing.Spawned)
                return;

            Map map = thing.Map;

            // From ThingUtility.UpdateRegionListers(..)
            RegionGrid regionGrid = map.regionGrid;
            Region region = null;
            if (cell.InBounds(map))
            {
                region = regionGrid.GetValidRegionAt(cell);
            }
            if (region != null)
            {
                if (!region.ListerThings.Contains(thing))
                {
                    region.ListerThings.Add(thing);
                    //Log.Warning("ReAdded Robot to region.ListerThings..");
                }
            }
        }


        public static void RemoveCommUnit(X2_AIRobot pawn)
        {

            // Do not remove, if one of the following work types:
            if (pawn.workSettings.GetPriority(WorkTypeDefOf.Doctor) > 0 ||
                pawn.workSettings.GetPriority(WorkTypeDefOf.Handling) > 0 ||
                pawn.workSettings.GetPriority(WorkTypeDefOf.Warden) > 0)
            {
                return;
            }

            PawnCapacityDef activity = PawnCapacityDefOf.Talking;
            if (pawn.health.capacities.CapableOf(activity))
            {

                HediffSet hediffSet = pawn.health.hediffSet;
                IEnumerable<BodyPartRecord> notMissingParts = hediffSet.GetNotMissingParts();

                BodyPartRecord bodyPart = notMissingParts.Where(p => p.def.defName == "AIRobot_CommUnit").FirstOrDefault();

                if (bodyPart != null)
                {
                    DamageInfo damageInfo = new DamageInfo(DamageDefOf.EMP, Mathf.RoundToInt(hediffSet.GetPartHealth(bodyPart)), 0f , -1f, null, bodyPart, null, DamageInfo.SourceCategory.ThingOrUnknown, null);
                    //pawn.TakeDamage(damageInfo);
                    
                    
                    Hediff_MissingPart hediff_MissingPart = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn, null);
                    hediff_MissingPart.IsFresh = false;
                    hediff_MissingPart.lastInjury = null;
                    pawn.health.AddHediff(hediff_MissingPart, bodyPart, damageInfo);
                    pawn.health.Notify_HediffChanged(hediff_MissingPart);

                    pawn.apparel.Notify_LostBodyPart();

                }
            }

        }


        public static void UpdateBaseShieldingWhileRecharging(Pawn pawn, bool inRechargeStation, string shieldingDefName )
        {
            if (!inRechargeStation)
                return;

            // Check if available, try to repair
            foreach (Apparel a in pawn.apparel.WornApparel)
                if (a.def.defName == shieldingDefName && a.HitPoints < a.MaxHitPoints * 0.95)
                    a.HitPoints += 1;
            
            // Not naked, do nothing else.
            if (pawn.apparel.WornApparelCount != 0)
                return;
            
            // Inventar empty + in Bed -> rebuild shielding
            ThingDef item = DefDatabase<ThingDef>.GetNamed(shieldingDefName);
            Apparel apparel = (Apparel)ThingMaker.MakeThing(item);
            apparel.HitPoints = (int)(apparel.MaxHitPoints * 0.05);

            if (ApparelUtility.HasPartsToWear(pawn, apparel.def))
                pawn.apparel.Wear(apparel, false);
        }



        /// <summary>
        /// This is a find ingredient function for jobs.
        /// It is used by the JobDriver_RepairDamagedRobot
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="ingredientDef"></param>
        /// <param name="countNeeded"></param>
        /// <param name="ingredients"></param>
        /// <param name="ingredientsCount"></param>
        /// <returns></returns>
        public static bool GetAllNeededIngredients(Pawn pawn, ThingDef ingredientDef, int countNeeded, out List<Thing> ingredients, out List<int> ingredientsCount)
        {
           

            //ingredients = null; ingredientsCount = null;

            ingredients = new List<Thing>();
            ingredientsCount = new List<int>();

            int totalAvailable;
            List<Thing> resourcesAvailable = FindAvailableNearbyResources(ingredientDef, pawn, out totalAvailable);
            if (totalAvailable < countNeeded)
                return false;

            int remainingCount = countNeeded;

            foreach (Thing t in resourcesAvailable)
            {
                ingredients.Add(t);

                if (remainingCount - t.stackCount < 0)
                    ingredientsCount.Add(remainingCount);
                else
                    ingredientsCount.Add(t.stackCount);

                remainingCount = -t.stackCount;

                if (remainingCount <= 0)
                    return true;
            }

            return false;
        }
        public static List<Thing> FindAvailableNearbyResources(ThingDef resourceDef, Pawn pawn, out int resourcesTotalAvailable)
        {
            float searchDistanceAfterFirst = 25f;

            resourcesTotalAvailable = 0;
            List<Thing> resourcesAvailable = new List<Thing>();

            Predicate<Thing> validator = (th => th.def == resourceDef && !th.IsForbidden(pawn) && pawn.CanReserve(th));
            Thing closestThing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(resourceDef), PathEndMode.ClosestTouch,
                                                                    TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, validator);

            if (closestThing == null || !pawn.Spawned)
                return null;

            int num = Mathf.Min(closestThing.def.stackLimit, pawn.carryTracker.MaxStackSpaceEver(closestThing.def));

            resourcesAvailable.Add(closestThing);
            resourcesTotalAvailable += closestThing.stackCount;

            if (resourcesTotalAvailable < num)
            {
                foreach (Thing current in GenRadial.RadialDistinctThingsAround(closestThing.Position, closestThing.Map, searchDistanceAfterFirst, false))
                {
                    if (resourcesTotalAvailable >= num)
                    {
                        break;
                    }
                    if (current.def == closestThing.def)
                    {
                        if (GenAI.CanUseItemForWork(pawn, current))
                        {
                            resourcesAvailable.Add(current);
                            resourcesTotalAvailable += current.stackCount;
                        }
                    }
                }
            }

            // Only select as much as we need.
            if (resourcesTotalAvailable > num)
                resourcesTotalAvailable = num;

            return resourcesAvailable;
        }




        /// <summary>
        /// This function returns all possible combos of the list items (input: max. 32items!)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="initialList"></param>
        /// <param name="includeEmpty"></param>
        /// <returns>Source:
        /// http://msmvps.com/blogs/kathleen/archive/2013/12/31/algorithm-find-all-unique-combinations-in-a-list.aspx
        /// </returns>
        public static List<List<T>> GetAllCombos<T>(List<T> initialList, bool includeEmpty = false, bool includeInitList = true)
        {
            var ret = new List<List<T>>();

            // The final number of sets will be 2^N (or 2^N - 1 if skipping empty set)
            int setCount;
            if (includeEmpty)
                setCount = Convert.ToInt32(Math.Pow(2, initialList.Count()));
            else
                setCount = Convert.ToInt32(Math.Pow(2, initialList.Count())) - 1;

            // Start at 1 if you do not want the empty set
            int initValue;
            if (includeEmpty)
                initValue = 0;
            else
                initValue = 1;
            for (int mask = initValue; mask < setCount; mask++)
            {
                var nestedList = new List<T>();
                for (int j = 0; j < initialList.Count(); j++)
                {
                    // Each position in the initial list maps to a bit here
                    var pos = 1 << j;
                    if ((mask & pos) == pos)
                        nestedList.Add(initialList[j]);
                }
                ret.Add(nestedList);
            }

            // If wanted, return init list too
            if (includeInitList)
                ret.Add(initialList);

            return ret;
        }


        /// <summary>
        /// Get the distance between two points
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double GetDistance(IntVec3 p1, IntVec3 p2)
        {
            int X = Math.Abs(p1.x - p2.x);
            int Y = Math.Abs(p1.y - p2.y);
            int Z = Math.Abs(p1.z - p2.z);

            return Math.Sqrt(X * X + Y * Y + Z * Z);

        }

        /// <summary>
        /// Better use this one, as it doesn't need the calc intensive Sqrt function!
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static bool IsInDistance(IntVec3 p1, IntVec3 p2, float distance)
        {
            int X = Math.Abs(p1.x - p2.x);
            int Y = Math.Abs(p1.y - p2.y);
            int Z = Math.Abs(p1.z - p2.z);

            return ((X * X + Y * Y + Z * Z) <= distance * distance);
        }


        public static float GetSlopePoint(float X, IntVec3 cell1, IntVec3 cell2)
        {
            return GetSlopePoint(X, cell1.x, cell2.x, cell1.y, cell2.y);
        }
        public static float GetSlopePoint(float X, float X1, float X2, float Y1, float Y2)
        {
            return (Y2 - Y1) / (X2 - X1) * (X - X1) + Y1;
        }
    }
}
