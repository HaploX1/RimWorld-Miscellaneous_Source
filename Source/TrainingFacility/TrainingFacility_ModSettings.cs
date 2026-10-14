using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;

namespace TrainingFacility
{
    public class TrainingFacility_ModSettings : ModSettings
    {
        public static Single learningXpMultiplierShooting = 1f;
        public static Single learningXpMultiplierMelee = 1f;

        public static bool stopWorkJobOnSkillSaturation = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref learningXpMultiplierShooting, "learningXpMultiplierShooting", 1f, false);
            Scribe_Values.Look(ref learningXpMultiplierMelee, "learningXpMultiplierMelee", 1f, false);
            Scribe_Values.Look(ref stopWorkJobOnSkillSaturation, "stopWorkJobOnSkillSaturation", false, false);
        }
        
    }
}
