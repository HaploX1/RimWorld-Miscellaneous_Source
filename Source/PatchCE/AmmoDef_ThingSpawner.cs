using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
//using Verse.Sound;
using RimWorld;

namespace Patches_Misc_CE
{
    public class AmmoDef_ThingSpawner : CombatExtended.AmmoDef
    {
        public ThingDef spawnDef;
    }
}
