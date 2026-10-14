using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using RimWorld;
using RimWorld.Planet;
using Verse.AI;
using Verse.AI.Group;
using RimWorld.BaseGen;

namespace MapGenerator
{
    public class GenStep_CreateBlueprintBaseClass : GenStep_Scatterer
    {
        public override int SeedPart
        {
            get
            {
                return 131314176;
            }
        }

        protected static DateTime usedCells_lastChange = DateTime.UtcNow;
        protected static HashSet<IntVec3> usedCells = new HashSet<IntVec3>();

        // RimWorld 1.7: large vanilla structures (ancient complexes, gravship wreckage, orbital
        // mechhives, ...) register their footprint in MapGenerator.UsedRects and reserve it with
        // map.layoutStructureSketches. The base scatterer's CanScatterAt only rejects the sketch
        // center cell, not a whole blueprint rect, so the mod checks both itself before placing.
        protected static bool OverlapsVanillaStructure(CellRect rect, Map map)
        {
            // Layout sketches (ancient complex etc.) — same check vanilla's own
            // GenStep_Scatterer.CanScatterAt performs for a single cell, here for the full rect.
            foreach (LayoutStructureSketch sketch in map.layoutStructureSketches)
            {
                if (rect.Overlaps(sketch.layoutSketch.OccupiedRect))
                    return true;
            }

            // UsedRects (critical + non-critical large structures).
            // Note: vanilla's Verse.MapGenerator is shadowed by our namespace name, so qualify it.
            if (Verse.MapGenerator.TryGetVar<List<CellRect>>(Verse.MapGenerator.UsedRectsName, out List<CellRect> usedRects))
            {
                foreach (CellRect usedRect in usedRects)
                {
                    if (rect.Overlaps(usedRect))
                        return true;
                }
            }
            return false;
        }

        protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int count = 1)
        {
            throw new NotImplementedException();
        }
    }
}
