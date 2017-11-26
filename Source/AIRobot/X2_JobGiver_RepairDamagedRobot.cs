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

    public class X2_JobGiver_RepairDamagedRobot : ThinkNode_JobGiver
    {
        public string ingredientDefName;
        public int ingredientCount;
        public string jobDefName;

        public X2_Building_AIRobotRechargeStation rechargeStation;
        public X2_AIRobot_disabled disabledRobot;
        
        public X2_JobGiver_RepairDamagedRobot()
        {

        }

        public override float GetPriority(Pawn pawn)
        {
            if (pawn.GetLord() != null)
                return 9f;

            return 0f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            List<Thing> foundIngredients;
            List<int> foundIngredientsCount;
            if (!AIRobot_Helper.GetAllNeededIngredients(pawn, DefDatabase<ThingDef>.GetNamed(ingredientDefName), this.ingredientCount, out foundIngredients, out foundIngredientsCount) ||
                    foundIngredients == null || foundIngredients.Count == 0)
                return null;

            //Log.Error("foundIngredients="+foundIngredients.Count.ToString() + " " + "foundIngredientsCount="+foundIngredientsCount.Count.ToString());

            List<LocalTargetInfo> ingredientsLTI = new List<LocalTargetInfo>();
            foreach (Thing t in foundIngredients)
                ingredientsLTI.Add(t);

            X2_JobDriver_RepairDamagedRobot repairRobot = new X2_JobDriver_RepairDamagedRobot();
            Job job = new Job(DefDatabase<JobDef>.GetNamed(this.jobDefName), this.rechargeStation, foundIngredients[0], disabledRobot);
            job.targetQueueB = ingredientsLTI;
            job.countQueue = foundIngredientsCount;

            return job;
        }





    }
}