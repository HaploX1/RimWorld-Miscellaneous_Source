using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;


namespace AIRobot
{

    public class X2_JobGiver_Work : ThinkNode
    {

        public override float GetPriority(Pawn pawn)
        {
            //if (pawn.workSettings == null || !pawn.workSettings.EverWork)
            //{
            //    return 0f;
            //}
            TimeAssignmentDef timeAssignmentDef = (pawn.timetable == null) ? TimeAssignmentDefOf.Anything : pawn.timetable.CurrentAssignment;
            if (timeAssignmentDef == TimeAssignmentDefOf.Anything)
            {
                return 5.5f;
            }
            if (timeAssignmentDef == TimeAssignmentDefOf.Work)
            {
                return 9f;
            }
            if (timeAssignmentDef == TimeAssignmentDefOf.Sleep)
            {
                return 3f;
            }
            if (timeAssignmentDef == TimeAssignmentDefOf.Joy)
            {
                return 2f;
            }
            if (timeAssignmentDef == TimeAssignmentDefOf.Meditate)
            {
                return 2f;
            }
            throw new NotImplementedException();
        }
        
        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {

            X2_AIRobot robot = pawn as X2_AIRobot;
            if (robot == null || !robot.Spawned || robot.Destroyed || robot.GetWorkGivers(false) == null)
                return ThinkResult.NoJob;

            //Profiler.BeginSample("JobGiver_Work");

            //if (emergency && pawn.mindState.priorityWork.IsPrioritized)
            //{
            //	List<WorkGiverDef> workGiversByPriority = pawn.mindState.priorityWork.WorkGiver.workType.workGiversByPriority;
            //	for (int i = 0; i < workGiversByPriority.Count; i++)
            //	{
            //		WorkGiver worker = workGiversByPriority[i].Worker;
            //		if (WorkGiversRelated(pawn.mindState.priorityWork.WorkGiver, worker.def))
            //		{
            //			Job job = GiverTryGiveJobPrioritized(pawn, worker, pawn.mindState.priorityWork.Cell);
            //			if (job != null)
            //			{
            //				job.playerForced = true;
            //				return new ThinkResult(job, this, workGiversByPriority[i].tagToGive);
            //			}
            //		}
            //	}
            //	pawn.mindState.priorityWork.Clear();
            //}



            List<WorkGiver> list = robot.GetWorkGivers(false); // Get Non-Emergency WorkGivers
            int num = -999;
            TargetInfo bestTargetOfLastPriority = TargetInfo.Invalid;
            WorkGiver_Scanner scannerWhoProvidedTarget = null;
            WorkGiver_Scanner scanner;
            IntVec3 pawnPosition;
            bool prioritized;
            bool allowUnreachable;
            Danger maxPathDanger;
            for (int j = 0; j < list.Count; j++)
            {
                WorkGiver workGiver = list[j];
                if (workGiver.def.priorityInType != num && bestTargetOfLastPriority.IsValid)
                {
                    break;
                }
                if (!PawnCanUseWorkGiver(ref pawn, workGiver))
                {
                    continue;
                }
                try
                {
                    Job job2 = workGiver.NonScanJob(pawn);
                    if (job2 != null)
                    {
                        return new ThinkResult(job2, this, list[j].def.tagToGive);
                    }
                    scanner = (workGiver as WorkGiver_Scanner);
                    float closestDistSquared;
                    float bestPriority;
                    if (scanner != null)
                    {
                        if (scanner.def.scanThings)
                        {
                            Predicate<Thing> validator = (Thing t) => !t.IsForbidden(pawn) && scanner.HasJobOnThing(pawn, t);
                            IEnumerable<Thing> potentialWorkThings = scanner.PotentialWorkThingsGlobal(pawn);
                            Thing thing;
                            try
                            {
                                if (scanner.Prioritized)
                                {
                                    IEnumerable<Thing> potentialWorkThings2 = potentialWorkThings;
                                    if (potentialWorkThings2 == null)
                                    {
                                        potentialWorkThings2 = pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
                                    }
                                    thing = ((!scanner.AllowUnreachable) ? GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, potentialWorkThings2, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, validator, (Thing x) => scanner.GetPriority(pawn, x)) : GenClosest.ClosestThing_Global(pawn.Position, potentialWorkThings2, 99999f, validator, (Thing x) => scanner.GetPriority(pawn, x)));
                                }
                                else if (scanner.AllowUnreachable)
                                {
                                    IEnumerable<Thing> potentialWorkThings3 = potentialWorkThings;
                                    if (potentialWorkThings3 == null)
                                    {
                                        potentialWorkThings3 = pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
                                    }
                                    thing = GenClosest.ClosestThing_Global(pawn.Position, potentialWorkThings3, 99999f, validator);
                                }
                                else
                                {
                                    thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, validator, potentialWorkThings, 0, scanner.MaxRegionsToScanBeforeGlobalSearch, potentialWorkThings != null);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error("Error in WorkGiver: " + ex.Message);
                                thing = null;
                            }
                            if (thing != null)
                            {
                                bestTargetOfLastPriority = thing;
                                scannerWhoProvidedTarget = scanner;
                            }
                        }

                        int maxCheck = 200; //75; // check max work types for possible work

                        if (scanner.def.scanCells)
                        {
                            pawnPosition = pawn.Position;
                            closestDistSquared = 99999f;
                            bestPriority = float.MinValue;
                            prioritized = scanner.Prioritized;
                            allowUnreachable = scanner.AllowUnreachable;
                            maxPathDanger = scanner.MaxPathDanger(pawn);
                            IEnumerable<IntVec3> allWork4Pawn = scanner.PotentialWorkCellsGlobal(pawn);
                            IList<IntVec3> currWork4Pawn;

                            if ((currWork4Pawn = (allWork4Pawn as IList<IntVec3>)) != null)
                            {
                                for (int k = 0; k < currWork4Pawn.Count; k++)
                                {
                                    ProcessCell(currWork4Pawn[k], ref pawn, ref scanner, ref pawnPosition, prioritized, allowUnreachable, maxPathDanger,
                                        ref bestTargetOfLastPriority, ref scannerWhoProvidedTarget, ref closestDistSquared, ref bestPriority);

                                    if (bestTargetOfLastPriority != TargetInfo.Invalid)
                                        break;
                                    else if (scanner.ToString() != "RimWorld.WorkGiver_GrowerSow")
                                        maxCheck--;
                                    if (maxCheck <= 0)
                                        break;
                                }
                            }
                            else
                            {
                                foreach (IntVec3 item in allWork4Pawn)
                                {
                                    ProcessCell(item, ref pawn, ref scanner, ref pawnPosition, prioritized, allowUnreachable, maxPathDanger,
                                        ref bestTargetOfLastPriority, ref scannerWhoProvidedTarget, ref closestDistSquared, ref bestPriority);

                                    if (bestTargetOfLastPriority != TargetInfo.Invalid)
                                        break;
                                    else if (scanner.ToString() != "RimWorld.WorkGiver_GrowerSow")
                                        maxCheck--;
                                    if (maxCheck <= 0)
                                        break;
                                }
                            }

                        }
                    }

                }
                catch (Exception ex)
                {
                    Log.Error(pawn + " threw exception in WorkGiver " + workGiver.def.defName + ": " + ex.ToString());
                }
                finally
                {
                }
                if (bestTargetOfLastPriority.IsValid)
                {
                    Job job3 = (!bestTargetOfLastPriority.HasThing) ? scannerWhoProvidedTarget.JobOnCell(pawn, bestTargetOfLastPriority.Cell) : scannerWhoProvidedTarget.JobOnThing(pawn, bestTargetOfLastPriority.Thing);
                    if (job3 != null)
                    {
                        job3.workGiverDef = scannerWhoProvidedTarget.def;
                        return new ThinkResult(job3, this, list[j].def.tagToGive);
                    }
                    Log.ErrorOnce(scannerWhoProvidedTarget + " provided target " + bestTargetOfLastPriority + " but yielded no actual job for pawn " + pawn + ". The CanGiveJob and JobOnX methods may not be synchronized.", 6112651);
                }
                num = workGiver.def.priorityInType;
            }
            return ThinkResult.NoJob;
        }

        // 1.4: used ref for 'pawn' and 'scanner' to prevent unneeded copying
        private void ProcessCell(IntVec3 c, ref Pawn pawn, ref WorkGiver_Scanner scanner, ref IntVec3 pawnPosition, bool prioritized, bool allowUnreachable, Danger maxPathDanger,
                                    ref TargetInfo bestTargetOfLastPriority, ref WorkGiver_Scanner scannerWhoProvidedTarget, 
                                    ref float closestDistSquared, ref float bestPriority)
        {
            bool found = false;
            float distSquared = (c - pawnPosition).LengthHorizontalSquared;
            float priority = 0f;
            if (prioritized)
            {
                if (!c.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, c))
                {
                    if (!allowUnreachable && !pawn.CanReach(c, scanner.PathEndMode, maxPathDanger))
                    {
                        return;
                    }
                    priority = scanner.GetPriority(pawn, c);
                    if (priority > bestPriority || (priority == bestPriority && distSquared < closestDistSquared))
                    {
                        found = true;
                    }
                }
            }
            else if (distSquared < closestDistSquared && !c.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, c))
            {
                if (!allowUnreachable && !pawn.CanReach(c, scanner.PathEndMode, maxPathDanger))
                {
                    return;
                }
                found = true;
            }
            if (found)
            {
                bestTargetOfLastPriority = new TargetInfo(c, pawn.Map);
                scannerWhoProvidedTarget = scanner;
                closestDistSquared = distSquared;
                bestPriority = priority;
            }
        }

        private bool PawnCanUseWorkGiver(ref Pawn pawn, WorkGiver giver)
        {
            try
            {
                return !pawn.DestroyedOrNull() && pawn.Spawned && giver.MissingRequiredCapacity(pawn) == null && !giver.ShouldSkip(pawn);
            }
            catch (Exception ex)
            {
                Log.Warning("Robot caught error in PawnCanUseWorkGiver: Robot " + pawn.def.defName + " on WorkGiver '" + giver.def.defName + "', this will be ignored..\n" + ex.ToString());
                // Catch errors from WorkGivers not recognising the robot as a valid pawn (For example because it doesn't use the worksettings?)
                return false;
            }
        }

        //private Job GiverTryGiveJobPrioritized(Pawn pawn, WorkGiver giver, IntVec3 cell)
        //{
        //    if (!this.PawnCanUseWorkGiver(pawn, giver))
        //    {
        //        return null;
        //    }
        //    try
        //    {
        //        Job job = giver.NonScanJob(pawn);
        //        if (job != null)
        //        {
        //            Job result = job;
        //            return result;
        //        }
        //        WorkGiver_Scanner scanner = giver as WorkGiver_Scanner;
        //        if (scanner != null)
        //        {
        //            if (giver.def.scanThings)
        //            {
        //                Predicate<Thing> predicate = (Thing t) => !t.IsForbidden(pawn) && scanner.HasJobOnThing(pawn, t, false);
        //                List<Thing> thingList = cell.GetThingList(pawn.Map);
        //                for (int i = 0; i < thingList.Count; i++)
        //                {
        //                    Thing thing = thingList[i];
        //                    if (scanner.PotentialWorkThingRequest.Accepts(thing) && predicate(thing))
        //                    {
        //                        pawn.mindState.lastGivenWorkType = giver.def.workType;
        //                        Job result = scanner.JobOnThing(pawn, thing, false);
        //                        return result;
        //                    }
        //                }
        //            }
        //            if (giver.def.scanCells && !cell.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, cell))
        //            {
        //                pawn.mindState.lastGivenWorkType = giver.def.workType;
        //                Job result = scanner.JobOnCell(pawn, cell);
        //                return result;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(string.Concat(new object[]
        //        {
        //            pawn,
        //            " threw exception in GiverTryGiveJobTargeted on WorkGiver ",
        //            giver.def.defName,
        //            ": ",
        //            ex.ToString()
        //        }));
        //    }
        //    return null;
        //}
    }
}