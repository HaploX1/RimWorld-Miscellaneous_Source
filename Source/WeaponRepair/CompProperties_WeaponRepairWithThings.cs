using System;
using System.Collections.Generic;
using Verse;

namespace WeaponRepair
{
    public class CompProperties_WeaponRepairWithThings : CompProperties
    {
        public List<ThingDef> worktableDefs = new List<ThingDef>();
        public WorkTypeDef workTypeDef;
        public JobDef jobDef;
        public float maxRepair = 0.90f;
        public float repairRatioPrice2Thing = 500.0f;
        public ThingDef repairThingDef = null;

        public CompProperties_WeaponRepairWithThings()
        {
            compClass = typeof(CompWeaponRepairWithThings);
        }
    }
}
