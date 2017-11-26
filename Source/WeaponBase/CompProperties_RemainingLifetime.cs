using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
//using Verse.Sound;
using RimWorld;
//using RimWorld.Planet;
//using RimWorld.SquadAI;


namespace WeaponBase
{
    public class CompProperties_RemainingLifetime : CompProperties
    {
        public float lifetime = 20.0f;
        public CompProperties_RemainingLifetime()
        {
            this.compClass = typeof(CompRemainingLifetime);
        }
    }
}
