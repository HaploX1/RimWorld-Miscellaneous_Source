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
    public class X2_AIRobot_disabled : Building
    {
        public X2_Building_AIRobotRechargeStation rechargestation;

        public static string jobDefName_deconstruct = "AIRobot_DeconstructDamagedRobot";
        public static string jobDefName_repair = "AIRobot_RepairDamagedRobot";

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

        public override void Tick()
        {
            base.Tick();

            return;

            //float returnMulti = 0.6f;

            //if (rechargestation != null)
            //{
            //    foreach (ThingDefCountClass tDefCC in rechargestation.def2.robotRepairCosts)
            //    {
            //        Thing t = GenSpawn.Spawn(tDefCC.thingDef, this.PositionHeld, this.MapHeld);
            //        t.stackCount = (int)Math.Floor(tDefCC.count * returnMulti);
            //    }
            //}

            //if (!Destroyed)
            //    Destroy(DestroyMode.Vanish);
        }

        //public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        //{
        //    foreach (FloatMenuOption fmo in base.GetFloatMenuOptions(selPawn))
        //        yield return fmo;

        //    foreach (FloatMenuOption fmo in X2_AIRobot_disabled.GetFloatMenuOptions(selPawn, this))
        //        yield return fmo;
        //}

        //public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn, X2_AIRobot_disabled disabledRobot)
        //{

        //    if (!selPawn.CanReach(disabledRobot, PathEndMode.InteractionCell, Danger.Deadly))
        //    {
        //        yield return new FloatMenuOption("CannotUseNoPath".Translate().CapitalizeFirst(), null);
        //        yield break;
        //    }

        //    if (selPawn.skills.GetSkill(SkillDefOf.Construction).Level < AIRobot_Helper.jobRepairRobotSkillMin)
        //    {
        //        yield return new FloatMenuOption("ConstructionSkillTooLow".Translate().CapitalizeFirst() + ": " + "MinSkill".Translate() + " " + AIRobot_Helper.jobRepairRobotSkillMin.ToString(), null);
        //        yield break;
        //    }


        //    ThingDef ingredientDef = disabledRobot.def.costList[0].thingDef; // DefDatabase<ThingDef>.GetNamed(ingredientDefName);
        //    ThingDef ingredient2Def = disabledRobot.def.costList[1].thingDef; //DefDatabase<ThingDef>.GetNamed(ingredient2DefName);
        //    int availableResources; int availableResources2;
        //    AIRobot_Helper.FindAvailableNearbyResources(ingredientDef, selPawn, out availableResources);
        //    AIRobot_Helper.FindAvailableNearbyResources(ingredient2Def, selPawn, out availableResources2);

        //    bool resourcesOk = true;
        //    if (resourcesOk && availableResources < disabledRobot.def.costList[0].count) //X2_AIRobot_disabled.ingredientNeedCount)
        //    {
        //        resourcesOk = false;
        //        yield return new FloatMenuOption("AIRobot_RepairRobot".Translate().CapitalizeFirst() + ": " + "NotEnoughStoredLower".Translate() + " (" + availableResources.ToString() + " / " + disabledRobot.def.costList[0].count.ToString() + " " + ingredientDef.LabelCap + ")", null);
        //    }
        //    if (resourcesOk && availableResources2 < disabledRobot.def.costList[1].count) //X2_AIRobot_disabled.ingredient2NeedCount)
        //    {
        //        resourcesOk = false;
        //        yield return new FloatMenuOption("AIRobot_RepairRobot".Translate().CapitalizeFirst() + ": " + "NotEnoughStoredLower".Translate() + " (" + availableResources2.ToString() + " / " + disabledRobot.def.costList[1].count.ToString() + " " + ingredient2Def.LabelCap + ")", null);
        //    }
        //    if (resourcesOk)
        //    {
        //        yield return new FloatMenuOption("AIRobot_RepairRobot".Translate().CapitalizeFirst(), delegate { X2_AIRobot_disabled.StartRepairJob2(selPawn, disabledRobot); });
        //    }

        //    yield return new FloatMenuOption("AIRobot_DismantleRobot".Translate(), delegate { StartDismantleJob(selPawn, disabledRobot); });
        //}


        //private static void StartDismantleJob(Pawn pawn, X2_AIRobot_disabled robot)
        //{

        //    X2_JobDriver_GoToCellAndDeconstructDisabledRobot deconstructRobot = new X2_JobDriver_GoToCellAndDeconstructDisabledRobot();
        //    Job job = new Job(DefDatabase<JobDef>.GetNamed(X2_AIRobot_disabled.jobDefName_deconstruct), robot, robot.rechargestation);

        //    pawn.jobs.StopAll();
        //    pawn.jobs.StartJob(job);

        //    //Log.Error("Pawn.CurJob:" + pawn.CurJob.def.defName);
        //    //Log.Error("Job: "+ job.def.defName);

        //}

        //public static void StartRepairJob2(Pawn pawn, X2_AIRobot_disabled disabledRobot)
        //{
        //    List<Thing> foundIngredients, foundIngredients2;
        //    List<int> foundIngredientsCount, foundIngredients2Count;
        //    if (!AIRobot_Helper.GetAllNeededIngredients(pawn, disabledRobot.def.costList[0].thingDef, disabledRobot.def.costList[0].count, out foundIngredients, out foundIngredientsCount) ||
        //            foundIngredients == null || foundIngredients.Count == 0)
        //        return;
        //    if (!AIRobot_Helper.GetAllNeededIngredients(pawn, disabledRobot.def.costList[1].thingDef, disabledRobot.def.costList[1].count, out foundIngredients2, out foundIngredients2Count) ||
        //            foundIngredients2 == null || foundIngredients2.Count == 0)
        //        return;


        //    //Log.Error("foundIngredients="+foundIngredients.Count.ToString() + " " + "foundIngredientsCount="+foundIngredientsCount.Count.ToString());

        //    X2_JobDriver_RepairStationRobot repairRobot = new X2_JobDriver_RepairStationRobot();
        //    Job job = new Job(DefDatabase<JobDef>.GetNamed(X2_AIRobot_disabled.jobDefName_repair), disabledRobot.rechargestation, disabledRobot, disabledRobot.rechargestation.Position);

        //    job.count = 1;
        //    job.targetQueueB = new List<LocalTargetInfo>(foundIngredients.Count + foundIngredients2.Count);
        //    job.countQueue = new List<int>(foundIngredients.Count + foundIngredients2.Count);

        //    job.targetQueueB.Add(disabledRobot);
        //    job.countQueue.Add(1);

        //    for (int i = 0; i < foundIngredients.Count; i++)
        //    {
        //        job.targetQueueB.Add(foundIngredients[i]);
        //        job.countQueue.Add(foundIngredientsCount[i]);
        //    }
        //    for (int i = 0; i < foundIngredients2.Count; i++)
        //    {
        //        job.targetQueueB.Add(foundIngredients2[i]);
        //        job.countQueue.Add(foundIngredients2Count[i]);
        //    }
        //    job.haulMode = HaulMode.ToCellNonStorage;


        //    pawn.jobs.StopAll();
        //    pawn.jobs.StartJob(job);

        //    //Log.Error("Pawn.CurJob:" + pawn.CurJob.def.defName);
        //    //Log.Error("Job: "+ job.def.defName + " Ingredients: "+ foundIngredientsCount[0].ToString());

        //}





    }
}
