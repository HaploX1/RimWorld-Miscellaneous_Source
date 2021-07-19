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
    public class Genstep_CreateBlueprintBase : GenStep_Scatterer
    {
        public bool testActive = false;

        public bool randomlyUseVanilla = true;

        private ThingDef selectedWallStuff;

        private static Dictionary<int, string> mapWorldCoord2Blueprint;

        public override int SeedPart
        {
            get
            {
                return 1401313111;
            }
        }

        protected override bool CanScatterAt(IntVec3 c, Map map)
        {
            return base.CanScatterAt(c, map) && c.Standable(map) && !c.Roofed(map) && map.reachability.CanReachMapEdge(c, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false));
        }


        protected override void ScatterAt(IntVec3 c, Map map, GenStepParams genStepParms, int stackCount = 1)
        {
            if (testActive)
                Log.Warning("Genstep_CreateBlueprintBase - Test-Mode is active!");

            Faction faction;
            if (map.info.parent == null || map.info.parent.Faction == null || map.info.parent.Faction == Faction.OfPlayer)
            {
                faction = Find.FactionManager.RandomEnemyFaction(false, false, false);
            }
            else
            {
                faction = map.info.parent.Faction;
            }

            int worldTile = -1;
            if (map != null)
                worldTile = map.Tile;

            TechLevel techlevel = faction.def.techLevel;
            // Select only blueprints with a techlevel corresponding to the faction techlevel
            IEnumerable<MapGeneratorBaseBlueprintDef> blueprint1stSelection = DefDatabase<MapGeneratorBaseBlueprintDef>.AllDefsListForReading
                .Where ((MapGeneratorBaseBlueprintDef b) => b.factionDef == null && 
                                                            b.techLevelRequired <= techlevel && (b.techLevelMax == TechLevel.Undefined || b.techLevelMax >= techlevel) );

            IEnumerable<MapGeneratorBaseBlueprintDef> blueprint2ndSelection = DefDatabase<MapGeneratorBaseBlueprintDef>.AllDefsListForReading
                .Where ((MapGeneratorBaseBlueprintDef b) => b.factionDef != null && b.factionDef == faction.def );

            float createVanillaLimit = 0.95f;
            if ( blueprint1stSelection != null && blueprint1stSelection.Count() > 0 )
            {
                if (blueprint1stSelection.Count() <= 3)
                    createVanillaLimit = 0.85f;
                else if (blueprint1stSelection.Count() <= 5)
                    createVanillaLimit = 0.80f;
                else if (blueprint1stSelection.Count() <= 7)
                    createVanillaLimit = 0.75f;
                else if (blueprint1stSelection.Count() <= 10)
                    createVanillaLimit = 0.70f;
                else if (blueprint1stSelection.Count() <= 15)
                    createVanillaLimit = 0.60f;
                else if (blueprint1stSelection.Count() <= 20)
                    createVanillaLimit = 0.50f;
                else
                    createVanillaLimit = 0.40f;
            }

            // If there are faction specific blueprints found, reduce the vanilla chance
            if (blueprint2ndSelection.Count() > 0)
            {
                createVanillaLimit = 0.10f;

                if (testActive)
                    Log.Warning("Reduced vanilla chance: " + createVanillaLimit.ToStringPercent() + " / Faction specific blueprints found: " + faction.Name + " -> " + blueprint2ndSelection.Count());
            }

            if ( blueprint1stSelection == null || blueprint1stSelection.Count() == 0 )
                Log.Warning("Genstep_CreateBlueprintBase - no usable blueprint found. Using vanilla base generation..");


            if ( blueprint1stSelection == null || blueprint1stSelection.Count() == 0 || 
                ( !testActive && randomlyUseVanilla && Rand.Value < createVanillaLimit && 
                  (mapWorldCoord2Blueprint == null || !mapWorldCoord2Blueprint.ContainsKey(worldTile))
                ))
            {
                // No blueprint for this faction techlevel found?
                // Use basic base builder code instead!
                Core_ScatterAt(c, map, genStepParms, stackCount);
                return;
            }

            MapGeneratorBaseBlueprintDef blueprint;
            if (blueprint2ndSelection.Count() > 0)
            {
                blueprint = blueprint2ndSelection.RandomElementByWeight((MapGeneratorBaseBlueprintDef b) => b.chance);

            }
            else
            {
                blueprint = blueprint1stSelection.RandomElementByWeight((MapGeneratorBaseBlueprintDef b) => b.chance);
            }

            // Check if this position was already used -> re-use old blueprint 
            if (mapWorldCoord2Blueprint == null)
                mapWorldCoord2Blueprint = new Dictionary<int, string>();
            if (mapWorldCoord2Blueprint.ContainsKey(worldTile))
            {
                MapGeneratorBaseBlueprintDef newBlueprint = DefDatabase<MapGeneratorBaseBlueprintDef>.GetNamedSilentFail(mapWorldCoord2Blueprint[worldTile]);
                if (newBlueprint != null && newBlueprint.techLevelRequired <= faction.def.techLevel && newBlueprint.techLevelMax >= faction.def.techLevel)
                    blueprint = newBlueprint;
            }
            else if (worldTile != -1)
                mapWorldCoord2Blueprint.Add(worldTile, blueprint.defName);

            // place the blueprint
            BlueprintHandler.CreateBlueprintAt(c, map, blueprint, faction, ref selectedWallStuff, ref usedSpots);

            // reset
            selectedWallStuff = null;

            // Add a message to honor the creator
            if (!blueprint.createdBy.NullOrEmpty()) {
                string label = "MapGenerator_FactionBase_Header_ProvidedBy".Translate();
                string text = "MapGenerator_FactionBase_Body_ProvidedBy".Translate(blueprint.createdBy);
                Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, new GlobalTargetInfo(c, map));
            }
        }


        // This is core code, that will be used if there isn't a blueprint available for the required tech level of the faction
        // Original from RimWorld.GenStep_FactionBase 

        private static readonly IntRange FactionBaseSizeRange = new IntRange(22, 23);

        protected void Core_ScatterAt(IntVec3 c, Map map, GenStepParams genStepParms, int stackCount = 1)
        {

            GenStep_Settlement gs = new GenStep_Settlement();

            gs.ForceScatterAt(c, map);

            //gs.ReflectCall("ScatterAt", c, map, genStepParms, stackCount);
            
            return;
        }


    }
}
