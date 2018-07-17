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
    public class Genstep_CreateBlueprintVillage_normal : GenStep_CreateBlueprintVillage
    {
        public override void Generate(Map map, GenStepParams parms)
        {
            // New: don't do this one, if the biome is XXX_UrbanRuins
            if (map.TileInfo.biome.defName.ToLower().Contains("urbanruins"))
                return;

            base.Generate(map, parms);
        }

    }
}
