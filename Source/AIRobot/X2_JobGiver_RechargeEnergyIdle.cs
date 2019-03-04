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
    /// This is the JobGiver 'Recharge your Energy when idle'.
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>Usage of this code is free. All I ask is that you mention my name somewhere.</permission>
    public class X2_JobGiver_RechargeEnergyIdle : ThinkNode
    {
        // When the robot is idle, check if it is inside the room of the recharge station. If not, return there.
        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            Need_Rest needRest = pawn.needs.rest;
            if (needRest == null)
                return ThinkResult.NoJob;

            float curLevel = needRest.CurLevel;

            if (curLevel > 0.70f )
                return ThinkResult.NoJob;

            //double distance = AIRobot_Helper.GetDistance(pawn.Position, (pawn as X2_AIRobot).rechargeStation.Position);
            //
            //if (distance > 15f)
            //    return ThinkResult.NoJob;

            Boolean isInDistance = AIRobot_Helper.IsInDistance(pawn.Position, (pawn as X2_AIRobot).rechargeStation.Position, 15);
            if (isInDistance)
                return ThinkResult.NoJob;

            X2_AIRobot robot = pawn as X2_AIRobot;
            if (robot.DestroyedOrNull()) return ThinkResult.NoJob;
            if (!robot.Spawned) return ThinkResult.NoJob;

            X2_Building_AIRobotRechargeStation rechargeStation = robot.rechargeStation;
            if (rechargeStation.DestroyedOrNull()) return ThinkResult.NoJob;
            if (!rechargeStation.Spawned) return ThinkResult.NoJob;

            Job job = new Job(DefDatabase<JobDef>.GetNamed("AIRobot_GoRecharge"), rechargeStation);
            job.locomotionUrgency = LocomotionUrgency.Amble;

            return new ThinkResult(job, this, JobTag.SatisfyingNeeds, false);

        }

    }
}
