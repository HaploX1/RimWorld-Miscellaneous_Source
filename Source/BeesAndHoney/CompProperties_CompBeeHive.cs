using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed


namespace BeeAndHoney
{
    public class GatherableResources
    {
        public ThingDef resourceDef;
        public int resourceCount;
    }

    public class HoneySeasonMultiplicator
    {
        public Season season;
        public float multi;
    }


    public class CompProperties_BeeHive : CompProperties
    {

        public int updateTicks = 598;

        public FloatRange activeTempRange = new FloatRange(0f, 40f);
        
        public float rangeThings = 10f;
        public int thingsCountMin = 3;

        public int resourceIntervalDays = 2;
        public GatherableResources resources;

        public List<HoneySeasonMultiplicator> seasonData = new List<HoneySeasonMultiplicator>()
        {
            new HoneySeasonMultiplicator() { season=Season.Spring, multi= 1.0f },
            new HoneySeasonMultiplicator() { season=Season.Summer, multi= 1.0f },
            new HoneySeasonMultiplicator() { season=Season.Fall,   multi= 0.5f },
            new HoneySeasonMultiplicator() { season=Season.Winter, multi= -0.05f }
        };

    }
}
