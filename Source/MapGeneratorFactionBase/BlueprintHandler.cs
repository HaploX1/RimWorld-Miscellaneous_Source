﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using Verse.AI;
using Verse.AI.Group;

namespace MapGenerator
{
    public class BlueprintHandler
    {
        private static readonly FloatRange NeolithicPawnsPoints = new FloatRange(200f, 800f);
        private static readonly FloatRange NonNeolithicPawnsPoints = new FloatRange(400f, 1600f);
        private static readonly IntRange CampfiresCount = new IntRange(1, 3);

        public static bool working;

        public static List<Pawn> allSpawnedPawns;
        public static Lord pawnLord;
        public static HashSet<Room> rooms;

        public static void CreateBlueprintAt(IntVec3 c, Map map, MapGeneratorBaseBlueprintDef blueprint, Faction faction, ref ThingDef wallStuff, ref List<IntVec3> usedSpots)
        {

            if (working)
            {
                Log.Error("Called BlueprintHandler.CreateBlueprintAt(..) while it's still working. This is not allowed!");
                return;
            }

            if (map == null)
            {
                Log.Error("Called BlueprintHandler.CreateBlueprintAt(..) with null map.");
                return;
            }

            working = true;
            allSpawnedPawns = new List<Pawn>();
            pawnLord = null;
            rooms = new HashSet<Room>();


            try
            {

                CellRect mapRect = new CellRect(c.x, c.z, blueprint.size.x, blueprint.size.z);

                mapRect.ClipInsideMap(map);

                // if mapRect was clipped -> the blueprint doesn't fit inside the map...
                if (mapRect.Width != blueprint.size.x || mapRect.Height != blueprint.size.z)
                    return;




                //// Don't do anything, if there is an cryosleep casket at the building site
                //foreach (IntVec3 current in mapRect.Cells)
                //{
                //    List<Thing> list = map.thingGrid.ThingsListAt(current);
                //    for (int i = 0; i < list.Count; i++)
                //    {
                //        if (list[i].def == ThingDefOf.AncientCryptosleepCasket)
                //            return;
                //    }

                //    if (usedSpots != null)
                //        usedSpots.Add(current); // prevent the base scatterer to use this spot again
                //}

                // If a building material is defined, use this
                if (blueprint.buildingMaterial != null && blueprint.buildingMaterial != "")
                    wallStuff = DefDatabase<ThingDef>.GetNamedSilentFail( blueprint.buildingMaterial );

                // Make all buildings from the same random stuff
                if (wallStuff == null)
                    wallStuff = BaseGenUtility.RandomCheapWallStuff(faction, false);

                MakeBlueprintObject(map, faction, mapRect, blueprint, wallStuff);

                if (blueprint.createTrigger)
                {
                    int nextSignalTagID = Find.UniqueIDsManager.GetNextSignalTagID();
                    string signalTag = "unfogBaseAreaTriggerSignal-" + nextSignalTagID;
                    SignalAction_Letter signalAction_Letter = ThingMaker.MakeThing(ThingDefOf.SignalAction_Letter, null) as SignalAction_Letter;
                    if (signalAction_Letter != null) {
                        signalAction_Letter.signalTag = signalTag;

                        if (blueprint.TriggerLetterMessageText != null)
                        {
                            if (blueprint.TriggerLetterDef == null)
                                blueprint.TriggerLetterDef = LetterDefOf.ThreatSmall;

                            if (blueprint.TriggerLetterLabel != null)
                            {
                                //signalAction_Letter.letter = LetterMaker.MakeLetter(blueprint.TriggerLetterLabel.Translate(), blueprint.TriggerLetterMessageText.Translate(), blueprint.TriggerLetterDef, new GlobalTargetInfo(mapRect.CenterCell, map, false));
                                signalAction_Letter.letterDef = blueprint.TriggerLetterDef;
                                signalAction_Letter.letterLabelKey = blueprint.TriggerLetterLabel;
                                signalAction_Letter.letterMessageKey = blueprint.TriggerLetterMessageText;
                                signalAction_Letter.lookTargets = new GlobalTargetInfo(mapRect.CenterCell, map, false);
                            }
                            else
                            {
                                //signalAction_Letter.letter = LetterMaker.MakeLetter("", blueprint.TriggerLetterMessageText.Translate(), blueprint.TriggerLetterDef, new GlobalTargetInfo(mapRect.CenterCell, map));
                                signalAction_Letter.letterDef = blueprint.TriggerLetterDef;
                                signalAction_Letter.letterLabelKey = " ";
                                signalAction_Letter.letterMessageKey = blueprint.TriggerLetterMessageText;
                                signalAction_Letter.lookTargets = new GlobalTargetInfo(mapRect.CenterCell, map);
                                }

                            GenSpawn.Spawn(signalAction_Letter, mapRect.CenterCell, map);
                        }
                    }

                    RectTrigger_UnfogBaseArea rectTrigger = ThingMaker.MakeThing(ThingDef.Named("RectTrigger_UnfogBaseArea"), null) as RectTrigger_UnfogBaseArea;
                    if (rectTrigger != null) {
                        rectTrigger.signalTag = signalTag;
                        rectTrigger.destroyIfUnfogged = true;
                        rectTrigger.Rect = mapRect;

                        GenSpawn.Spawn(rectTrigger, mapRect.CenterCell, map);
                    }
                }


                map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();

                HashSet<Room> rooms = new HashSet<Room>();
                foreach (IntVec3 current in mapRect.Cells)
                {
                    // Find all created rooms
                    Room room = current.GetRoom(map);
                    if (room != null && !room.TouchesMapEdge)
                        rooms.Add(room);
                }

                // Add rooms to unfog area
                AddRoomCentersToRootsToUnfog(rooms.ToList());


                // Create roof
                foreach (Room room in rooms)
                    BuildRoofToRoom(room, false);

                map.roofGrid.RoofGridUpdate();
                
            }
            catch (Exception ex)
            {
                string errBP = "";
                if (blueprint != null)
                    errBP = blueprint.defName;

                Log.Error("Error in BlueprintHandler.CreateBlueprintAt(..) '" + errBP + "': " + ex);
            }
            finally
            {
                // Whatever happends, when its done, reset the working state.
                working = false;

                // Clear all data holder
                allSpawnedPawns = null;
                pawnLord = null;
                rooms = null;
            }
        }



        private static void MakeBlueprintObject(Map map, Faction faction, CellRect mapRect, MapGeneratorBaseBlueprintDef blueprint, ThingDef stuffDef)
        {
            blueprint.buildingData = GetCleanedBlueprintData(blueprint.buildingData);
            blueprint.nonbuildingData = GetCleanedBlueprintData(blueprint.nonbuildingData);
            blueprint.floorData = GetCleanedBlueprintData(blueprint.floorData);
            blueprint.pawnData = GetCleanedBlueprintData(blueprint.pawnData);
            blueprint.itemData = GetCleanedBlueprintData(blueprint.itemData);

            if (blueprint.buildingData == null && blueprint.nonbuildingData == null && blueprint.floorData == null)
            {
                Log.ErrorOnce(string.Format("After cleaning the BlueprintData and FloorData of blueprint {0} -> both are null, nothing will be done!", blueprint.defName), 313001);
                return;
            }

            IntVec3 spawnBaseCell = new IntVec3(mapRect.Min.x, mapRect.Max.y, mapRect.Max.z);
            IntVec3 spawnCell;

            //// Check all cells and abort if there is something indestructible found -> disabled
            //foreach (IntVec3 cell in mapRect)
            //{
            //    if (!CheckCell(cell, map))
            //        return;
            //}

            allSpawnedPawns = null;

            
            // Disable automatic room updating
            map.regionAndRoomUpdater.Enabled = false;

            int step = 1;
            while (step <= 5)
            {
                //Log.Warning("Current step: " + step);

                // Work through blueprint - Note: top-left to bottom-right
                // Work step by step: 1st all floors, 2nd all things, 3rd all items, 4th all pawns
                for (int zn = 0; zn < blueprint.size.z; zn++)
                {
                    for (int x = 0; x < blueprint.size.x; x++)
                    {
                        //// map can be clipped, don't work with the clipped parts
                        //if (x > mapRect.Width - 1 || zn > mapRect.Height - 1)
                        //    continue;

                        if (blueprint.canHaveHoles && Rand.Value < 0.08f)
                            continue;

                        spawnCell = spawnBaseCell + new IntVec3(x, 0, -zn);

                        if (!TrySetCell_prepare_CheckCell(spawnCell, map))
                            continue;

                        
                        int itemPos = x + blueprint.size.x * zn;

                        try
                        {

                            ThingDef thingDef = TryGetThingDefFromBuildingData(blueprint, itemPos);
                            Rot4 thingRot = TryGetRotationFromBuildingData(blueprint, itemPos);
                            ThingDef nonthingDef = TryGetThingDefFromNonBuildingData(blueprint, itemPos);
                            TerrainDef terrainDef = TryGetTerrainDefFromFloorData(blueprint, itemPos);
                            PawnKindDef pawnKindDef = TryGetPawnKindDefFromPawnData(blueprint, itemPos);
                            ThingDef itemDef = TryGetItemDefFromItemData(blueprint, itemPos);

                            //List<Thing> list = map.thingGrid.ThingsListAt(spawnCell);
                            //for (int i = 0; i < list.Count; i++)
                            //{
                            //    if (list[i].def == thingDef)
                            //        continue;
                            //}


                            //Only clear the space, if something will be made here
                            // Do only in step 1:
                            if (step == 1)
                            {
                                if (thingDef != null || terrainDef != null || pawnKindDef != null || itemDef != null)
                                    ClearCell(spawnCell, map);
                            }

                            switch (step)
                            {
                                case 1: // Terrain
                                    TrySetCell_1_SetFloor(spawnCell, map, terrainDef, thingDef, stuffDef);
                                    break;
                                case 2: // non-Building
                                    TrySetCell_3_SetNonThing(spawnCell, map, nonthingDef);
                                    break;
                                case 3: // Building
                                    TrySetCell_2_SetThing(spawnCell, map, faction, thingDef, thingRot, stuffDef);
                                    break;
                                case 4: // Item
                                    TrySetCell_4_SetItem(spawnCell, map, itemDef, blueprint);
                                    break;
                                case 5: // Pawn
                                    TrySetCell_5_SetPawn(spawnCell, map, faction, pawnKindDef, blueprint);
                                    break;
                                default:
                                    return;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("MapGeneratorFactionBase - Error while creating the blueprint (" + blueprint.defName + ")\n" + ex.Message + "\n" + ex.StackTrace);
                        }
                    }
                }
                step++;
            }

            // Update the powernets
            map.powerNetManager.UpdatePowerNetsAndConnections_First();

            // Enable automatic room updating and rebuild all rooms
            map.regionAndRoomUpdater.Enabled = true;
            map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();

            HashSet<Room> rooms = new HashSet<Room>();
            foreach (IntVec3 current in mapRect.Cells)
            {
                // Find all created rooms
                Room room = current.GetRoom(map);
                if (room != null && !room.TouchesMapEdge)
                    rooms.Add(room);
            }
            // Add rooms to unfog area
            AddRoomCentersToRootsToUnfog(rooms.ToList());


            // make the appropriate Lord
            pawnLord = LordMaker.MakeNewLord(faction, new LordJob_DefendBase(faction, mapRect.CenterCell), map, null);
            
            // Get points used by current pawns
            float pointsUsed = 0f;
            if (allSpawnedPawns != null && allSpawnedPawns.Count > 0)
            {
                foreach (Pawn pawnSpawned in allSpawnedPawns)
                    pointsUsed += pawnSpawned.kindDef.combatPower;
            }

            // Make additional pawns if these are not enough!
            if (allSpawnedPawns == null || allSpawnedPawns.Count == 0 || 
                pointsUsed < (faction.def.techLevel == TechLevel.Neolithic ? NeolithicPawnsPoints.min : NonNeolithicPawnsPoints.min))
            {
                //Log.Warning("Info: Creating base pawns..");
                PrepareBaseGen_PawnGroup(map, mapRect, faction, rooms.ToList(), pawnLord, pointsUsed);
            }

            PrepareBaseGen_CampFires(map, mapRect, faction);

            BaseGen.Generate();

            if (allSpawnedPawns != null)
            {
                foreach (Pawn pawn in allSpawnedPawns)
                    pawnLord.AddPawn(pawn);
            }

            allSpawnedPawns = null;
        }



        // 1st step: Get the TerrainDef of the position from the FloorData of the blueprint.
        private static TerrainDef TryGetTerrainDefFromFloorData(MapGeneratorBaseBlueprintDef blueprint, int itemPos)
        {
            if (blueprint.floorData == null || blueprint.floorData.Count() - 1 < itemPos ||
                    blueprint.floorLegend == null)
                return null;

            char keyChar = blueprint.floorData.ElementAt(itemPos);
            string key = keyChar.ToString();

            if (!blueprint.floorLegend.ContainsKey(key))
                return null;

            return DefDatabase<TerrainDef>.GetNamedSilentFail( blueprint.floorLegend[key] );
        }

        // 2nd step: Get the ThingDef of the position from the BuildingData of the blueprint.
        private static ThingDef TryGetThingDefFromBuildingData(MapGeneratorBaseBlueprintDef blueprint, int itemPos)
        {
            if (blueprint.buildingData == null || blueprint.buildingData.Count() - 1 < itemPos ||
                    blueprint.buildingLegend == null)
                return null;

            char keyChar = blueprint.buildingData.ElementAt(itemPos);
            string key = keyChar.ToString();

            if (!blueprint.buildingLegend.ContainsKey(key))
                return null;

            return DefDatabase<ThingDef>.GetNamedSilentFail(blueprint.buildingLegend[key]);
        }
        // 2nd step (b): Get the Rotation of the position from the BuildingData of the blueprint.
        private static Rot4 TryGetRotationFromBuildingData(MapGeneratorBaseBlueprintDef blueprint, int itemPos)
        {
            // Using buildingData and rotationLegend here..
            if (blueprint.buildingData == null || blueprint.buildingData.Count() - 1 < itemPos ||
                    blueprint.rotationLegend == null)
                return Rot4.Invalid;

            char keyChar = blueprint.buildingData.ElementAt(itemPos);
            string key = keyChar.ToString();

            if (!blueprint.rotationLegend.ContainsKey(key))
                return Rot4.Invalid;

            return blueprint.rotationLegend[key];
        }

        // 3rd step: Get the ThingDef of the position from the Non-BuildingData of the blueprint.
        private static ThingDef TryGetThingDefFromNonBuildingData(MapGeneratorBaseBlueprintDef blueprint, int itemPos)
        {
            if (blueprint.nonbuildingData == null || blueprint.nonbuildingData.Count() - 1 < itemPos ||
                    blueprint.nonbuildingLegend == null)
                return null;

            char keyChar = blueprint.nonbuildingData.ElementAt(itemPos);
            string key = keyChar.ToString();

            if (!blueprint.nonbuildingLegend.ContainsKey(key))
                return null;

            return DefDatabase<ThingDef>.GetNamedSilentFail(blueprint.nonbuildingLegend[key]);
        }

        // 4th step: Get the ThingDef of the position from the ItemData of the blueprint.
        private static ThingDef TryGetItemDefFromItemData(MapGeneratorBaseBlueprintDef blueprint, int itemPos)
        {
            if (blueprint.itemData == null || blueprint.itemData.Count() - 1 < itemPos ||
                    blueprint.itemLegend == null)
                return null;

            char keyChar = blueprint.itemData.ElementAt(itemPos);
            string key = keyChar.ToString();

            if (!blueprint.itemLegend.ContainsKey(key))
                return null;

            return DefDatabase<ThingDef>.GetNamedSilentFail(blueprint.itemLegend[key]);
        }

        // 5th step: Get the PawnKindDef of the position from the PawnData of the blueprint.
        private static PawnKindDef TryGetPawnKindDefFromPawnData(MapGeneratorBaseBlueprintDef blueprint, int itemPos)
        {
            if (blueprint.pawnData == null || blueprint.pawnData.Count() - 1 < itemPos ||
                    blueprint.pawnLegend == null)
                return null;

            char keyChar = blueprint.pawnData.ElementAt(itemPos);
            string key = keyChar.ToString();

            if (!blueprint.pawnLegend.ContainsKey(key))
                return null;

            return DefDatabase<PawnKindDef>.GetNamedSilentFail(blueprint.pawnLegend[key]);
        }

        // Clear the cell from other destroyable objects
        private static bool ClearCell(IntVec3 c, Map map)
        {
            List<Thing> thingList = c.GetThingList(map);
            if (!CheckCell(c, map)) return false;
            for (int j = thingList.Count - 1; j >= 0; j--)
            {
                thingList[j].Destroy(DestroyMode.Vanish);
            }
            return true;
        }

        // Check the cell for not destroyable objects
        private static bool CheckCell(IntVec3 c, Map map)
        {
            List<Thing> thingList = c.GetThingList(map);
            for (int i = 0; i < thingList.Count; i++)
            {
                if (!thingList[i].def.destroyable)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool TrySetCell_prepare_CheckCell(IntVec3 c, Map map)
        {
            // Check the cell information
            if (c == null || !c.InBounds(map))
            {
                Log.Warning("BlueprintHandler: Invalid Target-Cell: cell is null or out of bounds.");
                return false;
            }

            // Check cell for undestroyable objects (Geysir)
            List<Thing> tl = map.thingGrid.ThingsListAt(c);
            if (tl != null && tl.Count > 0)
                foreach (Thing t in tl)
                    if (!t.def.destroyable)
                        return false;

            //// only continue to do work if here isn't anything indestructable
            //List<Thing> thingList = c.GetThingList(map);
            //for (int i = 0; i < thingList.Count; i++)
            //{
            //    if (!thingList[i].def.destroyable)
            //    {
            //        return false;
            //    }
            //}

            return true;
        }

        // Fill the cell with Floor
        private static void TrySetCell_1_SetFloor(IntVec3 c, Map map, TerrainDef terrainDef = null, ThingDef thingDef = null, ThingDef stuffDef = null)
        {
            //Note: Here is no functionality to clear the cell by design, because it is possible to place items that are larger than 1x1

            // 1st step - work with the Terrain
            if (terrainDef != null)
                map.terrainGrid.SetTerrain(c, terrainDef);
            else if (terrainDef == null && thingDef != null && stuffDef != null) // Do ONLY when a thing will be placed here!
                map.terrainGrid.SetTerrain(c, BaseGenUtility.CorrespondingTerrainDef(stuffDef, true));
        }
        
        // Fill the cell with Thing (Building)
        private static void TrySetCell_2_SetThing(IntVec3 c, Map map, Faction faction, ThingDef thingDef, Rot4 thingRot, ThingDef stuffDef = null)
        {
            //Note: Here is no functionality to clear the cell by design, because it is possible to place items that are larger than 1x1

            // 2nd step - work with the Thing (Buildings)
            if (thingDef != null)
            {
                ThingDef stuffDef1 = stuffDef;

                if (!thingDef.MadeFromStuff)
                    stuffDef1 = null;

                Thing newThing = ThingMaker.MakeThing(thingDef, stuffDef1);
                if (thingRot == null || thingRot == Rot4.Invalid)
                    newThing = GenSpawn.Spawn(newThing, c, map);
                else
                    newThing = GenSpawn.Spawn(newThing, c, map, thingRot);

                // Set it to the current faction
                newThing.SetFactionDirect( faction );


                // If CompGatherSpot -> disable it! 
                CompGatherSpot compGathering = newThing.TryGetComp<CompGatherSpot>();
                if (compGathering != null)
                    compGathering.Active = false;
            }
        }

        // Fill the cell with non-Thing (non-Building)
        private static void TrySetCell_3_SetNonThing(IntVec3 c, Map map, ThingDef thingDef) //, ThingDef stuffDef = null)
        {
            //Note: Here is no functionality to clear the cell by design, because it is possible to place items that are larger than 1x1

            // 3rd step - work with the Non-Thing (Non-Buildings)
            if (thingDef != null)
            {
                ThingDef stuffDef1 = null;//stuffDef;

                if (!thingDef.MadeFromStuff)
                    stuffDef1 = null;

                Thing newThing = ThingMaker.MakeThing(thingDef, stuffDef1);
                //if (thingRot == null || thingRot == Rot4.Invalid)
                    GenSpawn.Spawn(newThing, c, map);
                //else
                //    GenSpawn.Spawn(newThing, c, map, thingRot);
            }
        }

        // Fill the cell with Item
        private static void TrySetCell_4_SetItem(IntVec3 c, Map map, ThingDef itemDef = null, MapGeneratorBaseBlueprintDef blueprint = null)
        {
            //Note: Here is no functionality to clear the cell by design, because it is possible to place items that are larger than 1x1

            // The following needs blueprint data to work
            if (blueprint == null)
                return;
            

            // 4th step - work with the Item
            //if (itemDef != null) // && blueprint.itemSpawnChance / 100 > Rand.Value)
            if (itemDef != null && blueprint.itemSpawnChance / 100 > Rand.Value)
            {
                ThingDef stuffDef2;
                if (itemDef.IsApparel)
                {
                    if (!DefDatabase<ThingDef>.AllDefs.Where<ThingDef>(t => t.IsStuff &&
                                                                            t.stuffProps != null && t.stuffProps.categories != null && t.stuffProps.categories.Contains(StuffCategoryDefOf.Fabric))
                                                                                .TryRandomElement(out stuffDef2))
                    {
                        stuffDef2 = DefDatabase<ThingDef>.GetNamedSilentFail("Synthread");
                    }

                }
                else
                {
                    List<string> stuffPossibles = new List<string>() { "Steel", "Steel", "Steel", "Steel", "Silver", "Gold", "Jade", "Plasteel" };
                    stuffDef2 = DefDatabase<ThingDef>.GetNamedSilentFail(stuffPossibles.RandomElement());
                }

                if (!itemDef.MadeFromStuff)
                    stuffDef2 = null;

                Thing newItem = TryGetTreasure(itemDef, stuffDef2);

                newItem = GenSpawn.Spawn(newItem, c, map);

                // Don't forget to set the items to forbidden!
                if (newItem.TryGetComp<CompForbiddable>() != null)
                    newItem.SetForbidden(true, false);

                // If it is a hive, it needs to be deactivated
                Hive newHive = newItem as Hive;
                //if (newHive != null)
                //{
                //    newHive.active = false;
                //}
            }

        }

        // Fill the cell with Pawn
        private static void TrySetCell_5_SetPawn(IntVec3 c, Map map, Faction faction, PawnKindDef pawnKindDef = null, MapGeneratorBaseBlueprintDef blueprint = null)
        {
            //Note: Here is no functionality to clear the cell by design, because it is possible to place items that are larger than 1x1

            // The following needs blueprint data to work
            if (blueprint == null)
                return;

            // 5th step - work with the Pawn
            if (pawnKindDef != null && blueprint.pawnSpawnChance / 100 > Rand.Value)
            {
                if (blueprint.factionDef != null)
                    faction = Find.FactionManager.FirstFactionOfDef(blueprint.factionDef);

                // null - find a valid faction.
                if (faction == null)
                {
                    if ((from fac in Find.FactionManager.AllFactions
                                                     where fac.HostileTo(Faction.OfPlayer)
                                                     select fac).TryRandomElementByWeight((Faction fac) => 101 - fac.def.RaidCommonalityFromPoints(map.IncidentPointsRandomFactorRange.RandomInRange), out faction)
                                                     == false)
                    {
                        faction = Faction.OfMechanoids;
                    }
                }

                Pawn pawn = PawnGenerator.GeneratePawn(pawnKindDef, faction);
                //pawn.mindState.Active = true;
                pawn = GenSpawn.Spawn(pawn, c, map) as Pawn;

                if (pawn != null)
                {
                    if (allSpawnedPawns == null)
                        allSpawnedPawns = new List<Pawn>();

                    allSpawnedPawns.Add(pawn);
                }
            }

        }

        private static Thing TryGetTreasure(ThingDef treasureDef, ThingDef stuffDef)
        {
            Thing treasure = null;

            // make treasure
            if (treasureDef == null)
                return null;

            treasure = ThingMaker.MakeThing(treasureDef, stuffDef);

            // try adjust quality
            CompQuality treasureQuality = treasure.TryGetComp<CompQuality>();
            if (treasureQuality != null)
                treasureQuality.SetQuality(QualityUtility.GenerateQualityRandomEqualChance(), ArtGenerationContext.Outsider);

            // adjust Stack to a random stack size
            if (treasure.def.stackLimit > 1)
            {
                if (treasure.def.stackLimit > 50)
                    treasure.stackCount = Rand.RangeInclusive(1, 45);
                else
                    treasure.stackCount = Rand.RangeInclusive(1, treasure.def.stackLimit);
            }

            // adjust Hitpoints (40% to 100%)
            if (treasure.stackCount == 1)
                treasure.HitPoints = Rand.RangeInclusive((int)(treasure.MaxHitPoints * 0.4), treasure.MaxHitPoints);

            return treasure;
        }


        private static void AddRoomCentersToRootsToUnfog(List<Room> allRooms)
        {
            if (Current.ProgramState != ProgramState.MapInitializing)
            {
                return;
            }
            List<IntVec3> rootsToUnfog = Verse.MapGenerator.rootsToUnfog;
            for (int i = 0; i < allRooms.Count; i++)
            {
                rootsToUnfog.Add(allRooms[i].Cells.RandomElement());
            }
        }


        private static void PrepareBaseGen_CampFires(Map map, CellRect rect, Faction faction)
        {
            if (map.mapTemperature.OutdoorTemp > 0f || rect.Area == 0)
                return;

            int randomInRange = CampfiresCount.RandomInRange;
            
            ResolveParams resolveParams = default(ResolveParams);
            resolveParams.rect = rect;
            resolveParams.faction = faction;

            for (int j = 0; j < randomInRange; j++)
            {
                resolveParams.rect = rect;
                resolveParams.faction = faction;
                BaseGen.symbolStack.Push("outdoorsCampfire", resolveParams);
            }
            
            BaseGen.globalSettings.map = map;
        }
        private static void PrepareBaseGen_PawnGroup(Map map, CellRect rect, Faction faction, List<Room> rooms, Lord lord, float pointsUsed)
        {
            ResolveParams resolveParams = default(ResolveParams);
            resolveParams.rect = rect;
            resolveParams.faction = faction;
            resolveParams.singlePawnLord = lord;
            resolveParams.pawnGroupKindDef = PawnGroupKindDefOf.Settlement;
            resolveParams.singlePawnSpawnCellExtraPredicate = ((IntVec3 x) => CanReachARoom(map, x, rooms));

            float points = (!faction.def.techLevel.IsNeolithicOrWorse()) ? NonNeolithicPawnsPoints.RandomInRange : NeolithicPawnsPoints.RandomInRange;

            // reduce by points already used by the current pawns
            if (points - pointsUsed > 100)
                points -= pointsUsed;
            else
                points = 200f;

            resolveParams.pawnGroupMakerParams = new PawnGroupMakerParms();
            resolveParams.pawnGroupMakerParams.tile = map.Tile;
            resolveParams.pawnGroupMakerParams.faction = faction;
            resolveParams.pawnGroupMakerParams.points = points;

            BaseGen.globalSettings.map = map;
            BaseGen.symbolStack.Push("pawnGroup", resolveParams);
            
        }
        private static bool CanReachARoom(Map map, IntVec3 root, List<Room> allRooms)
        {
            for (int i = 0; i < allRooms.Count; i++)
            {
                if (map.reachability.CanReach(root, allRooms[i].Cells.RandomElement(), PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
                {
                    return true;
                }
            }
            return false;
        }

        private static void BuildRoofToRoom(Room room, bool doUpdate = true)
        {
            if ( room.TouchesMapEdge )
                return;

            foreach (IntVec3 cell in room.Cells)
            {
                room.Map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
            }

            if (doUpdate)
                room.Map.roofGrid.RoofGridUpdate();
        }

        // The blueprint data isn't workable without some formatting.. 
        private static string GetCleanedBlueprintData(string data)
        {
            if (data.NullOrEmpty())
                return null;

            string newData = "";

            foreach (char c in data)
            {
                // only use allowed chars
                if (char.IsLetterOrDigit(c) || c == ',' || c == '.' || c == '#' || c == '~' || c == '?' || c == '!' || 
                                               c == '-' || c == '+' || c == '*' || c == '&' || c == '$' || c == '§' || 
                                               c == '@' || c == '€')
                    newData += c;
            }

            if (newData.NullOrEmpty())
                return null;
            return newData;
        }

    }
}
