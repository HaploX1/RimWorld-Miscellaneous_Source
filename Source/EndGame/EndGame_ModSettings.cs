using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;

namespace EndGame
{
    public class EndGame_ModSettings : ModSettings
    {
        public static int maxDaysActive = 30;

        public static IntRange ticksBetweenIncidents = new IntRange(30000, 120000);
        public static IntRange raidPointsFactorRangePercent = new IntRange(50, 300);

        public static int dangerLowUntilDay = 7;
        public static int dangerHighFromDay = 24;

        public static float dangerLowTimeFactor = 2.0f;
        public static float dangerMidTimeFactor = 1.0f;
        public static float dangerHighTimeFactor = 0.5f;

        public static float dangerLowRaidPointFactor = 0.65f;
        public static float dangerMidRaidPointFactor = 1.0f;
        public static float dangerHighRaidPointFactor = 1.35f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref maxDaysActive, "maxDaysActive", 30, false);
            Scribe_Values.Look<IntRange>(ref ticksBetweenIncidents, "ticksBetweenIncidents", new IntRange(30000, 120000), false);
            Scribe_Values.Look<IntRange>(ref raidPointsFactorRangePercent, "raidPointsFactorRangePercent", new IntRange(50, 300), false);

            Scribe_Values.Look<int>(ref dangerLowUntilDay, "dangerLowUntilDay", 7, false);
            Scribe_Values.Look<int>(ref dangerHighFromDay, "dangerHighFromDay", 24, false);

            Scribe_Values.Look<float>(ref dangerLowTimeFactor, "dangerLowTimeFactor", 2.0f);
            Scribe_Values.Look<float>(ref dangerMidTimeFactor, "dangerMidTimeFactor", 1.0f);
            Scribe_Values.Look<float>(ref dangerHighTimeFactor, "dangerHighTimeFactor", 0.5f);

            Scribe_Values.Look<float>(ref dangerLowRaidPointFactor, "dangerLowRaidPointFactor", 0.65f);
            Scribe_Values.Look<float>(ref dangerMidRaidPointFactor, "dangerMidRaidPointFactor", 1.0f);
            Scribe_Values.Look<float>(ref dangerHighRaidPointFactor, "dangerHighRaidPointFactor", 1.35f);
        }
        
    }
}
