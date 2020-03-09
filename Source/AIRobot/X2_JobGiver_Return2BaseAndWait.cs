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
    public class X2_JobGiver_Return2BaseAndWait : X2_JobGiver_RechargeEnergy
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            X2_AIRobot aiRobot = pawn as X2_AIRobot;
            X2_Building_AIRobotRechargeStation rechargeStation = AIRobot_Helper.FindRechargeStationFor(aiRobot);

            if (rechargeStation == null)
                return null;

            if (aiRobot.rechargeStation != rechargeStation)
                return null;

            Job job = new Job(JobDefOf.Goto, rechargeStation);

            return job;
        }

    }
}
