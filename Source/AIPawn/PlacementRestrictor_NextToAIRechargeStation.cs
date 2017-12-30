using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed
using Verse.AI; // Needed when you do something with the AI
//using Verse.Sound; // Needed when you do something with the Sound

namespace AIPawn
{
    /// <summary>
    /// This placement restricter allows for the NanoScanner only to be placed next to an NanoPrinter
    /// </summary>
    public class PlacementRestrictor_NextToAIRechargeStation : PlaceWorker
    {
        private string NextToThingDef = "AIPawn_RechargeStation";
        private string NextToThingDef2 = "AIPawn_RechargeStation2x";
        private string txtMustPlaceNextToAIRechargeStation = "AIPawn_MustPlaceNextToAIPawnRechargeStation";

        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 cell, Rot4 rot, Map map, Thing thingToIgnore = null)
        {
            for (int i = 0; i < 4; i++)
            {
                IntVec3 intVec3 = cell + GenAdj.CardinalDirections[i];
                if (intVec3.InBounds(map))
                {
                    List<Thing> thingList = intVec3.GetThingList(map);
                    for (int j = 0; j < thingList.Count; j++)
                    {
                        Thing item = thingList[j];
                        if (item != null)
                        {
                            if (item.def.defName == NextToThingDef || item.def.defName == NextToThingDef2)
                                return true;
                        }
                    }
                }
            }
            return txtMustPlaceNextToAIRechargeStation.Translate();
        }
    }

}

