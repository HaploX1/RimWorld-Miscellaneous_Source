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

        protected override void ScatterAt(IntVec3 loc, Map map, int count = 1)
        {
            throw new NotImplementedException();
        }
    }
}
