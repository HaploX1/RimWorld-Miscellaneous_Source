using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MapGenerator
{
    public class MapGeneratorBaseBlueprintDef : Def
    {
        public bool mapCenterBlueprint = true;

        public string createdBy;

        public TechLevel techLevelRequired = TechLevel.Undefined;
        public TechLevel techLevelMax = TechLevel.Undefined;
        public float chance = 1;
        public IntVec2 size;
        public ThingDef buildingMaterial = null;
        public string buildingData;
        public string nonbuildingData;
        public string floorData;
        public string pawnData;
        public string itemData;
        public bool canHaveHoles;
        public bool createTrigger;
        public string TriggerLetterLabel = null;
        public string TriggerLetterMessageText = null;
        public LetterDef TriggerLetterDef = LetterDefOf.BadNonUrgent;
        public Dictionary<string, ThingDef> buildingLegend;
        public Dictionary<string, ThingDef> nonbuildingLegend;
        public Dictionary<string, Rot4> rotationLegend;
        public Dictionary<string, TerrainDef> floorLegend;
        public Dictionary<string, PawnKindDef> pawnLegend;
        public Dictionary<string, ThingDef> itemLegend;
        public FactionDef factionDef = null;
        public float itemSpawnChance = 70;
        public float pawnSpawnChance = 70;
    }
}
