using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace EndGame
{
    // TODO: Delete this, as it is no longer needed --> ModSettings
    public class CompProperties_EndGame : CompProperties
    {
        public float maxDaysActive = 30.0f;
        public IntRange ticksBetweenIncidents = new IntRange( 5 * GenDate.TicksPerHour, 20 * GenDate.TicksPerHour);

        public FloatRange raidPointsFactorRange = new FloatRange(0.8f, 3.5f);

        public float dangerLowUntilDay = 10.0f;
        public float dangerHighOnDay = 26.0f;

        public float dangerLow_RaidPointMultiplier = 0.66f;
        public float dangerHigh_RaidPointMultiplier = 1.15f;

        public List<IncidentDef> possibleIncidents = new List<IncidentDef>()
        {
            //IncidentDefOf.RaidEnemy,
        };

        public CompProperties_EndGame()
        {
            this.compClass = typeof(CompEndGame);
        }
    }
}
