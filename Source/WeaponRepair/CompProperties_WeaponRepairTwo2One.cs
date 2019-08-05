using System;
using System.Collections.Generic;
using Verse;

namespace WeaponRepair
{
    public class CompProperties_WeaponRepairTwo2One : CompProperties
    {
        public List<ThingDef> worktableDefs = new List<ThingDef>();
        public WorkTypeDef workTypeDef;
        public bool compareQuality = true;
        public JobDef jobDef;
        public float maxRepair = 0.95f;


        public CompProperties_WeaponRepairTwo2One()
        {
            compClass = typeof(CompWeaponRepairTwo2One);
        }
    }
}
