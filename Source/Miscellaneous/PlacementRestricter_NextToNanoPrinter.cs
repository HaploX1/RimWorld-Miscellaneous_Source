using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed
using Verse.AI; // Needed when you do something with the AI
//using Verse.Sound; // Needed when you do something with the Sound

namespace NanoPrinter
{
    /// <summary>
    /// This placement restricter allows for the NanoScanner only to be placed next to an NanoPrinter
    /// </summary>
    class PlaceWorker_NextToNanoPrinter : PlaceWorker
    {
        private string NanoPrinterDef = "NanoPrinter";
        private string txtMustPlaceNextToNanoPrinter = "NanoScanner_MustPlaceNextToNanoPrinter";

        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            for (int i = 0; i < 4; i++)
            {
                IntVec3 intVec3 = loc + GenAdj.CardinalDirections[i];
                if (intVec3.InBounds(map))
                {
                    List<Thing> thingList = intVec3.GetThingList(map);
                    for (int j = 0; j < thingList.Count; j++)
                    {
                        Thing item = thingList[j];
                        ThingDef thingDef = GenConstruct.BuiltDefOf(item.def) as ThingDef;
                        if (thingDef != null && thingDef.building != null)
                        {
                            if (thingDef.defName == NanoPrinterDef)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return txtMustPlaceNextToNanoPrinter.Translate();
        }
    }

}
