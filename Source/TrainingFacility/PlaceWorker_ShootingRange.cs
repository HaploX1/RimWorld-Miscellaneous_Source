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
    public class PlaceWorker_ShootingRange : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot)
        {
            //JoyGiverDef joyGiverDef = DefDatabase<JoyGiverDef>.GetNamed("PracticeShooting");

            GenDraw.DrawFieldEdges(Utility_PositionFinder.FindAllWatchBuildingCells(center, Find.VisibleMap, def.rotatable, rot, def.building.watchBuildingStandDistanceRange).ToList<IntVec3>());
        }
    }
}
