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


namespace TurretWeaponBase
{
    public class ThingDef_TurretWeaponBase : ThingDef
    {

        public Building_TurretWeaponBase.TopMatType usedTopGraphic = Building_TurretWeaponBase.TopMatType.BuildingMat;

        public string TopMatShortPath = null;
        public string TopMatMediumPath = null;
        public string TopMatLongPath = null;

        public float priceShortMax = 0;
        public float priceMediumMax = 0;

        public float cooldownMultiplicator = 1.5f;
        public float cooldownAddition = 5.0f;

        public string cooldownResearchName = null;
        public float cooldownResearchMultiplicator = 1.5f;
        public float cooldownResearchAddition = 4.0f;
    }
}
