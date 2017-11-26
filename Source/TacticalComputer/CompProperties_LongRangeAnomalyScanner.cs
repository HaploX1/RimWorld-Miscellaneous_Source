using System;
using System.Collections.Generic;
using Verse;

namespace TacticalComputer
{
    public class CompProperties_LongRangeAnomalyScanner : CompProperties
    {
        public float chanceForNoSitePart = 0.35f;
        public float radius = 20f;
        public float mtbDays = 40f;

        public ResearchProjectDef researchSensorsDef = null;

        public CompProperties_LongRangeAnomalyScanner()
        {
            this.compClass = typeof(CompLongRangeAnomalyScanner);
        }
    }
}
