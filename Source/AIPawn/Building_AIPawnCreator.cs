using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;


namespace AIPawn
{
    /// <summary>
    /// This is the mai creator.
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>Usage of this code is free. All I ask is that you mention my name somewhere.</permission>
    public class Building_AIPawnCreator : Building
    {
        private bool init = false;
        private bool destroy = false;

        public string pawnDefName = "AIPawn";
        public Gender gender = Gender.Female;

        public override void SpawnSetup(Map map, bool respawnAfterLoad)
        {
        //    LongEventHandler.ExecuteWhenFinished(SpawnSetup_Part2);
        //}

        //public void SpawnSetup_Part2()
        //{ 
            base.SpawnSetup(map, respawnAfterLoad);
            init = true;
        }


        public override void Tick()
        {
            if (!init)
                return;
            
            //Log.Error("MAI gender (tick base): " + gender.ToString());

            if (destroy)
            {
                Destroy(DestroyMode.Vanish);
                return;
            }

            try
            {
                Pawn p = Create();
                DoMessage(p);
            }
            catch (Exception ex)
            {
                Log.Error("Error while creating "+ pawnDefName + ".\n" + ex.Message + "\nSTACK:\n" + ex.StackTrace );
            }

            destroy = true;
        }

        public virtual Pawn Create()
        {
            //Log.Error("MAI gender (create base): " + gender.ToString());

            return CreateAIPawn(pawnDefName, Position, this.Map, gender);
        }



        public static Thing Spawn(Thing newThing, IntVec3 loc, Map map)
        {
            return Spawn(newThing, loc, map, Rot4.South);
        }
        public static Thing Spawn(Thing newThing, IntVec3 loc, Map map, Rot4 rot)
        {
            //GenSpawn.WipeExistingThings(loc, rot, newThing.def, map, DestroyMode.Vanish);
            //if (newThing.def.randomizeRotationOnSpawn)
            //{
            //    newThing.Rotation = Rot4.Random;
            //}
            //else
            //{
            //    newThing.Rotation = rot;
            //}
            //newThing.SetPositionDirect(IntVec3.Invalid);
            //newThing.Position = loc;
            //newThing.SpawnSetup(map);

            newThing = GenSpawn.Spawn(newThing, loc, map, rot);

            //newThing.SetPositionDirect(IntVec3.Invalid);
            newThing.Position = loc;

            return newThing;
        }

        public static AIPawn CreateAIPawn(string pawnDefName, IntVec3 position, Map map, Gender gender)
        {
            return CreateAIPawn(pawnDefName, position, map, Faction.OfPlayer, gender);
        }
        public static AIPawn CreateAIPawn(string pawnDefName, IntVec3 position, Map map, Faction faction, Gender gender)
        {
            AIPawn pawn = AIPawnGenerator.GenerateAIPawn(pawnDefName, faction, map, gender);

            //// Old: 
            //IntVec3 spawnPos = position; // GenCellFinder.RandomStandableClosewalkCellNear(position, 1);

            // New: Find empty position in adjacent cell
            IntVec3 spawnPos = IntVec3.Invalid;

            if (map.thingGrid.ThingAt(position, ThingCategory.Building) != null || map.thingGrid.ThingAt(position, ThingCategory.Pawn) != null)
            {
                foreach (IntVec3 c in GenAdjFast.AdjacentCells8Way(position))
                {
                    if (map.thingGrid.ThingAt(c, ThingCategory.Building) == null && map.thingGrid.ThingAt(c, ThingCategory.Pawn) == null)
                    {
                        spawnPos = c;
                        break;
                    }
                }
            }

            if (spawnPos == IntVec3.Invalid)
                spawnPos = position;

            return (AIPawn)Spawn(pawn, spawnPos, map);
        }

        public static void DoMessage(Pawn pawn)
        {

            Letter letter = LetterMaker.MakeLetter("AIPawn_LetterLabel_BuildRechargeStationSoon".Translate(), 
                                                    "AIPawn_LetterText_BuildRechargeStationSoon".Translate().AdjustedFor(pawn), 
                                                    LetterDefOf.Good, 
                                                    pawn);
            Find.LetterStack.ReceiveLetter(letter);
            
        }

    }
}
