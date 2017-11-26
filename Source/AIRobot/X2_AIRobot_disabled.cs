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
    public class X2_AIRobot_disabled : ThingWithComps
    {
        public X2_Building_AIRobotRechargeStation rechargestation;

        private string jobDefName_deconstruct = "AIRobot_DeconstructDamagedRobot";
        private string jobDefName_repair = "AIRobot_RepairDamagedRobot";
        private string ingredientDefName = "Steel";
        private int ingredientNeedCount = 25;


        public override void ExposeData()
        {
            base.ExposeData();
            try
            {
                Scribe_References.Look<X2_Building_AIRobotRechargeStation>(ref rechargestation, "rechargestation", true);
            }
            catch (Exception ex)
            {
                Log.Warning("X2_AIRobot_disabled -- Error while loading 'rechargestation':\n" + ex.Message + "\n" + ex.StackTrace);
            }
        }


        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption fmo in base.GetFloatMenuOptions(selPawn))
                yield return fmo;

            if (!selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption("CannotUseNoPath".Translate(), null);
                yield break;
            }


            ThingDef ingredientDef = DefDatabase<ThingDef>.GetNamed(ingredientDefName);
            int availableResources;
            AIRobot_Helper.FindAvailableNearbyResources(ingredientDef, selPawn, out availableResources);

            if (availableResources < this.ingredientNeedCount)
            {
                yield return new FloatMenuOption("NotEnoughStoredLower".Translate() + " (" + availableResources.ToString() + " / " + ingredientNeedCount.ToString() + " " + ingredientDef.LabelCap + ")", null);
            }
            else
            {
                //yield return new FloatMenuOption("(Not implemented) "+ "AIRobot_RepairRobot".Translate(), null);
                yield return new FloatMenuOption("AIRobot_RepairRobot".Translate() + " --- STILL NOT WORKING !!! ", delegate { StartRepairJob2(selPawn); });
            }

            yield return new FloatMenuOption("AIRobot_DismantleRobot".Translate(), delegate { StartDismantleJob(selPawn); });
        }


        private void StartDismantleJob(Pawn pawn)
        {

            X2_JobDriver_GoToCellAndDeconstructDisabledRobot deconstructRobot = new X2_JobDriver_GoToCellAndDeconstructDisabledRobot();
            Job job = new Job(DefDatabase<JobDef>.GetNamed(this.jobDefName_deconstruct), this, this.rechargestation);
        
            pawn.jobs.StopAll();
            pawn.jobs.StartJob(job);

            //Log.Error("Pawn.CurJob:" + pawn.CurJob.def.defName);
            //Log.Error("Job: "+ job.def.defName);

        }

        private void StartRepairJob(Pawn pawn)
        {
            List<Thing> foundIngredients;
            List<int> foundIngredientsCount;
            if (!AIRobot_Helper.GetAllNeededIngredients(pawn, DefDatabase<ThingDef>.GetNamed(ingredientDefName), this.ingredientNeedCount, out foundIngredients, out foundIngredientsCount) ||
                    foundIngredients == null || foundIngredients.Count == 0)
                return;

            //Log.Error("foundIngredients="+foundIngredients.Count.ToString() + " " + "foundIngredientsCount="+foundIngredientsCount.Count.ToString());

            List<LocalTargetInfo> ingredientsLTI = new List<LocalTargetInfo>();
            foreach (Thing t in foundIngredients)
                ingredientsLTI.Add(t);

            X2_JobDriver_RepairDamagedRobot repairRobot = new X2_JobDriver_RepairDamagedRobot();
            Job job = new Job(DefDatabase<JobDef>.GetNamed(this.jobDefName_repair), this.rechargestation, foundIngredients[0], this);
            job.targetQueueB = ingredientsLTI;
            job.countQueue = foundIngredientsCount;
            pawn.jobs.StopAll();
            pawn.jobs.StartJob(job);

            Log.Error("Pawn.CurJob:" + pawn.CurJob.def.defName);
            //Log.Error("Job: "+ job.def.defName + " Ingredients: "+ foundIngredientsCount[0].ToString());

        }

        private void StartRepairJob2(Pawn pawn)
        {
            List<Thing> foundIngredients;
            List<int> foundIngredientsCount;
            if (!AIRobot_Helper.GetAllNeededIngredients(pawn, DefDatabase<ThingDef>.GetNamed(ingredientDefName), this.ingredientNeedCount, out foundIngredients, out foundIngredientsCount) ||
                    foundIngredients == null || foundIngredients.Count == 0)
                return;

            //Log.Error("foundIngredients="+foundIngredients.Count.ToString() + " " + "foundIngredientsCount="+foundIngredientsCount.Count.ToString());

            List<LocalTargetInfo> ingredientsLTI = new List<LocalTargetInfo>();
            foreach (Thing t in foundIngredients)
                ingredientsLTI.Add(t);

            X2_JobDriver_RepairDamagedRobot repairRobot = new X2_JobDriver_RepairDamagedRobot();
            Job job = new Job(DefDatabase<JobDef>.GetNamed(this.jobDefName_repair), this.rechargestation, foundIngredients[0], this);
            job.targetQueueB = ingredientsLTI;
            job.countQueue = foundIngredientsCount;
            pawn.jobs.StopAll();
            pawn.jobs.StartJob(job);

            Log.Error("Pawn.CurJob:" + pawn.CurJob.def.defName);
            //Log.Error("Job: "+ job.def.defName + " Ingredients: "+ foundIngredientsCount[0].ToString());

        }
    }
}
