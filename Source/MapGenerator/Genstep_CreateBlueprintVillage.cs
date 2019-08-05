using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using RimWorld;
using RimWorld.Planet;
using Verse.AI;
using Verse.AI.Group;
using RimWorld.BaseGen;

namespace MapGenerator
{
    public class GenStep_CreateBlueprintVillage : GenStep_CreateBlueprintBaseClass
    {
        public readonly IntRange ruinOffsetHorizontalRange = new IntRange(5, 15);
        public readonly IntRange ruinOffsetVerticalRange = new IntRange(5, 15);

        public IntRange ruinDistanceRange = new IntRange(4, 20);

        public IntRange ruinCountRange = new IntRange(3, 8);
        private int ruinCountDown;

        public IntRange villageCountRange = new IntRange(1, 1);

        //private List<IntVec3> usedCells = new List<IntVec3>();

        private ThingDef selectedWallStuff;
        private Faction faction;
        private List<Pawn> allSpawnedPawns;


        protected override void ScatterAt(IntVec3 loc, Map map, int stackCount = 1)
        {
            
            // After 5 min reset the saved cells!
            if (usedCells_lastChange.AddMinutes(5) < DateTime.UtcNow)
            {
                usedCells.Clear();
                usedCells_lastChange = DateTime.UtcNow;
            }

            // update the usedSpots
            if (usedSpots != null && usedSpots.Count > 0)
            {
                foreach (IntVec3 usedSpot in usedSpots)
                    usedCells.Add(usedSpot);
                usedCells_lastChange = DateTime.UtcNow;
            }


            ruinCountDown = ruinCountRange.RandomInRange;

            while (ruinCountDown > 0)
            {
                //Select random blueprint, but not a mapCenterBlueprint
                MapGeneratorBlueprintDef blueprint = DefDatabase<MapGeneratorBlueprintDef>.AllDefsListForReading
                    .Where((MapGeneratorBlueprintDef b) => !b.mapCenterBlueprint)
                    .RandomElementByWeight((MapGeneratorBlueprintDef b) => b.chance);

                // set the ruinDistance to the size of the blueprint
                if (blueprint.size.x > blueprint.size.z)
                    ruinDistanceRange.min = blueprint.size.x / 2 + 1;
                else
                    ruinDistanceRange.min = blueprint.size.z / 2 + 1;

                if (ruinDistanceRange.min > ruinDistanceRange.max)
                    ruinDistanceRange.max = ruinDistanceRange.min + 4;


                IntVec3 workLoc = TryFindValidScatterCellNear(loc, map, blueprint, usedCells);
                if (workLoc != IntVec3.Invalid)
                {
                    // place a blueprint ruin
                    ScatterBlueprintAt(workLoc, map, blueprint, ref selectedWallStuff, usedSpots);
                }

                ruinCountDown--;
            }

            // reset
            selectedWallStuff = null;
            //usedCells.Clear();
        }


        protected override bool CanScatterAt(IntVec3 loc, Map map)
        {
            return base.CanScatterAt(loc, map) && loc.SupportsStructureType(map, TerrainAffordanceDefOf.Heavy);
        }


        // This function may need some work...
        private IntVec3 TryFindValidScatterCellNear(IntVec3 loc, Map map, MapGeneratorBlueprintDef blueprint, HashSet<IntVec3> invalidCells)
        {
            if (usedCells.Count == 0)
                return loc;

            IntVec2 size = blueprint.size;
            bool allowCenter = blueprint.mapCenterBlueprint;

            int searchTry = 0;
            int searchTriesMax = 30;
            while (searchTry < searchTriesMax)
            {

                if (!allowCenter)
                {
                    // Check if the loc is near the spawn location -> move loc by +20, 0, -20
                    IntVec2 nogoCenter = new IntVec2(map.Size.x / 2, map.Size.z / 2);
                    CellRect nogoCenterRect = new CellRect(nogoCenter.x, nogoCenter.z, 1, 1);
                    nogoCenterRect = nogoCenterRect.ExpandedBy(10);
                    if (nogoCenterRect.Contains(loc))
                        loc = new IntVec3(loc.x + 20, loc.y, loc.z - 20);
                }

                int placement = Rand.RangeInclusive(0, 7);

                // Find nearest used cell to the distance
                IntVec3 workCell = IntVec3.Invalid;
                foreach (IntVec3 cell in usedCells)
                {
                    switch (placement)
                    {
                        case 0: // north
                            workCell = workCell == IntVec3.Invalid || cell.z > workCell.z ? cell : workCell;
                            break;
                        case 1: // north-east
                            workCell = workCell == IntVec3.Invalid || cell.z > workCell.z && cell.x > workCell.x ? cell : workCell;
                            break;
                        case 2: // east
                            workCell = workCell == IntVec3.Invalid || cell.x > workCell.x ? cell : workCell;
                            break;
                        case 3: // south-east
                            workCell = workCell == IntVec3.Invalid || cell.z < workCell.z && cell.x > workCell.x ? cell : workCell;
                            break;
                        case 4: // south
                            workCell = workCell == IntVec3.Invalid || cell.z < workCell.z ? cell : workCell;
                            break;
                        case 5: // south-west
                            workCell = workCell == IntVec3.Invalid || cell.z < workCell.z && cell.x < workCell.x ? cell : workCell;
                            break;
                        case 6: // west
                            workCell = workCell == IntVec3.Invalid || cell.x < workCell.x ? cell : workCell;
                            break;
                        case 7: // north-west
                            workCell = workCell == IntVec3.Invalid || cell.z > workCell.z && cell.x < workCell.x ? cell : workCell;
                            break;
                        default:
                            // error
                            workCell = IntVec3.Invalid;
                            break;
                    }
                }

                // No valid cell found
                if (workCell == IntVec3.Invalid)
                    return IntVec3.Invalid;

                IntVec3 tmpCell = IntVec3.Invalid;

                int workDistance = ruinDistanceRange.RandomInRange;

                // set workDistance according to blueprint size
                if (size.x > size.z)
                    workDistance = size.x + Rand.RangeInclusive(1,5);
                else
                    workDistance = size.z + Rand.RangeInclusive(1, 5);


                // set new cell
                switch (placement)
                {
                    case 0: // north
                        tmpCell = new IntVec3(0, 0, +workDistance);
                        workCell += tmpCell;
                        break;
                    case 1: // north-east
                        tmpCell = new IntVec3(+workDistance, 0, +workDistance);
                        workCell += tmpCell;
                        break;
                    case 2: // east
                        tmpCell = new IntVec3(+workDistance, 0, 0);
                        workCell += tmpCell;
                        break;
                    case 3: // south-east
                        tmpCell = new IntVec3(+workDistance, 0, -workDistance - (int)ruinOffsetVerticalRange.Average);
                        workCell += tmpCell;
                        break;
                    case 4: // south
                        tmpCell = new IntVec3(0, 0, -workDistance - (int)ruinOffsetVerticalRange.Average);
                        workCell += tmpCell;
                        break;
                    case 5: // south-west
                        tmpCell = new IntVec3(-workDistance - (int)ruinOffsetHorizontalRange.Average, 0, -workDistance - (int)ruinOffsetVerticalRange.Average);
                        workCell += tmpCell;
                        break;
                    case 6: // west
                        tmpCell = new IntVec3(-workDistance - (int)ruinOffsetHorizontalRange.Average, 0, 0);
                        workCell += tmpCell;
                        break;
                    case 7: // north-west
                        tmpCell = new IntVec3(-workDistance - (int)ruinOffsetHorizontalRange.Average, 0, +workDistance);
                        workCell += tmpCell;
                        break;
                    default:
                        // error
                        workCell = IntVec3.Invalid;
                        break;
                }

                // set new min distance according to tmpCell
                if (tmpCell.IsValid)
                {
                    if (Math.Abs(tmpCell.x) / 2 < ruinDistanceRange.max && Math.Abs(tmpCell.z) / 2 < ruinDistanceRange.max)
                    {
                        if (Math.Abs(tmpCell.x) > Math.Abs(tmpCell.z))
                            ruinDistanceRange.min = Math.Abs(tmpCell.x) / 2;
                        else
                            ruinDistanceRange.min = Math.Abs(tmpCell.z) / 2;

                        if (ruinDistanceRange.min > ruinDistanceRange.max)
                            ruinDistanceRange.max = ruinDistanceRange.min;
                    }
                }

                if (workCell.InBounds(map) && 
                    CanScatterAt(workCell, map) && 
                    IsPositionValidForBlueprint(workCell, size, invalidCells))
                        return workCell;

                searchTry++;
            }
            return IntVec3.Invalid;
        }

        private bool IsPositionValidForBlueprint(IntVec3 cell, IntVec2 size, HashSet<IntVec3> invalidCells)
        {
            // create all needed cells
            CellRect workRect = new CellRect(cell.x, cell.z, size.x, size.z);

            List<IntVec3> workCells = new List<IntVec3>(workRect.Cells);

            // check if one of the cells fit into the invalidCells
            foreach (IntVec3 invalidCell in invalidCells)
            {
                foreach (IntVec3 workCell in workCells)
                {
                    if (invalidCell == workCell)
                        return false;
                }
            }
            return true;
        }



        private void ScatterBlueprintAt(IntVec3 loc, Map map, MapGeneratorBlueprintDef blueprint, ref ThingDef wallStuff, List<IntVec3> listOfUsedCells)
        {

            CellRect mapRect = new CellRect(loc.x, loc.z, blueprint.size.x, blueprint.size.z);

            mapRect.ClipInsideMap(map);

            // if mapRect was clipped -> the blueprint doesn't fit inside the map, end here!
            if (mapRect.Width != blueprint.size.x || mapRect.Height != blueprint.size.z)
                return;

            // Check if we will build on a usedCell
            bool usedCellFound = false;
            foreach (IntVec3 cell in mapRect.Cells)
            {
                if (listOfUsedCells != null && listOfUsedCells.Contains(cell))
                {
                    usedCellFound = true;
                    break;
                }
            }
            if (usedCellFound)
                return;


            // Don't do anything, if there is a cryosleep casket at the building site
            foreach (IntVec3 current in mapRect.Cells)
            {
                List<Thing> list = map.thingGrid.ThingsListAt(current);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].def == ThingDefOf.AncientCryptosleepCasket)
                        return;
                }
                // Don't do anything if there is a pawn (mechanoid? insect?) here
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                    if (pawn != null && pawn.Spawned && pawn.Position == current)
                        return;

                usedCells.Add(current); // don't use the same spot twice..
                usedSpots.Add(current); // ..also prevent the base scatterer to use this spot
                usedCells_lastChange = DateTime.UtcNow;
            }

            // If a building material is defined, use this
            if (blueprint.buildingMaterial != null && blueprint.buildingMaterial != "")
                wallStuff = DefDatabase<ThingDef>.GetNamedSilentFail(blueprint.buildingMaterial);

            int w = 0;
            while (true)
            {
                w++;
                if (w > 100) break;

                // Make all buildings from the same random stuff -- In BaseGen use faction, in MapGen use null!
                if (wallStuff == null)
                    wallStuff = BaseGenUtility.RandomCheapWallStuff(null, true); // BaseGenUtility.RandomCheapWallStuff(faction, true);

                //If not specified, don't use wood or leather
                if (blueprint.buildingMaterial != null || (!wallStuff.defName.ToLower().Contains("wood") && !wallStuff.defName.ToLower().Contains("leather")))
                    break;
            }

            MakeBlueprintRoom(mapRect, map, blueprint, wallStuff);
            
            if (blueprint.createTrigger)
            {
                int nextSignalTagID = Find.UniqueIDsManager.GetNextSignalTagID();
                string signalTag = "unfogAreaTriggerSignal-" + nextSignalTagID;
                SignalAction_Letter signalAction_Letter = (SignalAction_Letter)ThingMaker.MakeThing(ThingDefOf.SignalAction_Letter, null);
                signalAction_Letter.signalTag = signalTag;

                if (blueprint.TriggerLetterMessageText != null)
                {
                    if (blueprint.TriggerLetterLabel != null)
                        signalAction_Letter.letter = LetterMaker.MakeLetter(blueprint.TriggerLetterLabel.Translate(), blueprint.TriggerLetterMessageText.Translate(), blueprint.TriggerLetterDef, new GlobalTargetInfo(mapRect.CenterCell, map, false));
                    else
                        signalAction_Letter.letter = LetterMaker.MakeLetter(" ", blueprint.TriggerLetterMessageText.Translate(), blueprint.TriggerLetterDef, new GlobalTargetInfo(mapRect.CenterCell, map));

                    GenSpawn.Spawn(signalAction_Letter, mapRect.CenterCell, map);
                }

                RectTrigger_UnfogArea rectTrigger = (RectTrigger_UnfogArea)ThingMaker.MakeThing(ThingDef.Named("RectTrigger_UnfogArea"), null);
                rectTrigger.signalTag = signalTag;
                rectTrigger.destroyIfUnfogged = true;
                rectTrigger.Rect = mapRect;

                GenSpawn.Spawn(rectTrigger, mapRect.CenterCell, map);
            }
        }


        private void MakeBlueprintRoom(CellRect mapRect, Map map, MapGeneratorBlueprintDef blueprint, ThingDef stuffDef)
        {
            blueprint.buildingData = CleanUpBlueprintData(blueprint.buildingData);
            blueprint.floorData = CleanUpBlueprintData(blueprint.floorData);
            blueprint.pawnData = CleanUpBlueprintData(blueprint.pawnData);
            blueprint.itemData = CleanUpBlueprintData(blueprint.itemData);

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

            try
            {
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

                        if ((blueprint.canHaveHoles ||
                            (MapGenerator_ModSettings.createAllNonPawnBPsWithHoles && (blueprint.pawnLegend == null || blueprint.pawnLegend.Count <= 0))) &&
                            Rand.Value < MapGenerator_ModSettings.chanceForHoles)
                            continue;

                        // If placed on water, increase the hole chance, if no pawns are to be placed!
                        if (spawnCell.GetTerrain(map).defName.ToLower().Contains("water") && (blueprint.pawnLegend == null || blueprint.pawnLegend.Count <= 0) && Rand.Value < MapGenerator_ModSettings.chanceForHolesOnWater)
                            continue;
                        
                        TrySetCellAs(spawnCell, map, thingDef, thingRot, stuffDef, terrainDef, pawnKindDef, itemDef, blueprint);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Misc. MapGenerator -- Error with blueprint '" + blueprint.defName + "'. Placement placement position at " +
                                mapRect.CenterCell.ToString() + " on a map of the size " + map.Size.ToString() + "\n" +
                                ex.Message + "\n" + ex.StackTrace);
            }

            // If pawns are spawned, place ancient shrine trigger
            if (allSpawnedPawns != null && allSpawnedPawns.Count > 0)
            {
                int nextSignalTagID = Find.UniqueIDsManager.GetNextSignalTagID();
                string signalTag = "hiddenEnemyPawnsApproached-" + nextSignalTagID;

                SignalAction_Letter signalAction_Letter = (SignalAction_Letter)ThingMaker.MakeThing(ThingDefOf.SignalAction_Letter, null);
                signalAction_Letter.signalTag = signalTag;
                signalAction_Letter.letter = LetterMaker.MakeLetter("LetterLabelAncientShrineWarning".Translate(), "AncientShrineWarning".Translate(), LetterDefOf.NeutralEvent, new TargetInfo(mapRect.CenterCell, map, false));
                GenSpawn.Spawn(signalAction_Letter, mapRect.CenterCell, map);

                RectTrigger rectTrigger = (RectTrigger)ThingMaker.MakeThing(ThingDefOf.RectTrigger, null);
                rectTrigger.signalTag = signalTag;
                rectTrigger.Rect = mapRect.ExpandedBy(1).ClipInsideMap(map);
                rectTrigger.destroyIfUnfogged = true;
                GenSpawn.Spawn(rectTrigger, mapRect.CenterCell, map);
            }

            // also if pawns are spawned make the appropriate LordJob
            LordJob lordJob;
            if (allSpawnedPawns != null && allSpawnedPawns.Count > 0)
            {
                if (blueprint.factionSelection == FactionSelection.friendly)
                {
                    lordJob = new LordJob_AssistColony(allSpawnedPawns[0].Faction, allSpawnedPawns[0].Position);
                }
                else
                {
                    if (Rand.Value < 0.5f)
                    {
                        lordJob = new LordJob_DefendPoint(allSpawnedPawns[0].Position);
                    }
                    else
                    {
                        lordJob = new LordJob_AssaultColony(allSpawnedPawns[0].Faction, false, false, false);
                    }
                }
                LordMaker.MakeNewLord(allSpawnedPawns[0].Faction, lordJob, map, allSpawnedPawns);

                allSpawnedPawns = null;
            }
        }

        // The blueprint data isn't workable without some formatting.. 
        private string CleanUpBlueprintData(string data)
        {
            if (data.NullOrEmpty())
                return null;

            string newData = "";

            foreach (char c in data)
            {
                // allowed chars
                if (char.IsLetterOrDigit(c) || c == ',' || c == '.' || c == '#' || c == '~' || c == '?' || c == '!' || c == '-' || c == '+' || c == '*' || c == '@')
                    newData += c;
            }

            if (newData.NullOrEmpty())
                return null;
            return newData;
        }

        // 1st step: Get the TerrainDef of the position from the FloorData of the blueprint.
        private TerrainDef TryGetTerrainDefFromFloorData(MapGeneratorBlueprintDef blueprint, int itemPos)
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
        private ThingDef TryGetThingDefFromBuildingData(MapGeneratorBlueprintDef blueprint, int itemPos)
        {
            if (blueprint.buildingData == null || blueprint.buildingData.Count() - 1 < itemPos ||
                    blueprint.buildingLegend == null)
                return null;

            char keyChar = blueprint.buildingData.ElementAt(itemPos);
            string key = keyChar.ToString();
            
            if (!blueprint.buildingLegend.ContainsKey(key))
                return null;

            return DefDatabase<ThingDef>.GetNamedSilentFail( blueprint.buildingLegend[key] );
        }
        // 2nd step (b): Get the Rotation of the position from the BuildingData of the blueprint.
        private Rot4 TryGetRotationFromBuildingData(MapGeneratorBlueprintDef blueprint, int itemPos)
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
        private ThingDef TryGetItemDefFromItemData(MapGeneratorBlueprintDef blueprint, int itemPos)
        {
            if (blueprint.itemData == null || blueprint.itemData.Count() - 1 < itemPos ||
                    blueprint.itemLegend == null)
                return null;

            char keyChar = blueprint.itemData.ElementAt(itemPos);
            string key = keyChar.ToString();

            if (!blueprint.itemLegend.ContainsKey(key))
                return null;

            return DefDatabase<ThingDef>.GetNamedSilentFail( blueprint.itemLegend[key] );
        }

        // 4th step: Get the PawnKindDef of the position from the PawnData of the blueprint.
        private PawnKindDef TryGetPawnKindDefFromPawnData(MapGeneratorBlueprintDef blueprint, int itemPos)
        {
            if (blueprint.pawnData == null || blueprint.pawnData.Count() - 1 < itemPos ||
                    blueprint.pawnLegend == null)
                return null;

            char keyChar = blueprint.pawnData.ElementAt(itemPos);
            string key = keyChar.ToString();

            if (!blueprint.pawnLegend.ContainsKey(key))
                return null;

            return DefDatabase<PawnKindDef>.GetNamedSilentFail( blueprint.pawnLegend[key] );
        }

        // Clear the cell from other destroyable objects
        private bool ClearCell(IntVec3 c, Map map)
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
        private bool CheckCell(IntVec3 c, Map map)
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
        private void TrySetCellAs(IntVec3 c, Map map, ThingDef thingDef, Rot4 thingRot, ThingDef stuffDef = null, TerrainDef terrainDef = null, 
                                        PawnKindDef pawnKindDef = null,  ThingDef itemDef = null, MapGeneratorBlueprintDef blueprint = null)
        {
            //Note: Here is no functionality to clear the cell by design, because it is possible to place items that are larger than 1x1

            // Check the cell information
            if (c == null || !c.InBounds(map))
            {
                Log.Warning("GenStep_CreateBlueprint: Invalid Target-Cell: cell is null or out of bounds.");
                return;
            }

            // only continue to do work if here isn't anything indestructable
            List<Thing> thingList = c.GetThingList(map);
            for (int i = 0; i < thingList.Count; i++)
            {
                if (!thingList[i].def.destroyable)
                {
                    return;
                }
            }

            // 1st step - work with the Terrain
            if (terrainDef != null)
                map.terrainGrid.SetTerrain(c, terrainDef);
            else if (terrainDef == null && thingDef != null && stuffDef != null)
                map.terrainGrid.SetTerrain(c, this.CorrespondingTileDef(stuffDef));

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

                CompGatherSpot compGathering = newThing.TryGetComp<CompGatherSpot>();
                if (compGathering != null)
                    compGathering.Active = false;
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
                    if (!DefDatabase<ThingDef>.AllDefs.Where<ThingDef>(t => t.IsStuff &&
                                                                            t.stuffProps != null && t.stuffProps.categories != null && t.stuffProps.categories.Contains(StuffCategoryDefOf.Fabric))
                                                                                .TryRandomElement(out stuffDef2))
                    {
                        stuffDef2 = DefDatabase<ThingDef>.GetNamedSilentFail("Synthread");
                    }

                } else
                {
                    List<string> stuffPossibles = new List<string>() { "Steel", "Steel", "Steel", "Steel", "Silver", "Gold", "Jade", "Plasteel" };
                    stuffDef2 = DefDatabase<ThingDef>.GetNamedSilentFail(stuffPossibles.RandomElement());
                }

                if (!itemDef.MadeFromStuff)
                    stuffDef2 = null;

                Thing newItem = TryGetTreasure(itemDef, stuffDef2);
                newItem = GenSpawn.Spawn(newItem, c, map);
                // don't forget to forbid the item!
                newItem.SetForbidden(true, false);
            }
            

            // 4th step - work with the Pawn
            if (pawnKindDef != null && blueprint.pawnSpawnChance / 100 > Rand.Value)
            {
                if (this.faction == null)
                    this.faction = Find.FactionManager.FirstFactionOfDef(blueprint.factionDef);

                float pointsForRaid = map.IncidentPointsRandomFactorRange.RandomInRange;

                // still null - find a valid faction.
                if (this.faction == null)
                {
                    switch (blueprint.factionSelection)
                    {
                        case FactionSelection.friendly:
                            faction = (from fac in Find.FactionManager.AllFactions
                                       where !fac.HostileTo(Faction.OfPlayer) && fac.PlayerGoodwill > 0 && !(fac == Faction.OfPlayer)
                                       select fac)
                                      .RandomElementByWeight((Faction fac) => 101 - fac.def.RaidCommonalityFromPoints(pointsForRaid));

                            if (faction == null)
                                faction = Find.FactionManager.AllFactions.RandomElementByWeight((Faction fac) => fac.def.RaidCommonalityFromPoints(pointsForRaid));

                            break;

                        case FactionSelection.hostile:
                            faction = (from fac in Find.FactionManager.AllFactions
                                       where fac.HostileTo(Faction.OfPlayer)
                                       select fac)
                                      .RandomElementByWeight((Faction fac) => 101 - fac.def.RaidCommonalityFromPoints(pointsForRaid));

                            if (faction == null)
                                faction = Faction.OfMechanoids;

                            break;

                        case FactionSelection.none:
                            faction = Find.FactionManager.AllFactions
                                      .RandomElementByWeight((Faction fac) => fac.def.RaidCommonalityFromPoints(pointsForRaid));

                            if (faction == null)
                                faction = Faction.OfMechanoids;

                            break;
                    }
                }

                Pawn pawn = PawnGenerator.GeneratePawn(pawnKindDef, faction);
                pawn.mindState.Active = false;
                pawn = GenSpawn.Spawn(pawn, c, map) as Pawn;

                if (pawn != null)
                {
                    if (allSpawnedPawns == null)
                        allSpawnedPawns = new List<Pawn>();

                    allSpawnedPawns.Add(pawn);
                }
            }

        }

        private Thing TryGetTreasure(ThingDef treasureDef, ThingDef stuffDef)
        {
            Thing treasure = null;

            // make treasure
            if (treasureDef == null)
                return null;
            
            treasure = ThingMaker.MakeThing(treasureDef, stuffDef);

            // try adjust quality
            CompQuality treasureCQ = treasure.TryGetComp<CompQuality>();
            if (treasureCQ != null)
                treasureCQ.SetQuality(QualityUtility.GenerateQualityBaseGen(), ArtGenerationContext.Outsider);

            // adjust Stack to a random stack size
            if (treasure.def.stackLimit > 1)
            {
                if (treasure.def.stackLimit > 50)
                    treasure.stackCount = Rand.RangeInclusive(1, 45);
                else
                    treasure.stackCount = Rand.RangeInclusive(1, treasure.def.stackLimit);
            }

            // adjust Hitpoints (40 to Max)
            if (treasure.stackCount == 1)
                treasure.HitPoints = Rand.RangeInclusive(40, treasure.MaxHitPoints);

            return treasure;
        }


        protected ThingDef RandomWallStuff()
        {
            return (from def in DefDatabase<ThingDef>.AllDefs
                    where def.IsStuff && def.stuffProps.CanMake(ThingDefOf.Wall) && def.BaseFlammability < 0.5f && def.BaseMarketValue / def.VolumePerUnit < 15f
                    select def).RandomElement<ThingDef>();
        }

        protected TerrainDef CorrespondingTileDef(ThingDef stuffDef)
        {
            TerrainDef terrainDef = null;
            List<TerrainDef> allDefsListForReading = DefDatabase<TerrainDef>.AllDefsListForReading;
            for (int i = 0; i < allDefsListForReading.Count; i++)
            {
                if (allDefsListForReading[i].costList != null)
                {
                    for (int j = 0; j < allDefsListForReading[i].costList.Count; j++)
                    {
                        if (allDefsListForReading[i].costList[j].thingDef == stuffDef)
                        {
                            terrainDef = allDefsListForReading[i];
                            break;
                        }
                    }
                }
                if (terrainDef != null)
                {
                    break;
                }
            }
            if (terrainDef == null)
            {
                terrainDef = TerrainDefOf.Concrete;
            }
            return terrainDef;
        }
    }
}
