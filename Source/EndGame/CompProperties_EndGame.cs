using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace EndGame
{
    public class CompProperties_EndGame : CompProperties
    {
        public float maxDaysActive = 30.0f;
        public IntRange ticksBetweenIncidents = new IntRange( 5 * 2500, 20 * 2500 );

        public FloatRange raidPointsFactorRange = new FloatRange(0.8f, 3.5f);

        public float dangerIncreaseOnDay = 20.0f;

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
