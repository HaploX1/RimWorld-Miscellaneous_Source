using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace AIRobot
{
    public class X2_ThingDef_AIRobot_Building_RechargeStation : ThingDef
    {
        public string secondaryGraphicPath = "";
        public string spawnThingDef = null;
        public float rechargeEfficiency = 1.0f;
    }
}
