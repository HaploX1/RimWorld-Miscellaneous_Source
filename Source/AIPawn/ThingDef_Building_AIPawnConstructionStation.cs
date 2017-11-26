using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace AIPawn
{
    public class ThingDef_Building_AIPawnConstructionStation : ThingDef
    {
        public int maxSteelCount = -1;
        public int maxSilverCount = -1;
        public int counterUsingResources = -1;
        public string UI_StartProduction_Path = null;
        public string UI_StopProduction_Path = null;

    }
}
