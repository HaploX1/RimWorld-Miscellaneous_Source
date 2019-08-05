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
        public string buildingMaterial = null;
        public string buildingData;
        public string floorData;
        public string pawnData;
        public string itemData;
        public bool canHaveHoles;
        public bool createTrigger;
        public string TriggerLetterLabel = null;
        public string TriggerLetterMessageText = null;
        public LetterDef TriggerLetterDef = null;
        public Dictionary<string, string> buildingLegend;
        public Dictionary<string, Rot4> rotationLegend;
        public Dictionary<string, string> floorLegend;
        public Dictionary<string, string> pawnLegend;
        public Dictionary<string, string> itemLegend;
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
