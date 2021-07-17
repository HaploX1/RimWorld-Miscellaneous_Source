﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;


namespace AIRobot
{

    public class X2_JobGiver_Work_v1 : ThinkNode
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

            //if (this.emergency && pawn.mindState.priorityWork.IsPrioritized)
            //{
            //    List<WorkGiverDef> workGiversByPriority = pawn.mindState.priorityWork.WorkType.workGiversByPriority;
            //    for (int i = 0; i < workGiversByPriority.Count; i++)
            //    {
            //        WorkGiver worker = workGiversByPriority[i].Worker;
            //        Job job = this.GiverTryGiveJobPrioritized(pawn, worker, pawn.mindState.priorityWork.Cell);
            //        if (job != null)
            //        {
            //            job.playerForced = true;
            //            return new ThinkResult(job, this, new JobTag?(workGiversByPriority[i].tagToGive), false);
            //        }
            //    }
            //    pawn.mindState.priorityWork.Clear();
            //}


            List<WorkGiver> list = robot.GetWorkGivers(false); // Get Non-Emergency WorkGivers
            int num = -999;
            TargetInfo targetInfo = TargetInfo.Invalid;
            WorkGiver_Scanner workGiver_Scanner = null;
            WorkGiver_Scanner scanner;

            for (int j = 0; j < list.Count; j++)
            {
                WorkGiver workGiver = list[j];
                if (workGiver.def.priorityInType != num && targetInfo.IsValid)
                {
                    break;
                }
                if (PawnCanUseWorkGiver(pawn, workGiver))
                {
                    //Profiler.BeginSample("WorkGiver: " + workGiver.def.defName);
                    try
                    {
                        Job job2 = workGiver.NonScanJob(pawn);
                        if (job2 != null)
                        {
                            return new ThinkResult(job2, this, list[j].def.tagToGive, false);
                        }
                        scanner = (workGiver as WorkGiver_Scanner);
                        if (scanner != null)
                        {
                            IntVec3 intVec;
                            if (scanner.def.scanThings)
                            {
                                Predicate<Thing> predicate = (Thing t) => !t.IsForbidden(pawn) && scanner.HasJobOnThing(pawn, t, false);
                                IEnumerable<Thing> enumerable = scanner.PotentialWorkThingsGlobal(pawn);
                                Thing thing;
                                if (scanner.Prioritized)
                                {
                                    IEnumerable<Thing> enumerable2 = enumerable;
                                    if (enumerable2 == null)
                                    {
                                        enumerable2 = pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
                                    }
                                    if (scanner.AllowUnreachable)
                                    {
                                        intVec = pawn.Position;
                                        IEnumerable<Thing> searchSet = enumerable2;
                                        Predicate<Thing> validator = predicate;
                                        try
                                        {
                                            thing = GenClosest.ClosestThing_Global(intVec, searchSet, 99999f, validator, (Thing x) => scanner.GetPriority(pawn, x));
                                        }
                                        catch //(Exception ex)
                                        {
                                            //Log.Warning(string.Concat(new object[]
                                            //{
                                            //    pawn,
                                            //    " threw exception in WorkGiver ",
                                            //    workGiver.def.defName,
                                            //    ": ",
                                            //    ex.ToString()
                                            //}));

                                            thing = null;
                                        }
                                    }
                                    else
                                    {
                                        intVec = pawn.Position;
                                        Map map = pawn.Map;
                                        IEnumerable<Thing> searchSet = enumerable2;
                                        PathEndMode pathEndMode = scanner.PathEndMode;
                                        TraverseParms traverseParams = TraverseParms.For(pawn, scanner.MaxPathDanger(pawn), TraverseMode.ByPawn, false);
                                        Predicate<Thing> validator = predicate;
                                        try
                                        {
                                            thing = GenClosest.ClosestThing_Global_Reachable(intVec, map, searchSet, pathEndMode, traverseParams, 9999f, validator, (Thing x) => scanner.GetPriority(pawn, x));
                                        }
                                        catch //(Exception ex)
                                        {
                                            //Log.Warning(string.Concat(new object[]
                                            //{
                                            //    pawn,
                                            //    " threw exception in WorkGiver ",
                                            //    workGiver.def.defName,
                                            //    ": ",
                                            //    ex.ToString()
                                            //}));

                                            thing = null;
                                        }
                                    }
                                }
                                else if (scanner.AllowUnreachable)
                                {
                                    IEnumerable<Thing> enumerable3 = enumerable;
                                    if (enumerable3 == null)
                                    {
                                        enumerable3 = pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
                                    }
                                    intVec = pawn.Position;
                                    IEnumerable<Thing> searchSet = enumerable3;
                                    Predicate<Thing> validator = predicate;
                                    try
                                    {
                                        thing = GenClosest.ClosestThing_Global(intVec, searchSet, 99999f, validator, null);
                                    }
                                    catch //(Exception ex)
                                    {
                                        //Log.Warning(string.Concat(new object[]
                                        //{
                                        //    pawn,
                                        //    " threw exception in WorkGiver ",
                                        //    workGiver.def.defName,
                                        //    ": ",
                                        //    ex.ToString()
                                        //}));

                                        thing = null;
                                    }
                                }
                                else
                                {
                                    intVec = pawn.Position;
                                    Map map = pawn.Map;
                                    ThingRequest potentialWorkThingRequest = scanner.PotentialWorkThingRequest;
                                    PathEndMode pathEndMode = scanner.PathEndMode;
                                    TraverseParms traverseParams = TraverseParms.For(pawn, scanner.MaxPathDanger(pawn), TraverseMode.ByPawn, false);
                                    Predicate<Thing> validator = predicate;
                                    bool forceGlobalSearch = enumerable != null;
                                    try
                                    {
                                        thing = GenClosest.ClosestThingReachable(intVec, map, potentialWorkThingRequest, pathEndMode, traverseParams, 9999f, validator, enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch, forceGlobalSearch, RegionType.Set_Passable, false);
                                    }
                                    catch //(Exception ex)
                                    {
                                        //Log.Warning(string.Concat(new object[]
                                        //{
                                        //    pawn,
                                        //    " threw exception in WorkGiver ",
                                        //    workGiver.def.defName,
                                        //    ": ",
                                        //    ex.ToString()
                                        //}));

                                        thing = null;
                                    }
                                }
                                if (thing != null)
                                {
                                    targetInfo = thing;
                                    workGiver_Scanner = scanner;
                                }
                            }
                            if (scanner.def.scanCells)
                            {
                                IntVec3 position = pawn.Position;
                                float num2 = 99999f;
                                float num3 = -3.40282347E+38f;
                                bool prioritized = scanner.Prioritized;
                                bool allowUnreachable = scanner.AllowUnreachable;
                                Danger maxDanger = scanner.MaxPathDanger(pawn);
                                foreach (IntVec3 item in scanner.PotentialWorkCellsGlobal(pawn))
                                {
                                    bool flag = false;
                                    intVec = item - position;
                                    float num4 = (float)intVec.LengthHorizontalSquared;
                                    float num5 = 0f;
                                    if (prioritized)
                                    {
                                        if (!item.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, item, false))
                                        {
                                            if (!allowUnreachable && !pawn.CanReach(item, scanner.PathEndMode, maxDanger, false, false, TraverseMode.ByPawn))
                                            {
                                                continue;
                                            }
                                            num5 = scanner.GetPriority(pawn, item);
                                            if (num5 > num3 || (num5 == num3 && num4 < num2))
                                            {
                                                flag = true;
                                            }
                                        }
                                    }
                                    else if (num4 < num2 && !item.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, item, false))
                                    {
                                        if (!allowUnreachable && !pawn.CanReach(item, scanner.PathEndMode, maxDanger, false, false, TraverseMode.ByPawn))
                                        {
                                            continue;
                                        }
                                        flag = true;
                                    }
                                    if (flag)
                                    {
                                        targetInfo = new TargetInfo(item, pawn.Map, false);
                                        workGiver_Scanner = scanner;
                                        num2 = num4;
                                        num3 = num5;
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
                        //Profiler.EndSample();
                    }
                    if (targetInfo.IsValid)
                    {
                        //Profiler.EndSample();
                        Job job3 = null;
                        try
                        {
                            //pawn.mindState.lastGivenWorkType = workGiver.def.workType;
                            job3 = (!targetInfo.HasThing) ? workGiver_Scanner.JobOnCell(pawn, targetInfo.Cell, false) : workGiver_Scanner.JobOnThing(pawn, targetInfo.Thing, false);
                        }
                        catch //(Exception ex)
                        {
                            //Log.Warning(string.Concat(new object[]
                            //{
                            //    pawn,
                            //    " threw exception in WorkGiver ",
                            //    workGiver.def.defName,
                            //    " in JobOnX: ",
                            //    ex.ToString()
                            //}));

                            // Error --> Force NoJob
                            return ThinkResult.NoJob;
                        }
                        if (job3 != null)
                        {
                            return new ThinkResult(job3, this, list[j].def.tagToGive, false);
                        }
                        Log.ErrorOnce(workGiver_Scanner + " provided target " + targetInfo + " but yielded no actual job for pawn " + pawn + ". The CanGiveJob and JobOnX methods may not be synchronized.", 6112651);

                        
                    }
                    num = workGiver.def.priorityInType;
                }
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