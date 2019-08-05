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
        public string buildingMaterial = null;
        public string buildingData;
        public string nonbuildingData;
        public string floorData;
        public string pawnData;
        public string itemData;
        public bool canHaveHoles;
        public bool createTrigger;
        public string TriggerLetterLabel = null;
        public string TriggerLetterMessageText = null;
        public LetterDef TriggerLetterDef = null;
        public Dictionary<string, string> buildingLegend;
        public Dictionary<string, string> nonbuildingLegend;
        public Dictionary<string, Rot4> rotationLegend;
        public Dictionary<string, string> floorLegend;
        public Dictionary<string, string> pawnLegend;
        public Dictionary<string, string> itemLegend;
        public FactionDef factionDef = null;
        public float itemSpawnChance = 70;
        public float pawnSpawnChance = 70;
    }
}
