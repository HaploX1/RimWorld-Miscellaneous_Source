using System;
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
    public class BlueprintHandler__
    {
        private static readonly FloatRange NeolithicPawnsPoints = new FloatRange(500f, 900f);
        private static readonly FloatRange NonNeolithicPawnsPoints = new FloatRange(800f, 1600f);

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




                // Don't do anything, if there is an cryosleep casket at the building site
                foreach (IntVec3 current in mapRect.Cells)
                {
                    List<Thing> list = map.thingGrid.ThingsListAt(current);
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].def == ThingDefOf.AncientCryptosleepCasket)
                            return;
                    }

                    if (usedSpots != null)
                        usedSpots.Add(current); // prevent the base scatterer to use this spot again
                }

                // If a building material is defined, use this
                if (blueprint.buildingMaterial != null)
                    wallStuff = blueprint.buildingMaterial;

                // Make all buildings from the same random stuff
                if (wallStuff == null)
                    wallStuff = BaseGenUtility.RandomCheapWallStuff(faction, false);

                MakeBlueprintObject(map, faction, mapRect, blueprint, wallStuff);

                if (blueprint.createTrigger)
                {
                    RectTrigger_UnfogBaseArea rectTrigger = (RectTrigger_UnfogBaseArea)ThingMaker.MakeThing(ThingDef.Named("RectTrigger_UnfogBaseArea"), null);
                    rectTrigger.destroyIfUnfogged = true;
                    rectTrigger.Rect = mapRect;
                    if (blueprint.TriggerLetterMessageText != null)
                    {
                        if (blueprint.TriggerLetterLabel != null)
                            rectTrigger.letter = LetterMaker.MakeLetter(blueprint.TriggerLetterLabel.Translate(), blueprint.TriggerLetterMessageText.Translate(), blueprint.TriggerLetterDef, new GlobalTargetInfo(mapRect.CenterCell, map));
                        else
                            rectTrigger.letter = LetterMaker.MakeLetter("", blueprint.TriggerLetterMessageText.Translate(), blueprint.TriggerLetterDef, new GlobalTargetInfo(mapRect.CenterCell, map));
                    }
                    GenSpawn.Spawn(rectTrigger, mapRect.CenterCell, map);
                }


                // Create roof
                map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();

                HashSet<Room> rooms = new HashSet<Room>();
                foreach (IntVec3 current in mapRect.Cells)
                {
                    // Find all created rooms
                    Room room = c.GetRoom(map);
                    if (room != null && !room.TouchesMapEdge)
                        rooms.Add(room);
                }
                foreach (Room room in rooms)
                {
                    foreach (IntVec3 roofCell in room.Cells)
                    {
                        map.roofGrid.SetRoof(c, RoofDefOf.RoofConstructed);
                    }
                }
                map.roofGrid.RoofGridUpdate();


                // Add rooms to unfog area
                AddRoomCentersToRootsToUnfog(rooms.ToList());


            }
            catch (Exception ex)
            {
                Log.Error("Error in BlueprintHandler.CreateBlueprintAt(..): " + ex);
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
            blueprint.floorData = GetCleanedBlueprintData(blueprint.floorData);
            blueprint.pawnData = GetCleanedBlueprintData(blueprint.pawnData);
            blueprint.itemData = GetCleanedBlueprintData(blueprint.itemData);

            if (blueprint.buildingData == null && blueprint.floorData == null)
            {
                Log.ErrorOnce(string.Format("After cleaning the BlueprintData and FloorData of blueprint {0} -> both are null, nothing will be done!", blueprint.defName), 313001);
                return;
            }

            IntVec3 spawnBaseCell = new IntVec3(mapRect.BottomLeft.x, mapRect.TopRight.y, mapRect.TopRight.z);
            IntVec3 spawnCell;

            foreach (IntVec3 cell in mapRect)
            {
                // Check all cells and abort if there is something indestructible found
                if (!CheckCell(cell, map))
                    return;
            }

            allSpawnedPawns = null;

            // Work through blueprint. Note: top-left to bottom-right
            for (int zn = 0; zn < blueprint.size.z; zn++)
            {
                for (int x = 0; x < blueprint.size.x; x++)
                {
                    //// map can be clipped, don't work with the clipped parts
                    //if (x > mapRect.Width - 1 || zn > mapRect.Height - 1)
                    //    continue;

                    spawnCell = spawnBaseCell + new IntVec3(x, 0, -zn);

                    int itemPos = x + blueprint.size.x * zn;
                    ThingDef thingDef = TryGetThingDefFromBuildingData(blueprint, itemPos);
                    Rot4 thingRot = TryGetRotationFromBuildingData(blueprint, itemPos);
                    TerrainDef terrainDef = TryGetTerrainDefFromFloorData(blueprint, itemPos);
                    PawnKindDef pawnKindDef = TryGetPawnKindDefFromPawnData(blueprint, itemPos);
                    ThingDef itemDef = TryGetItemDefFromItemData(blueprint, itemPos);

                    List<Thing> list = map.thingGrid.ThingsListAt(spawnCell);
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].def == thingDef)
                            continue;
                    }

                    //Only clear the space, if something will be made here
                    if (thingDef != null || terrainDef != null || pawnKindDef != null || itemDef != null)
                        ClearCell(spawnCell, map);

                    if (blueprint.canHaveHoles && Rand.Value < 0.08f)
                        continue;

                    TrySetCellAs(spawnCell, map, faction, thingDef, thingRot, stuffDef, terrainDef, pawnKindDef, itemDef, blueprint);
                }

            }

            //// If pawns are spawned, place ancient shrine trigger
            //if (allSpawnedPawns != null && allSpawnedPawns.Count > 0)
            //{
            //    RectTrigger rectTrigger = (RectTrigger)ThingMaker.MakeThing(ThingDefOf.RectTrigger, null);
            //    rectTrigger.Rect = mapRect.ExpandedBy(1).ClipInsideMap(map);
            //    rectTrigger.letter = new Letter("LetterLabelAncientShrineWarning".Translate(), "AncientShrineWarning".Translate(), LetterType.BadNonUrgent, new GlobalTargetInfo(mapRect.CenterCell, map));
            //    rectTrigger.destroyIfUnfogged = false;
            //    GenSpawn.Spawn(rectTrigger, mapRect.CenterCell, map);
            //}


            // make the appropriate Lord
            pawnLord = LordMaker.MakeNewLord(faction, new LordJob_DefendBase(faction, mapRect.CenterCell), map, null);

            if (allSpawnedPawns != null)
            {
                foreach (Pawn pawn in allSpawnedPawns)
                    pawnLord.AddPawn(pawn);
            }

            // Make additional pawns if these are not enough!
            if (allSpawnedPawns == null || allSpawnedPawns.Count == 0 || allSpawnedPawns.Count <= 2 )
                CreateNewPawnGroup(map, mapRect, faction, rooms.ToList(), pawnLord);

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

            return blueprint.floorLegend[key];
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

            return blueprint.buildingLegend[key];
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

        // 3rd step: Get the ThingDef of the position from the ItemData of the blueprint.
        private static ThingDef TryGetItemDefFromItemData(MapGeneratorBaseBlueprintDef blueprint, int itemPos)
        {
            if (blueprint.itemData == null || blueprint.itemData.Count() - 1 < itemPos ||
                    blueprint.itemLegend == null)
                return null;

            char keyChar = blueprint.itemData.ElementAt(itemPos);
            string key = keyChar.ToString();

            if (!blueprint.itemLegend.ContainsKey(key))
                return null;

            return blueprint.itemLegend[key];
        }

        // 4th step: Get the PawnKindDef of the position from the PawnData of the blueprint.
        private static PawnKindDef TryGetPawnKindDefFromPawnData(MapGeneratorBaseBlueprintDef blueprint, int itemPos)
        {
            if (blueprint.pawnData == null || blueprint.pawnData.Count() - 1 < itemPos ||
                    blueprint.pawnLegend == null)
                return null;

            char keyChar = blueprint.pawnData.ElementAt(itemPos);
            string key = keyChar.ToString();

            if (!blueprint.pawnLegend.ContainsKey(key))
                return null;

            return blueprint.pawnLegend[key];
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

        // Clear the cell from other destroyable objects
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


        // Fill the cell
        private static void TrySetCellAs(IntVec3 c, Map map, Faction faction, ThingDef thingDef, Rot4 thingRot, ThingDef stuffDef = null, TerrainDef terrainDef = null,
                                        PawnKindDef pawnKindDef = null, ThingDef itemDef = null, MapGeneratorBaseBlueprintDef blueprint = null)
        {
            //Note: Here is no functionality to clear the cell by design, because it is possible to place items that are larger than 1x1

            // Check the cell information
            if (c == null || !c.InBounds(map))
            {
                Log.Warning("BlueprintHandler: Invalid Target-Cell: cell is null or out of bounds.");
                return;
            }

            //// only continue to do work if here isn't anything indestructable
            //List<Thing> thingList = c.GetThingList(map);
            //for (int i = 0; i < thingList.Count; i++)
            //{
            //    if (!thingList[i].def.destroyable)
            //    {
            //        return;
            //    }
            //}

            // 1st step - work with the Terrain
            if (terrainDef != null)
                map.terrainGrid.SetTerrain(c, terrainDef);
            else if (terrainDef == null && thingDef != null && stuffDef != null)
                map.terrainGrid.SetTerrain(c, BaseGenUtility.CorrespondingTerrainDef(stuffDef, true));

            // 2nd step - work with the Thing (Buildings)
            if (thingDef != null)
            {
                ThingDef stuffDef1 = stuffDef;

                if (!thingDef.MadeFromStuff)
                    stuffDef1 = null;

                Thing newThing = ThingMaker.MakeThing(thingDef, stuffDef1);
                if (thingRot == null || thingRot == Rot4.Invalid)
                    GenSpawn.Spawn(newThing, c, map);
                else
                    GenSpawn.Spawn(newThing, c, map, thingRot);
            }

            // The following needs blueprint data to work
            if (blueprint == null)
                return;

            // 3rd step - work with the Item
            //if (itemDef != null) // && blueprint.itemSpawnChance / 100 > Rand.Value)
            if (itemDef != null && blueprint.itemSpawnChance / 100 > Rand.Value)
            {
                ThingDef stuffDef2;

                if (itemDef.IsApparel)
                {
                    int cnt = 0;
                    while (true)
                    {
                        stuffDef2 = DefDatabase<ThingDef>.GetRandom();
                        if (stuffDef2.IsStuff && stuffDef2.stuffCategories.Contains(StuffCategoryDefOf.Fabric))
                            break;
                        cnt++;
                        if (cnt > 100)
                        {
                            stuffDef2 = DefDatabase<ThingDef>.GetNamedSilentFail("Synthread");
                            break;
                        }
                    }

                }
                else
                {
                    List<string> stuffPossibles = new List<string>() { "Steel", "Steel", "Steel", "Steel", "WoodLog", "Plasteel" };
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
                if (newHive != null)
                {
                    newHive.active = false;
                }
            }


            // 4th step - work with the Pawn
            if (pawnKindDef != null && blueprint.pawnSpawnChance / 100 > Rand.Value)
            {
                if (blueprint.factionDef != null)
                    faction = Find.FactionManager.FirstFactionOfDef(blueprint.factionDef);

                // null - find a valid faction.
                if (faction == null)
                {
                    faction = (from fac in Find.FactionManager.AllFactions
                               where fac.HostileTo(Faction.OfPlayer)
                               select fac)
                              .RandomElementByWeight((Faction fac) => 101 - fac.def.raidCommonality);

                    if (faction == null)
                        faction = Faction.OfMechanoids;
                }

                Pawn pawn = PawnGenerator.GeneratePawn(pawnKindDef, faction);
                pawn.mindState.Active = true;
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
            CompQuality treasureCQ = treasure.TryGetComp<CompQuality>();
            if (treasureCQ != null)
                treasureCQ.SetQuality(QualityUtility.RandomCreationQuality(Rand.RangeInclusive(10, 18)), ArtGenerationContext.Outsider);

            // adjust Stack to a random stack size
            if (treasure.def.stackLimit > 1)
                treasure.stackCount = Rand.RangeInclusive(1, treasure.def.stackLimit);

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


        private static void CreateNewPawnGroup(Map map, CellRect rect, Faction faction, List<Room> rooms, Lord lord)
        {
            ResolveParams resolveParams = default(ResolveParams);
            resolveParams.rect = rect;
            resolveParams.faction = faction;
            resolveParams.singlePawnLord = lord;
            resolveParams.pawnGroupKindDef = PawnGroupKindDefOf.FactionBase;
            resolveParams.singlePawnSpawnCellExtraPredicate = ((IntVec3 x) => CanReachARoom(map, x, rooms));

            float points = (!faction.def.techLevel.IsNeolithicOrWorse()) ? NonNeolithicPawnsPoints.RandomInRange : NeolithicPawnsPoints.RandomInRange;
            resolveParams.pawnGroupMakerParams = new PawnGroupMakerParms();
            resolveParams.pawnGroupMakerParams.tile = map.Tile;
            resolveParams.pawnGroupMakerParams.faction = faction;
            resolveParams.pawnGroupMakerParams.points = points;

            BaseGen.globalSettings.map = map;
            BaseGen.symbolStack.Push("pawnGroup", resolveParams);
            BaseGen.Generate();
            
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
