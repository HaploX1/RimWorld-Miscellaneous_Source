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
    public class X2_WorkGiver_AIRobot_RepairRobot : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            IEnumerable<Building> list = pawn.Map.listerBuildings.allBuildingsColonist.Where(b => b is X2_Building_AIRobotRechargeStation && !b.DestroyedOrNull() && b.Spawned );
            foreach (Building b in list)
            {
                if ((b as X2_Building_AIRobotRechargeStation).isRepairRequestActive)
                    yield return b;
            }
            yield break;
        }
        
        //public override ThingRequest PotentialWorkThingRequest
        //{
        //    get
        //    {
        //        //ThingRequest tr = new ThingRequest();
        //        //tr.singleDef = DefDatabase<ThingDef>.GetNamed(thingDefName);
        //        //ThingRequestGroup trg = new ThingRequestGroup();
        //        return ThingRequest.ForUndefined();
        //        //return ThingRequest.ForGroup(ThingRequestGroup.PotentialBillGiver);
        //    }
        //}

        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.Touch;
            }
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return TryCreateJob(pawn, t, forced);
        }

        public static bool CanPawnWorkThisJob(Pawn pawn, Thing t, bool forced = false)
        {
            X2_Building_AIRobotRechargeStation t1 = (t as X2_Building_AIRobotRechargeStation);
            if (t1 == null)
                return false;

            if (!t1.isRepairRequestActive) // || t1.isRepairRequestCosts == null)
                return false;

            if (pawn.skills.GetSkill(SkillDefOf.Crafting).Level < AIRobot_Helper.jobRepairRobotSkillMin)
                return false;

            int missingThingsCount = AIRobot_Helper.GetStationRepairJobMissingThingStrings(t1.isRepairRequestCosts, pawn).Count;
            if (missingThingsCount != 0 || !pawn.CanReserveAndReach(t, PathEndMode.ClosestTouch, Danger.Some))
                return false;

            return true;
        }

        public static Job TryCreateJob(Pawn pawn, Thing t, bool forced = false)
        {
            if (!CanPawnWorkThisJob(pawn, t, forced))
                return null;

            X2_Building_AIRobotRechargeStation t1 = (t as X2_Building_AIRobotRechargeStation);
            
            return AIRobot_Helper.GetStationRepairJob(pawn, t1, t1.isRepairRequestCosts);
        }
        
        private static Boolean AreAllNeededResourcesAvailable(Dictionary<ThingDef, int> resources, Pawn pawn)
        {
            List<string> missingResources = new List<string>();
            foreach (ThingDef ingredientDef in resources.Keys)
            {
                int availableResources;
                AIRobot_Helper.FindAvailableNearbyResources(ingredientDef, pawn, out availableResources);

                if (availableResources < resources[ingredientDef])
                {
                    missingResources.Add("(" + availableResources.ToString() + " / " + resources[ingredientDef].ToString() + " " + ingredientDef.LabelCap + ")");
                }
            }

            if (missingResources.Count == 0)
                return true;
            return false;
        }

    }
}
