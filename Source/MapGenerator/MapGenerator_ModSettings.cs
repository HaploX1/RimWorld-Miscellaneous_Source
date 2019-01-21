using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;

namespace MapGenerator
{
    public class MapGenerator_ModSettings : ModSettings
    {

        public static bool createAllNonPawnBPsWithHoles = false;
        public static float chanceForHoles = 0.15f;
        public static float chanceForHolesOnWater = 0.35f;

        public static float chance4UrbanCitiesMultiplier = 1.0f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref createAllNonPawnBPsWithHoles, "createAllNonPawnBPsWithHoles", false);
            Scribe_Values.Look<float>(ref chanceForHoles, "chanceForHoles");
            Scribe_Values.Look<float>(ref chanceForHolesOnWater, "chanceForHolesOnWater");

            Scribe_Values.Look<float>(ref chance4UrbanCitiesMultiplier, "chance4UrbanCitiesMultiplier");
        }
        
    }
}
