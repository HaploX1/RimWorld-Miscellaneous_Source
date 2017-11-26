using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MapGenerator
{
    public class MapGeneratorBlueprintDef : Def
    {
        public bool mapCenterBlueprint = false;

        public float chance = 1;
        public IntVec2 size;
        public ThingDef buildingMaterial = null;
        public string buildingData;
        public string floorData;
        public string pawnData;
        public string itemData;
        public bool canHaveHoles;
        public bool createTrigger;
        public string TriggerLetterLabel = null;
        public string TriggerLetterMessageText = null;
        public LetterDef TriggerLetterDef = LetterDefOf.BadNonUrgent;
        public Dictionary<string, ThingDef> buildingLegend;
        public Dictionary<string, Rot4> rotationLegend;
        public Dictionary<string, TerrainDef> floorLegend;
        public Dictionary<string, PawnKindDef> pawnLegend;
        public Dictionary<string, ThingDef> itemLegend;
        public float pawnSpawnChance = 0;
        public float itemSpawnChance = 0;
        public FactionDef factionDef = null;
        public FactionSelection factionSelection = FactionSelection.none;

    }

    public enum FactionSelection
    {
        none,
        hostile,
        friendly
    }
}
