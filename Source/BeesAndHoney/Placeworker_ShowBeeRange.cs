using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed


namespace BeeAndHoney
{

    public class Placeworker_ShowBeeRange : PlaceWorker
    {
        public Placeworker_ShowBeeRange() { }

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            CompProperties_BeeHive comp = def.comps.Where(c => c as CompProperties_BeeHive != null).FirstOrDefault() as CompProperties_BeeHive;
            if (comp == null)
            {
                Log.Warning("Placeworker_ShowBeeRange -- comp is null!");
                return;
            }

            //Log.Error("Placeworker_ShowBeeRange -- radius: " + comp.rangeBees.ToString() + " // count of cells: " + BeeAndHoneyUtility.CalculateAllCellsInsideRadius(center, Mathf.RoundToInt(comp.rangeBees)).Count().ToString());
            List<IntVec3> cells = new List<IntVec3>(BeeAndHoneyUtility.CalculateAllCellsInsideRadius(center, Find.CurrentMap, Mathf.RoundToInt(comp.rangeThings)));
            GenDraw.DrawFieldEdges(cells, ghostCol);
        }
    }
}
