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
    public class ThingDef_Building_RechargeStation : ThingDef
    {
        public string secondaryGraphicPath = "";
        public string medicalGraphicPath = "";
        public string medicalSecondaryGraphicPath = "";
        public string uiButtonForceSleepPath = "";
    }
}
