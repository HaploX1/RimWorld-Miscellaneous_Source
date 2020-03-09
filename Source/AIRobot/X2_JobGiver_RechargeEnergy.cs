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
    /// This is the JobGiver 'Recharge your Energy' for mai.
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>Usage of this code is free. All I ask is that you mention my name somewhere.</permission>
    public class X2_JobGiver_RechargeEnergy : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn)
        {
            if (pawn == null || pawn.needs == null)
                return 0f;

            Need_Rest needRest = pawn.needs.rest;
            if (needRest == null)
                return 0f;

            float curLevel = needRest.CurLevel;
            TimeAssignmentDef timeAssignmentDef = (pawn.timetable != null ? pawn.timetable.CurrentAssignment : TimeAssignmentDefOf.Anything);

            if (timeAssignmentDef == TimeAssignmentDefOf.Anything || timeAssignmentDef == TimeAssignmentDefOf.Work)
            {
                if ((pawn as X2_AIRobot) == null || (pawn as X2_AIRobot).rechargeStation == null || (pawn as X2_AIRobot).rechargeStation.Position == null)
                    return 0f;

                double distance = AIRobot_Helper.GetDistance(pawn.Position, (pawn as X2_AIRobot).rechargeStation.Position);

                // Own implementation: When level < 45% && dist > 25
                bool isOutsideMaxDistance = distance > 25f;
                if (curLevel < 0.40f && pawn as X2_AIRobot != null && isOutsideMaxDistance)
                    return 8f;

                //// Own implementation: When level < 70% && idle
                //bool isInsideCloseCallRange = distance < 15f && distance > 0f;
                //if (curLevel < 0.70f && pawn as X2_AIRobot != null && isInsideCloseCallRange && 
                //        (pawn.CurJobDef == null || pawn.CurJobDef == JobDefOf.Wait_Wander || pawn.CurJobDef == JobDefOf.GotoWander || pawn.CurJobDef == JobDefOf.Wait))
                //    return 8f;

                if (curLevel < 0.25f)
                    return 8f;

                return 0f;
            }
            if (timeAssignmentDef == TimeAssignmentDefOf.Joy)
            {
                if (curLevel < 0.3f)
                    return 8f;

                return 0f;
            }
            if (timeAssignmentDef == TimeAssignmentDefOf.Sleep)
            {
                if (curLevel < 0.75f)
                    return 8f;

                return 0f;
            }
            return 0f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            X2_AIRobot aiRobot = pawn as X2_AIRobot;
            X2_Building_AIRobotRechargeStation rechargeStation = AIRobot_Helper.FindRechargeStationFor(aiRobot);

            if (rechargeStation == null)
                return null;

            if (aiRobot.rechargeStation != rechargeStation)
                return null;

            Job job = new Job(DefDatabase<JobDef>.GetNamed("AIRobot_GoRecharge"), rechargeStation);

            return job;
        }

    }
}
