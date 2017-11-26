using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed


namespace BeeAndHoney
{
    public class CompProperties_TimedRespawn : CompProperties
    {

        public float daysUntilRespawn = 2f;
        public ThingDef changeDef = null;

        // Outside this temperature range the timed respawn will instantly trigger
        public bool useTempRange = false;
        public IntRange goodTempRange = new IntRange(-9999, 9999);

        public int TicksToRespawn
        {
            get { return Mathf.RoundToInt(this.daysUntilRespawn * 60000f); }
        }


        public CompProperties_TimedRespawn()
        {
            this.compClass = typeof(CompTimedRespawn);
        }

        public CompProperties_TimedRespawn(float daysToRespawn)
        {
            this.daysUntilRespawn = daysToRespawn;
        }


    }
}
