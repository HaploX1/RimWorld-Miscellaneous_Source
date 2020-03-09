using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace TrainingFacility
{
    public class PlaceWorker_MartialArtsTarget : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            //JoyGiverDef joyGiverDef = DefDatabase<JoyGiverDef>.GetNamed("PracticeMartialArts");
            
            GenDraw.DrawFieldEdges(Utility_PositionFinder.FindAllWatchBuildingCells(center, Find.CurrentMap, def.rotatable, rot, def.building.watchBuildingStandDistanceRange).ToList<IntVec3>(), ghostCol);
        }
    }
}
