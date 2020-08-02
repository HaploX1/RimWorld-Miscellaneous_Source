using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed
using Verse.AI; // Needed when you do something with the AI
using RimWorld.Planet;
//using Verse.Sound; // Needed when you do something with the Sound

namespace ArtefactFound
{
    public class IncidentWorker_ArtefactFound : IncidentWorker
    {
        // The ThingDef of the building (see XML)
        private string thingDefName_BuildingArtefact = "ArtefactFound_Artefact";

        // The Letter text from the translation
        private string letterArtefactWasFound = "ArtefactFound_ArtefactWasFound";
        private string labelLetterArtefactWasFound = "ArtefactFound_LabelLetterArtefact";

        /// <summary>
        /// Check, if the storyteller can use this
        /// </summary>
        /// <returns></returns>
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms))
                return false;

            Map map = (Map)parms.target;
            return map.mapPawns.ColonistCount >= 4;
        }
        

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            // Find random free colonist (Who's outside)
            IEnumerable<Pawn> pawns = map.mapPawns.FreeColonistsSpawned.Where(p => p.GetRoom() != null && p.GetRoom().TouchesMapEdge); // Radar.IsOutdoors( p, true ));

            if (pawns == null || pawns.Count() < 1)
                return false;

            Pawn pawn = pawns.RandomElement();

            if (pawn == null)
                return false;

            string str = letterArtefactWasFound.Translate(pawn.Name.ToString());
            string label = labelLetterArtefactWasFound.Translate();

            int invalidCounter = 20; // try to find a valid placement for max 20 times
            IntVec3 artefactPos = IntVec3.Invalid;

            // find placement near an colonist
            while (artefactPos == IntVec3.Invalid && invalidCounter > 0)
            {
                artefactPos = CellFinder.RandomClosewalkCellNear(pawn.Position, map, 2);

                if (map.thingGrid.CellContains(artefactPos, ThingCategory.Building) ||
                    map.thingGrid.CellContains(artefactPos, ThingCategory.Item) ||
                    map.thingGrid.CellContains(artefactPos, ThingCategory.Ethereal))
                {
                    artefactPos = IntVec3.Invalid;
                    invalidCounter--;
                }
            }

            // couldn't find valid placement
            if (artefactPos == IntVec3.Invalid)
                return false;

            // create artefact
            Building_Artefact buildingArtefact = (Building_Artefact)GenSpawn.Spawn(ThingDef.Named(thingDefName_BuildingArtefact), artefactPos, map);
            buildingArtefact.pointsToSpend = parms.points;

            // add game event
            Find.LetterStack.ReceiveLetter(label, str, LetterDefOf.ThreatSmall, new GlobalTargetInfo(buildingArtefact.Position, map));

            //Log.Error("Points to Spent: " + parms.threatPoints.ToString() + "// Max Squad Cost: " + parms.raidMaxSquadCost.ToString());

            return true;
        }

    }
}
