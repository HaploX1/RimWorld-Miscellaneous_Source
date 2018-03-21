using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blueprint2MapGenConverter
{
    public class Misc_Blueprint : IExposable
    {
        public class LegendData : IExposable
        {
            public string key;
            public string value;

            public LegendData() { }
            public LegendData(string key, string value)
            {
                this.key = key;
                this.value = value;
            }

            public void ExposeData()
            {
                Scribe_Values.LookValue<string>(ref this.key, "key", null, true);
                Scribe_Values.LookValue<string>(ref this.value, "value", null, true);
            }
        }

        public bool createMapGenFactionBaseBlueprint = true;
        
        public string defName = "unique_defName";
        public string createdBy = "username";
        public string techLevelRequired = "Neolithic";
        public int chance = 10;
        public IntVec2 size = IntVec2.Invalid;
        public bool canHaveHoles = false;
        public bool createTrigger = true;

        public string buildingData;
        public List<LegendData> buildingLegend;
        public List<LegendData> rotationLegend;

        public string floorData;
        public List<LegendData> floorLegend;

        public string itemData;
        public List<LegendData> itemLegend;
        public int itemSpawnChance = 50;

        public string pawnData;
        public List<LegendData> pawnLegend;
        public int pawnSpawnChance = 40;



        public Misc_Blueprint()
        {
            buildingLegend = new List<LegendData>();
            rotationLegend = new List<LegendData>();
            floorLegend = new List<LegendData>();
            itemLegend = new List<LegendData>();
            pawnLegend = new List<LegendData>();
        }


        public void ExposeData()
        {
            bool canHaveHoles = true;
            if (createMapGenFactionBaseBlueprint)
                canHaveHoles = false;

            Scribe_Values.LookValue<string>(ref this.defName, "defName", null, true);

            if (createMapGenFactionBaseBlueprint) // Not if not a base
                Scribe_Values.LookValue<string>(ref this.createdBy, "createdBy", null, true);
            if (createMapGenFactionBaseBlueprint) // Not if not a base
                Scribe_Values.LookValue<string>(ref this.techLevelRequired, "techLevelRequired", null, true);

            Scribe_Values.LookValue<int>(ref this.chance, "chance", -1, true);
            Scribe_Values.LookValue<IntVec2>(ref this.size, "size", IntVec2.Invalid, true);
            
            Scribe_Values.LookValue<bool>(ref this.canHaveHoles, "canHaveHoles", canHaveHoles, true);
           
            Scribe_Values.LookValue<bool>(ref this.createTrigger, "createTrigger", false, true);

            Scribe_Values.LookValue<string>(ref this.buildingData, "buildingData", null, true);
            Scribe_Collections.LookList<LegendData>(ref this.buildingLegend, "buildingLegend", LookMode.Deep, new object[] { });
            Scribe_Collections.LookList<LegendData>(ref this.rotationLegend, "rotationLegend", LookMode.Deep, new object[] { });
            Scribe_Values.LookValue<string>(ref this.floorData, "floorData", null, true);
            Scribe_Collections.LookList<LegendData>(ref this.floorLegend, "floorLegend", LookMode.Deep, new object[] { });

            Scribe_Values.LookValue<string>(ref this.itemData, "itemData", null, true);
            Scribe_Collections.LookList<LegendData>(ref this.itemLegend, "itemLegend", LookMode.Deep, new object[] { });
            Scribe_Values.LookValue<string>(ref this.pawnData, "pawnData", null, true);
            Scribe_Collections.LookList<LegendData>(ref this.pawnLegend, "pawnLegend", LookMode.Deep, new object[] { });
        }

    }
}
