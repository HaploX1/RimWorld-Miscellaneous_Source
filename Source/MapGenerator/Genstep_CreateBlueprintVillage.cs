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


        protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams genStepParms, int stackCount = 1)
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

                // Random orientation for this ruin: 0, 90, 180 or 270 degrees clockwise.
                // Non-square blueprints swap their footprint, so the rotated size is used
                // from here on and the data grid is rotated cell by cell below.
                int quarterTurns = Rot4.Random.AsInt;
                IntVec2 placementSize = RotatedSize(blueprint.size, quarterTurns);

                // set the ruinDistance to the size of the blueprint
                if (placementSize.x > placementSize.z)
                    ruinDistanceRange.min = placementSize.x / 2 + 1;
                else
                    ruinDistanceRange.min = placementSize.z / 2 + 1;

                if (ruinDistanceRange.min > ruinDistanceRange.max)
                    ruinDistanceRange.max = ruinDistanceRange.min + 4;


                IntVec3 workLoc = TryFindValidScatterCellNear(loc, map, blueprint, usedCells, placementSize);
                if (workLoc != IntVec3.Invalid)
                {
                    // place a blueprint ruin
                    ScatterBlueprintAt(workLoc, map, blueprint, ref selectedWallStuff, usedSpots, quarterTurns, placementSize);
                }

                ruinCountDown--;
            }

            // reset
            selectedWallStuff = null;
            //usedCells.Clear();
        }


        protected override bool CanScatterAt(IntVec3 loc, Map map)
        {
            // KeepsDistanceToPlayerStart is only the pre-check on the anchor cell; the
            // whole blueprint footprint is verified in IsPositionValidForBlueprint below. The
            // no-build border is deliberately NOT checked here: a stricter validator would
            // shrink the vanilla random cell search and can starve it completely.
            return base.CanScatterAt(loc, map)
                && loc.SupportsStructureType(map, TerrainAffordanceDefOf.Heavy)
                && KeepsDistanceToPlayerStart(loc, map);
        }


        // This function may need some work...
        private IntVec3 TryFindValidScatterCellNear(IntVec3 loc, Map map, MapGeneratorBlueprintDef blueprint, HashSet<IntVec3> invalidCells,
                                                    IntVec2 size)
        {
            if (usedCells.Count == 0)
            {
                // Even with no known cells the placement has to respect the border zone. Clamp
                // instead of rejecting, so the ruin is not lost.
                return ClampIntoNoBuildZone(loc, map, size);
            }

            bool allowCenter = blueprint.mapCenterBlueprint;

            int searchTry = 0;
            int searchTriesMax = 30;
            while (searchTry < searchTriesMax)
            {

                if (!allowCenter)
                {
                    // Check if the loc is near the spawn location -> move loc by -5, 0, -5
                    //IntVec2 nogoCenter = new IntVec2(map.Size.x / 2, map.Size.z / 2);
                    IntVec2 nogoCenter = new IntVec2(map.Size.x / 5, map.Size.z / 5);
                    CellRect nogoCenterRect = new CellRect(nogoCenter.x, nogoCenter.z, 1, 1);
                    nogoCenterRect = nogoCenterRect.ExpandedBy(5);
                    if (nogoCenterRect.Contains(loc))
                        loc = new IntVec3(loc.x - 5, loc.y, loc.z - 5);
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

                // Keep the ruin inside the no-build border: clamp the candidate instead of
                // rejecting it, otherwise a ruin that lands near a border is dropped entirely.
                workCell = ClampIntoNoBuildZone(workCell, map, size);
                if (workCell == IntVec3.Invalid)
                    break;

                if (workCell.InBounds(map) && 
                    CanScatterAt(workCell, map) && 
                    IsPositionValidForBlueprint(workCell, size, invalidCells, map, allowCenter))
                        return workCell;

                searchTry++;
            }
            return IntVec3.Invalid;
        }

        private bool IsPositionValidForBlueprint(IntVec3 cell, IntVec2 size, HashSet<IntVec3> invalidCells, Map map, bool allowAtPlayerStart)
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

            // RimWorld 1.7: keep the village ruins off vanilla large structures (ancient complex,
            // gravship wreckage, ...) that reserved their footprint via UsedRects / layout sketches.
            if (OverlapsVanillaStructure(workRect, map))
                return false;

            // No-build zone along the map borders: the whole footprint has to stay outside.
            if (!KeepsBorderDistance(workRect, map))
                return false;

            // Blueprints may be built up to MinDistToPlayerStart cells from the player
            // start spot, but never closer - the whole footprint is checked here. Defs with
            // mapCenterBlueprint are exempt; this step never picks such a def today, but the
            // rule is kept in one place.
            if (!allowAtPlayerStart && !KeepsDistanceToPlayerStart(workRect, map))
                return false;

            return true;
        }



        private void ScatterBlueprintAt(IntVec3 loc, Map map, MapGeneratorBlueprintDef blueprint, ref ThingDef wallStuff, List<IntVec3> listOfUsedCells,
                                        int quarterTurns, IntVec2 placementSize)
        {

            CellRect mapRect = new CellRect(loc.x, loc.z, placementSize.x, placementSize.z);

            mapRect.ClipInsideMap(map);

            // if mapRect was clipped -> the blueprint doesn't fit inside the map, end here!
            if (mapRect.Width != placementSize.x || mapRect.Height != placementSize.z)
                return;

            // Hard guarantee for the no-build zone along the map borders.
            if (!KeepsBorderDistance(mapRect, map))
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

            MakeBlueprintRoom(mapRect, map, blueprint, wallStuff, quarterTurns);
            
            if (blueprint.createTrigger)
            {
                int nextSignalTagID = Find.UniqueIDsManager.GetNextSignalTagID();
                string signalTag = "unfogAreaTriggerSignal-" + nextSignalTagID;
                SignalAction_Letter signalAction_Letter = (SignalAction_Letter)ThingMaker.MakeThing(ThingDefOf.SignalAction_Letter, null);
                signalAction_Letter.signalTag = signalTag;

                if (blueprint.TriggerLetterMessageText != null)
                {
                    if (blueprint.TriggerLetterLabel != null)
                    {
                        signalAction_Letter.letterDef = blueprint.TriggerLetterDef;
                        signalAction_Letter.letterLabelKey = blueprint.TriggerLetterLabel;
                        signalAction_Letter.letterMessageKey = blueprint.TriggerLetterMessageText;
                        signalAction_Letter.lookTargets = new GlobalTargetInfo(mapRect.CenterCell, map, false);
                    }
                    else
                    {
                        signalAction_Letter.letterDef = blueprint.TriggerLetterDef;
                        signalAction_Letter.letterLabelKey = " ";
                        signalAction_Letter.letterMessageKey = blueprint.TriggerLetterMessageText;
                        signalAction_Letter.lookTargets = new GlobalTargetInfo(mapRect.CenterCell, map);
                    }

                    GenSpawn.Spawn(signalAction_Letter, mapRect.CenterCell, map);
                }

                RectTrigger_UnfogArea rectTrigger = (RectTrigger_UnfogArea)ThingMaker.MakeThing(ThingDef.Named("RectTrigger_UnfogArea"), null);
                rectTrigger.signalTag = signalTag;
                rectTrigger.destroyIfUnfogged = true;
                rectTrigger.Rect = mapRect;

                GenSpawn.Spawn(rectTrigger, mapRect.CenterCell, map);
            }
        }


        private void MakeBlueprintRoom(CellRect mapRect, Map map, MapGeneratorBlueprintDef blueprint, ThingDef stuffDef, int quarterTurns)
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
            
            IntVec3 spawnBaseCell = new IntVec3(mapRect.Min.x, mapRect.Max.y, mapRect.Max.z);
            IntVec3 spawnCell;

            foreach (IntVec3 cell in mapRect)
            {
                // Check all cells and abort if there is something indestructible found
                if (!CheckCell(cell, map))
                    return;
            }

            // Cells of the rotated blueprint: every cell of the rotated grid points at the
            // matching cell of the authored data, so the layout rotates as one block.
            IntVec2 rotatedSize;
            int[] sourceIndex = BuildRotatedIndexMap(blueprint.size, quarterTurns, out rotatedSize);

            allSpawnedPawns = null;

            try
            {
                // Work through blueprint. Note: top-left to bottom-right
                for (int zn = 0; zn < rotatedSize.z; zn++)
                {
                    for (int x = 0; x < rotatedSize.x; x++)
                    {
                        //// map can be clipped, don't work with the clipped parts
                        //if (x > mapRect.Width - 1 || zn > mapRect.Height - 1)
                        //    continue;

                        spawnCell = spawnBaseCell + new IntVec3(x, 0, -zn);

                        int itemPos = sourceIndex[x + rotatedSize.x * zn];
                        ThingDef thingDef = TryGetThingDefFromBuildingData(blueprint, itemPos);
                        Rot4 thingRot = RotateWithBlueprint(TryGetRotationFromBuildingData(blueprint, itemPos), quarterTurns);
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
                        
                        TrySetCellAs(spawnCell, map, thingDef, thingRot, stuffDef, terrainDef, pawnKindDef, itemDef, blueprint, quarterTurns);
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
            string signalTag = null;
            if (allSpawnedPawns != null && allSpawnedPawns.Count > 0)
            {
                int nextSignalTagID = Find.UniqueIDsManager.GetNextSignalTagID();
                signalTag = "hiddenEnemyPawnsApproached-" + nextSignalTagID;

                SignalAction_Letter signalAction_Letter = (SignalAction_Letter)ThingMaker.MakeThing(ThingDefOf.SignalAction_Letter, null);
                signalAction_Letter.signalTag = signalTag;
                signalAction_Letter.letterDef = LetterDefOf.NeutralEvent;
                signalAction_Letter.letterLabelKey = "LetterLabelAncientShrineWarning";
                signalAction_Letter.letterMessageKey = "AncientShrineWarning";
                signalAction_Letter.lookTargets = new TargetInfo(mapRect.CenterCell, map, false);
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
                    if (allSpawnedPawns[0].Faction?.RelationWith(Faction.OfPlayer)?.kind == null || allSpawnedPawns[0].Faction.RelationWith(Faction.OfPlayer).kind == FactionRelationKind.Hostile)
                    {
                        allSpawnedPawns[0].Faction.SetRelation(new FactionRelation(Faction.OfPlayer, FactionRelationKind.Ally));
                    }

                    lordJob = new LordJob_AssistColony(allSpawnedPawns[0].Faction, allSpawnedPawns[0].Position);
                }
                else
                {
                    if (Rand.Value < 0.5f)
                    {
                        if (allSpawnedPawns[0].Faction?.RelationWith(Faction.OfPlayer)?.kind == null)
                        {
                            allSpawnedPawns[0].Faction.SetRelation(new FactionRelation(Faction.OfPlayer, FactionRelationKind.Hostile));
                        }

                        lordJob = new LordJob_DefendPoint(allSpawnedPawns[0].Position);
                    }
                    else
                    {
                        if (allSpawnedPawns[0].Faction?.RelationWith(Faction.OfPlayer)?.kind == null || allSpawnedPawns[0].Faction.RelationWith(Faction.OfPlayer).kind == FactionRelationKind.Ally)
                        {
                            allSpawnedPawns[0].Faction.SetRelation(new FactionRelation(Faction.OfPlayer, FactionRelationKind.Hostile));
                        }

                        lordJob = new LordJob_AssaultColony(allSpawnedPawns[0].Faction, false, false, false);
                    }
                }
                LordMaker.MakeNewLord(allSpawnedPawns[0].Faction, lordJob, map,
                                      allSpawnedPawns.Where(p => !p.RaceProps.IsMechanoid).ToList());

                // Mechanoids never join the assault/defend lord: they start dormant and wake
                // up when the ruin is approached (shrine signal) or when one of them is harmed.
                MakeMechanoidsDormant(allSpawnedPawns.Where(p => p.RaceProps.IsMechanoid).ToList(), map, signalTag);

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
                                        PawnKindDef pawnKindDef = null,  ThingDef itemDef = null, MapGeneratorBlueprintDef blueprint = null,
                                        int quarterTurns = 0)
        {
            //Note: Here is no functionality to clear the cell by design, because it is possible to place items that are larger than 1x1

            // Check the cell information
            if (c == default || !c.InBounds(map))
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

                // thingRot already carries the blueprint's quarter turns (see
                // RotateWithBlueprint), so the building faces the way the rotated layout
                // expects it to. Without rotation data the author meant north.
                if (thingRot == null || thingRot == Rot4.Invalid)
                    GenSpawn.Spawn(newThing, c, map, new Rot4(quarterTurns));
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

                // Loose items follow the blueprint as well: an item the author placed in
                // its default orientation gets the blueprint's quarter turns.
                newItem = GenSpawn.Spawn(newItem, c, map, new Rot4(quarterTurns));
                // don't forget to forbid the item!
                newItem.SetForbidden(true, false);
            }
            

            // 4th step - work with the Pawn
            if (pawnKindDef != null && blueprint.pawnSpawnChance / 100 > Rand.Value)
            {
                if (pawnKindDef.defaultFactionDef == FactionDefOf.Mechanoid)
                    this.faction = Faction.OfMechanoids;

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
                                       where fac != null && !fac.HostileTo(Faction.OfPlayer) && fac.PlayerGoodwill > 0 && !(fac == Faction.OfPlayer)
                                       select fac)
                                      .RandomElementByWeight((Faction fac) => 101 - fac.def.RaidCommonalityFromPoints(pointsForRaid));

                            if (faction == null)
                                faction = Find.FactionManager.AllFactions.RandomElementByWeight((Faction fac) => fac.def.RaidCommonalityFromPoints(pointsForRaid));

                            break;

                        case FactionSelection.hostile:
                            faction = (from fac in Find.FactionManager.AllFactions
                                       where fac != null && fac.HostileTo(Faction.OfPlayer)
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
                // Mechanoids get a sleep lord below and must be able to think for its duty, so
                // the "inactive until the lord exists" trick only applies to ordinary pawns.
                pawn.mindState.Active = pawn.RaceProps.IsMechanoid;
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
