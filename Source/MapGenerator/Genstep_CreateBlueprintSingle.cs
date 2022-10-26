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
    public class GenStep_CreateBlueprintSingle : GenStep_CreateBlueprintBaseClass
    {
        private ThingDef selectedWallStuff;
        private Faction faction;
        private List<Pawn> allSpawnedPawns;

        private static bool mapCenterBlueprintUsed = false;
        
        protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams genStepParms, int stackCount = 1)
        {
            // Once a mapcenter blueprint is placed, don't do anything more
            if (mapCenterBlueprintUsed)
                return;

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

            MapGeneratorBlueprintDef blueprint;

            // Safety: only use blueprints where the size is smaller than the map size  => Safety really needed?
            blueprint = DefDatabase<MapGeneratorBlueprintDef>.AllDefsListForReading
                .RandomElementByWeight((MapGeneratorBlueprintDef b) => b.chance);


            if (!blueprint.mapCenterBlueprint && blueprint.pawnLegend != null && blueprint.pawnLegend.Count > 0)
            {
                // Check if the loc is near the spawn location
                //IntVec2 nogoCenter = new IntVec2((int)(Find.World.info.initialMapSize.x / 2.5f), (int)(Find.World.info.initialMapSize.z / 2.5f));
                IntVec2 nogoCenter = new IntVec2((int)(Find.World.info.initialMapSize.x / 10f), (int)(Find.World.info.initialMapSize.z / 10f));
                CellRect nogoCenterRect = new CellRect(nogoCenter.x, nogoCenter.z, 1, 1);
                //nogoCenterRect = nogoCenterRect.ExpandedBy(20);
                nogoCenterRect = nogoCenterRect.ExpandedBy(5);
                if (nogoCenterRect.Contains(loc))
                {
                    // If loc is near the center, find new blueprint that doesn't contain any pawns
                    blueprint = DefDatabase<MapGeneratorBlueprintDef>.AllDefsListForReading
                        .Where(b => (b.pawnLegend == null || b.pawnLegend.Count == 0))
                        .RandomElementByWeight((MapGeneratorBlueprintDef b) => b.chance);
                }
            }
            if (blueprint == null)
                return;

            // if the blueprint is a map center blueprint, set the loc so that the center is at the map center
            if (blueprint.mapCenterBlueprint)
            {
                loc = new IntVec3(map.Center.x - (blueprint.size.x / 2), map.Center.y, map.Center.z - (blueprint.size.z / 2));
                mapCenterBlueprintUsed = true;
            }

            // place a blueprint ruin
            //ScatterBlueprintAt(loc, map, blueprint, ref selectedWallStuff, this.usedSpots);
            try
            {
                ScatterBlueprintAt(loc, map, blueprint, ref selectedWallStuff, usedCells);
            }
            catch (Exception err)
            {
                Log.Error("Misc. MapGenerator -- Could not spawn blueprint '" + blueprint.defName + "'. Error: " + err.Message + "\n" + err.StackTrace);
            }

            // reset
            selectedWallStuff = null;
        }


        protected override bool CanScatterAt(IntVec3 loc, Map map)
        {
            try
            {
                return base.CanScatterAt(loc, map) && loc.SupportsStructureType(map, TerrainAffordanceDefOf.Heavy);
            }
            catch (Exception err)
            {
                Log.Warning("Misc. MapGenerator -- Caught error while checking CanScatterAt() -> " + err.Message + "\n" + err.StackTrace);
            }
            return false;
        }


        private void ScatterBlueprintAt(IntVec3 loc, Map map, MapGeneratorBlueprintDef blueprint, ref ThingDef wallStuff, HashSet<IntVec3> listOfUsedCells)
        {

            CellRect mapRect = new CellRect(loc.x, loc.z, blueprint.size.x, blueprint.size.z);
            CellRect mapRectCheck4Neighbor = mapRect.ExpandedBy(1);

            mapRect.ClipInsideMap(map);

            // if mapRect was clipped -> the blueprint doesn't fit inside the map...
            if (mapRect.Width != blueprint.size.x || mapRect.Height != blueprint.size.z)
                return;

            // Check if we will build on a usedCell
            bool usedCellFound = false;
            foreach (IntVec3 cell in mapRectCheck4Neighbor.Cells) //mapRect.Cells)
            {
                if (listOfUsedCells != null && listOfUsedCells.Contains(cell))
                {
                    usedCellFound = true;
                    break;
                }
            }
            if (usedCellFound)
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
                // Don't do anything if there is a pawn (mechanoid? insect?) here
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                    if (pawn != null && pawn.Spawned && pawn.Position == current)
                        return;
                usedSpots.Add(current); // prevent the base scatterer to use this spot
                usedCells.Add(current);
                usedCells_lastChange = DateTime.UtcNow;
            }

            // Remove this blueprints map cells from the unfogging list
            List<IntVec3> rootsToUnfog = Verse.MapGenerator.rootsToUnfog;
            foreach (IntVec3 cell in mapRect.Cells)
            {
                if (rootsToUnfog != null && rootsToUnfog.Contains(cell))
                    rootsToUnfog.Remove(cell);
            }

            // If a building material is defined, use this
            if (blueprint.buildingMaterial != null && blueprint.buildingMaterial != "")
                wallStuff = DefDatabase<ThingDef>.GetNamedSilentFail( blueprint.buildingMaterial );
         
            int w = 0;
            while (true)
            {
                w++;
                if (w > 1000) break;

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
                string signalTag = "unfogTriggerSignal-" + nextSignalTagID;
                SignalAction_Letter signalAction_Letter = (SignalAction_Letter)ThingMaker.MakeThing(ThingDefOf.SignalAction_Letter, null);
                signalAction_Letter.signalTag = signalTag;

                if (blueprint.TriggerLetterMessageText != null)
                {
                    if (blueprint.TriggerLetterLabel != null)
                        signalAction_Letter.letter = LetterMaker.MakeLetter(blueprint.TriggerLetterLabel.Translate(), blueprint.TriggerLetterMessageText.Translate(), blueprint.TriggerLetterDef, new GlobalTargetInfo(mapRect.CenterCell, map, false));
                    else
                        signalAction_Letter.letter = LetterMaker.MakeLetter("", blueprint.TriggerLetterMessageText.Translate(), blueprint.TriggerLetterDef, new GlobalTargetInfo(mapRect.CenterCell, map));

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
                Log.ErrorOnce(string.Format("Misc. MapGenerator -- After cleaning the BlueprintData and FloorData of blueprint {0} -> both are null, nothing will be done!", blueprint.defName), 313001);
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
                        {
                            ClearCell(spawnCell, map);
                        }

                        if ((blueprint.canHaveHoles ||
                            (MapGenerator_ModSettings.createAllNonPawnBPsWithHoles && (blueprint.pawnLegend == null || blueprint.pawnLegend.Count <= 0))) &&
                            Rand.Value < MapGenerator_ModSettings.chanceForHoles)
                            {
                            continue;
                        }
                        
                        // If placed on water, increase the hole chance, if no pawns are to be placed!
                        if (spawnCell.GetTerrain(map).defName.ToLower().Contains("water") && 
                            (blueprint.pawnLegend == null || blueprint.pawnLegend.Count <= 0) && 
                            Rand.Value < MapGenerator_ModSettings.chanceForHolesOnWater)
                        {
                            continue;
                        }

                        TrySetCellAs(spawnCell, map, thingDef, thingRot, stuffDef, terrainDef, pawnKindDef, itemDef, blueprint);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Misc. MapGenerator -- Error with blueprint '" + blueprint.defName + "'. Placement position at " + 
                                mapRect.CenterCell.ToString() + " on a map of the size " + map.Size.ToString() + "\n" +
                                ex.Message + "\n" + ex.StackTrace);
            }


            // If pawns are spawned, place ancient shrine trigger
            if (allSpawnedPawns != null && allSpawnedPawns.Count > 0)
            {
                int nextSignalTagID = Find.UniqueIDsManager.GetNextSignalTagID();
                string signalTag = "ancientTempleApproached-" + nextSignalTagID;
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
                //Log.Error("Spawned faction: "+ allSpawnedPawns[0].Faction.ToString());
                try
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
                            lordJob = new LordJob_AssaultColony(allSpawnedPawns[0].Faction, false, false, false, false, false);
                        }
                    }
                    LordMaker.MakeNewLord(allSpawnedPawns[0].Faction, lordJob, map, allSpawnedPawns);

                }
                catch (Exception ex)
                {
                    Log.Warning("Misc. MapGenerator -- Error with LordMaker for blueprint '" + blueprint.defName + "'." + 
                        "\n" + ex.Message + "\n" + ex.StackTrace);
                }

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
                // only use allowed chars
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

            return DefDatabase<TerrainDef>.GetNamedSilentFail(blueprint.floorLegend[key]);
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

            return DefDatabase<ThingDef>.GetNamedSilentFail(blueprint.buildingLegend[key]);
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

            return DefDatabase<ThingDef>.GetNamedSilentFail(blueprint.itemLegend[key]);
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

            return DefDatabase<PawnKindDef>.GetNamedSilentFail(blueprint.pawnLegend[key]);
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

        private bool CheckCellForThing(IntVec3 c, Map map, ThingDef thingDef)
        {
            List<Thing> thingList = c.GetThingList(map);
            for (int i = 0; i < thingList.Count; i++)
            {
                if (thingList[i].def == thingDef)
                {
                    return true;
                }
            }
            return false;
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
                if ( newItem.TryGetComp<CompForbiddable>() != null) 
                    newItem.SetForbidden(true, false);

                // If it is a hive, it needs to be deactivated
                Hive newHive = newItem as Hive;
                //if (newHive != null)
                //{
                //    newHive..active = false;
                //}
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

            // adjust Hitpoints (40% to 100%)
            if (treasure.stackCount == 1)
                treasure.HitPoints = Rand.RangeInclusive((int)(treasure.MaxHitPoints * 0.4), treasure.MaxHitPoints);

            return treasure;
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
