using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace AIPawn
{
    /// <summary>
    /// This is a helper class.
    /// Here will some general functions
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>For usage of this code, please look at the license information.</permission>
    [StaticConstructorOnStartup]
    public class HelperAIPawn
    {
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


        /// <summary>
        /// Find a free recharge station for the pawn.
        /// (New sleeping bed usable for AIPawn only)
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Building_AIPawnRechargeStation FindRechargeStationFor(AIPawn p)
        {
            return FindRechargeStationFor(p, p, false, false);  //FindBedFor(p, p, p.IsPrisonerOfColony, true);
        }
        public static Building_AIPawnRechargeStation FindMedicalRechargeStationFor(AIPawn p)
        {
            return FindRechargeStationFor(p, p, false, false, true);  //FindBedFor(p, p, p.IsPrisonerOfColony, true);
        }
        public static Building_AIPawnRechargeStation FindRechargeStationFor(AIPawn sleeper, AIPawn traveler, bool sleeperWillBePrisoner, bool checkSocialProperness, bool forceCheckMedBed = false)
        {
            Predicate<Thing> bedValidator = (Thing t) =>
            {
                Building_AIPawnRechargeStation foundRechargeStation = t as Building_AIPawnRechargeStation;
                if (foundRechargeStation == null)
                    return false;

                if (!traveler.CanReserveAndReach(t, PathEndMode.OnCell, Danger.Some, foundRechargeStation.SleepingSlotsCount))
                    return false;

                if (!foundRechargeStation.AnyUnoccupiedSleepingSlot && (!sleeper.InBed() || sleeper.CurrentBed() != foundRechargeStation))
                {
                    foreach (Pawn owner in foundRechargeStation.OwnersForReading)
                    {
                        if (owner as AIPawn != null)
                            return false;
                    }
                }

                if (sleeperWillBePrisoner)
                {
                    if (!foundRechargeStation.ForPrisoners)
                        return false;

                    if (!foundRechargeStation.Position.IsInPrisonCell(sleeper.Map))
                        return false;
                }
                else
                {
                    if (foundRechargeStation.Faction != traveler.Faction)
                        return false;

                    if (foundRechargeStation.ForPrisoners)
                        return false;
                }
                

                if (foundRechargeStation.Medical)
                {
                    if (!HealthAIUtility.ShouldEverReceiveMedicalCareFromPlayer(sleeper))
                    {
                        return false;
                    }
                    if (!HealthAIUtility.ShouldSeekMedicalRest(sleeper))
                    {
                        return false;
                    }
                    if (!foundRechargeStation.AnyUnoccupiedSleepingSlot && (!sleeper.InBed() || sleeper.CurrentBed() != foundRechargeStation))
                    {
                        return false;
                    }
                }
                else if (foundRechargeStation.OwnersForReading.Any<Pawn>() && !foundRechargeStation.OwnersForReading.Contains(sleeper))
                {
                    
                    // The pawn in the recharge station is not an AIPawn. UnassignPawn!
                    foreach (Pawn owner in foundRechargeStation.OwnersForReading)
                    {
                        if (owner as AIPawn == null)
                        {
                            if (foundRechargeStation.OwnersForReading.Find((Pawn x) => LovePartnerRelationUtility.LovePartnerRelationExists(sleeper, x)) == null)
                            {
                                owner.ownership.UnclaimBed();
                                //foundRechargeStation.TryUnassignPawn(owner);
                                break;
                            }
                        }
                    }
                    // Now recheck if there is a free place
                    if (!foundRechargeStation.AnyUnownedSleepingSlot)
                    {
                        return false;
                    }
                }

                return (!checkSocialProperness || foundRechargeStation.IsSociallyProper(sleeper, sleeperWillBePrisoner, false)) && !foundRechargeStation.IsForbidden(traveler) && !foundRechargeStation.IsBurning();
            
            };

            if (sleeper.ownership != null && sleeper.ownership.OwnedBed != null && bedValidator(sleeper.ownership.OwnedBed))
            {
                Building_AIPawnRechargeStation rStation = sleeper.ownership.OwnedBed as Building_AIPawnRechargeStation;

                if (rStation != null)
                    return rStation;
                else
                    sleeper.ownership.UnclaimBed();
            }

            DirectPawnRelation directPawnRelation = LovePartnerRelationUtility.ExistingMostLikedLovePartnerRel(sleeper, false);
            if (directPawnRelation != null)
            {
                Building_AIPawnRechargeStation ownedBed = directPawnRelation.otherPawn.ownership.OwnedBed as Building_AIPawnRechargeStation;
                if (ownedBed != null && bedValidator(ownedBed))
                {
                    return ownedBed;
                }
            }
            for (int j = 0; j < RestUtility.AllBedDefBestToWorst.Count; j++)
            {
                ThingDef thingDef = RestUtility.AllBedDefBestToWorst[j];
                if (RestUtility.CanUseBedEver(sleeper, thingDef))
                {
                    Predicate<Thing> validator = (Thing b) => bedValidator(b) && (b as Building_AIPawnRechargeStation != null) && !((Building_AIPawnRechargeStation)b).Medical;
                    Building_AIPawnRechargeStation building_Bed2 = GenClosest.ClosestThingReachable(sleeper.Position, sleeper.Map, ThingRequest.ForDef(thingDef), PathEndMode.OnCell, TraverseParms.For(traveler, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, validator, null, 0, -1, false)
                        as Building_AIPawnRechargeStation;
                    if (building_Bed2 != null)
                    {
                        if (sleeper.ownership != null)
                        {
                            sleeper.ownership.UnclaimBed();
                        }
                        return building_Bed2;
                    }
                }
            }
            return null;

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

    }
}
