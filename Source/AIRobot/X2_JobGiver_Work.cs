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
            TimeAssignmentDef timeAssignmentDef = (pawn.timetable != null) ? pawn.timetable.CurrentAssignment : TimeAssignmentDefOf.Anything;
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
                if (!PawnCanUseWorkGiver(pawn, workGiver))
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
                            IEnumerable<Thing> enumerable = scanner.PotentialWorkThingsGlobal(pawn);
                            Thing thing;
                            try
                            {
                                if (scanner.Prioritized)
                                {
                                    IEnumerable<Thing> enumerable2 = enumerable;
                                    if (enumerable2 == null)
                                    {
                                        enumerable2 = pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
                                    }
                                    thing = ((!scanner.AllowUnreachable) ? GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, enumerable2, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, validator, (Thing x) => scanner.GetPriority(pawn, x)) : GenClosest.ClosestThing_Global(pawn.Position, enumerable2, 99999f, validator, (Thing x) => scanner.GetPriority(pawn, x)));
                                }
                                else if (scanner.AllowUnreachable)
                                {
                                    IEnumerable<Thing> enumerable3 = enumerable;
                                    if (enumerable3 == null)
                                    {
                                        enumerable3 = pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
                                    }
                                    thing = GenClosest.ClosestThing_Global(pawn.Position, enumerable3, 99999f, validator);
                                }
                                else
                                {
                                    thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, validator, enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch, enumerable != null);
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

                        if (scanner.def.scanCells)
                        {
                            pawnPosition = pawn.Position;
                            closestDistSquared = 99999f;
                            bestPriority = float.MinValue;
                            prioritized = scanner.Prioritized;
                            allowUnreachable = scanner.AllowUnreachable;
                            maxPathDanger = scanner.MaxPathDanger(pawn);
                            IEnumerable<IntVec3> enumerable4 = scanner.PotentialWorkCellsGlobal(pawn);
                            IList<IntVec3> list2;
                            if ((list2 = (enumerable4 as IList<IntVec3>)) != null)
                            {
                                for (int k = 0; k < list2.Count; k++)
                                {
                                    ProcessCell(list2[k]);
                                }
                            }
                            else
                            {
                                foreach (IntVec3 item in enumerable4)
                                {
                                    ProcessCell(item);
                                }
                            }
                        }
                    }
                    void ProcessCell(IntVec3 c)
                    {
                        bool flag = false;
                        float num2 = (c - pawnPosition).LengthHorizontalSquared;
                        float num3 = 0f;
                        if (prioritized)
                        {
                            if (!c.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, c))
                            {
                                if (!allowUnreachable && !pawn.CanReach(c, scanner.PathEndMode, maxPathDanger))
                                {
                                    return;
                                }
                                num3 = scanner.GetPriority(pawn, c);
                                if (num3 > bestPriority || (num3 == bestPriority && num2 < closestDistSquared))
                                {
                                    flag = true;
                                }
                            }
                        }
                        else if (num2 < closestDistSquared && !c.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, c))
                        {
                            if (!allowUnreachable && !pawn.CanReach(c, scanner.PathEndMode, maxPathDanger))
                            {
                                return;
                            }
                            flag = true;
                        }
                        if (flag)
                        {
                            bestTargetOfLastPriority = new TargetInfo(c, pawn.Map);
                            scannerWhoProvidedTarget = scanner;
                            closestDistSquared = num2;
                            bestPriority = num3;
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

        private bool PawnCanUseWorkGiver(Pawn pawn, WorkGiver giver)
        {
            try
            {
                return !pawn.DestroyedOrNull() && pawn.Spawned && giver.MissingRequiredCapacity(pawn) == null && !giver.ShouldSkip(pawn);
            }
            catch
            {
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