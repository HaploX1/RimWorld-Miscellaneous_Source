using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using RimWorld;


namespace EmergencyPowerCore
{
    public class CompProperties_Power_EmergencyPowerCore : CompProperties_Power
    {

        public int baseGeneratingTime;
        public int baseCoolingTime;
        public int ExplosionChance;
        public int ExplosionRadius;
        public DamageDef ExplosionDamageDef = DamageDefOf.Bomb;
        public int showEmergencyTimeInSeconds = 60;

    }
}
