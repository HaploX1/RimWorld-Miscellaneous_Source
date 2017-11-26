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
    public class X2_ThinkNode_Conditional_DoWork : ThinkNode_Conditional
    {
        WorkTypeDef workType;
        //float priority = 1f;

        public override float GetPriority(Pawn pawn)
        {
            return priority;
        }

        protected override bool Satisfied(Pawn pawn)
        {
            if (workType == null)
                return false;

            X2_AIRobot robot = pawn as X2_AIRobot;
            if (robot == null)
                return false;

            X2_ThingDef_AIRobot robotdef = robot.def as X2_ThingDef_AIRobot;
            if (robotdef == null)
                return false;

            return robot.CanDoWorkType(workType);
        }
        
        public override ThinkNode DeepCopy(bool resolve = true)
        {
            X2_ThinkNode_Conditional_DoWork thinkNode = (X2_ThinkNode_Conditional_DoWork)base.DeepCopy(resolve);
            thinkNode.workType = this.workType;
            return thinkNode;
        }

    }
}
