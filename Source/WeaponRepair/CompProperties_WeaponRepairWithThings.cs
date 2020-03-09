using RimWorld;
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
        public float repairPercentage = 0.20f;

        public Dictionary<QualityCategory, List<RepairCurveValues>> repairCostCurve = new Dictionary<QualityCategory, List<RepairCurveValues>>();
        
        public CompProperties_WeaponRepairWithThings()
        {
            compClass = typeof(CompWeaponRepairWithThings);
        }
    }

    public struct RepairCurveValues
    {
        public ThingDef thingDef;
        public int count;
    }

}
