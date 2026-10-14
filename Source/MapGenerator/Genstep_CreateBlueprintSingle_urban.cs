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
    public class Genstep_CreateBlueprintSingle_urban : GenStep_CreateBlueprintSingle
    {
        public override void Generate(Map map, GenStepParams parms)
        {
            // Run only on UrbanRuins biomes (see the "Misc. MapGenerator Urban" mod):
            // that addon is what puts a lot of buildings on a city-ruin map, while the
            // sparse _normal steps take care of every other biome. A tile can carry a
            // secondary biome, so all biomes of the tile are checked.
            bool urbanRuins = false;
            foreach (BiomeDef biomeDef in map.TileInfo.Biomes)
            {
                if (biomeDef.defName.ToLower().Contains("urbanruin"))
                {
                    urbanRuins = true;
                    break;
                }
            }

            if (!urbanRuins)
                return;

            base.Generate(map, parms);
        }

    }
}
