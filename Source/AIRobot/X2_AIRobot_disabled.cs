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
        public X2_Building_AIRobotRechargeStation rechargeStation;

        public static string jobDefName_deconstruct = "AIRobot_DeconstructDamagedRobot";
        public static string jobDefName_repair = "AIRobot_RepairDamagedRobot";

        public override void ExposeData()
        {
            base.ExposeData();
            try
            {
                Scribe_References.Look<X2_Building_AIRobotRechargeStation>(ref rechargeStation, "rechargestation", true);
            }
            catch (Exception ex)
            {
                Log.Warning("X2_AIRobot_disabled -- Error while loading 'rechargestation':\n" + ex.Message + "\n" + ex.StackTrace);
            }
        }

        protected override void Tick()
        {
            base.Tick();
            
            //return;

            TrySpawnResources(this.Map, this.Position);

            if (!Destroyed)
                Destroy(DestroyMode.Vanish);
        }

        private void TrySpawnResources(Map oldMap, IntVec3 oldPos)
        {
            float returnMulti;

            if (this.rechargeStation != null && rechargeStation.def2 != null && rechargeStation.def2.robotRepairCosts != null)
            {
                returnMulti = 0.4f;
                foreach (ThingDefCountClass tDefCC in rechargeStation.def2.robotRepairCosts)
                {
                    int count = (int)Math.Floor(tDefCC.count * returnMulti);
                    if (count < 1)
                        continue;

                    Thing t = GenSpawn.Spawn(tDefCC.thingDef, oldPos, oldMap);
                    t.stackCount = count;
                }
            }
            else if (this.rechargeStation != null && rechargeStation.def2 != null && rechargeStation.def2.costList != null)
            {
                returnMulti = 0.2f;
                foreach (ThingDefCountClass tDefCC in rechargeStation.def2.costList)
                {
                    int count = (int)Math.Floor(tDefCC.count * returnMulti);
                    if (count < 1)
                        continue;

                    Thing t = GenSpawn.Spawn(tDefCC.thingDef, oldPos, oldMap);
                    t.stackCount = count;
                }
            }
            else
            {
                returnMulti = 0.3f;
                foreach (ThingDefCountClass tDefCC in def.costList)
                {
                    int count = (int)Math.Floor(tDefCC.count * returnMulti);
                    if (count < 1)
                        continue;

                    Thing t = GenSpawn.Spawn(tDefCC.thingDef, oldPos, oldMap);
                    t.stackCount = count;
                }
            }
        }

    }
}
