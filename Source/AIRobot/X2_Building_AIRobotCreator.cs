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
    /// This is the mai creator.
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>Usage of this code is free. All I ask is that you mention my name somewhere.</permission>
    public class X2_Building_AIRobotCreator : Building
    {
        private bool destroy = false;

        public override void Tick()
        {
            if (destroy)
            {
                Destroy(DestroyMode.Vanish);
                return;
            }

            try
            {
                CreateRobot("AIRobot_Hauler", Position, Map);
            }
            catch (Exception ex)
            {
                Log.Error("Error while creating Robot.\n" + ex.Message + "\nSTACK:\n" + ex.StackTrace);
            }

            destroy = true;
        }


        public static Thing Spawn(Thing newThing, IntVec3 loc, Map map)
        {
            return Spawn(newThing, loc, map, Rot4.South);
        }
        public static Thing Spawn(Thing newThing, IntVec3 loc, Map map, Rot4 rot)
        {
            newThing = GenSpawn.Spawn(newThing, loc, map, rot);
            newThing.Position = loc;

            return newThing;
        }

        public static X2_AIRobot CreateRobot(string pawnDefName, IntVec3 position, Map map)
        {
            return CreateRobot(pawnDefName, position, map, Faction.OfPlayer);
        }
        public static X2_AIRobot CreateRobot(string pawnDefName, IntVec3 position, Map map, Faction faction)
        {
            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamed(pawnDefName);

            PawnGenerationRequest request = new PawnGenerationRequest(
                                kind, faction, PawnGenerationContext.NonPlayer, -1, true, true, false, false, false, 
                                0f , false, false, false, false, false, false, false, false, false, 
                                0f, 0f, null, 0f, null, null, null, null, null, 
                                0f, 0f, Gender.None);

            X2_AIRobot robot = (X2_AIRobot)PawnGenerator.GeneratePawn(request);

            robot.workSettings = new X2_AIRobot_Pawn_WorkSettings(robot);

            if (robot.inventory == null)
                robot.inventory = new Pawn_InventoryTracker(robot);
            if (true) // robot.RaceProps.ToolUser
            {
                if (robot.equipment == null)
                    robot.equipment = new Pawn_EquipmentTracker(robot);
                if (robot.apparel == null)
                    robot.apparel = new Pawn_ApparelTracker(robot);
            }
            if (robot.royalty == null)
                robot.royalty = new Pawn_RoyaltyTracker(robot);

            robot.workSettings.EnableAndInitializeIfNotAlreadyInitialized();

            robot.Drawer.renderer.EnsureGraphicsInitialized();

            //// Check/update faction
            //if (robot != null)
            //{
            //    if (faction != null && (robot.Faction == null || robot.Faction != faction))
            //        robot.SetFactionDirect(faction);
            //    if (robot.Faction == null && Faction.OfPlayerSilentFail != null)
            //        robot.SetFactionDirect(Faction.OfPlayerSilentFail);
            //}

            ////DEBUG
            //if (robot.workSettings == null)
            //    Log.Error("Worksettings == null!");
            //else
            //    Log.Error("Worksettings == OK, EverWork:" + robot.workSettings.EverWork.ToString());

            //AIRobot robot = RobotGenerator.GeneratePawn(pawnDefName, faction);
            IntVec3 spawnPos = position; // GenCellFinder.RandomStandableClosewalkCellNear(position, 1);

            return (X2_AIRobot)Spawn(robot, spawnPos, map);
            
        }

    }
}
