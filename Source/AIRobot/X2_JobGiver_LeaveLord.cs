using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace AIRobot
{

    public class X2_JobGiver_LeaveLord : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn)
        {
            if (pawn.GetLord() != null)
                return 9f;

            return 0f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            Lord lord = pawn.GetLord();
            if (lord != null)
                lord.Notify_PawnLost(pawn, PawnLostCondition.LeftVoluntarily);

            return null;
        }
    }
}