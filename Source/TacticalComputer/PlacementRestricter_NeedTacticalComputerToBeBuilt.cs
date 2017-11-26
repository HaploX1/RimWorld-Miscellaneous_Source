using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed
using Verse.AI; // Needed when you do something with the AI
//using Verse.Sound; // Needed when you do something with the Sound

namespace TacticalComputer
{
    /// <summary>
    /// This placement restricter allows for the Thing only to be placed, when there is a PawnScanner found
    /// </summary>
    public class PlaceWorker_NeedTacticalComputerToBeBuilt : PlaceWorker
    {
        private string txtNoTacticalComputerFound = "TacticalComputer_PlacementRestricter_NoTacticalComputerFound";


        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc,  Rot4 rot, Thing thingToIgnore = null)
        {
            AcceptanceReport acceptanceReport;

            IEnumerable<Building_TacticalComputer> foundScanners = base.Map.listerBuildings.AllBuildingsColonistOfClass<Building_TacticalComputer>();

            if (foundScanners != null && foundScanners.Count() > 0)
            {
                acceptanceReport = true;
                return acceptanceReport;
            }

            return txtNoTacticalComputerFound.Translate();
        }
    }

}
