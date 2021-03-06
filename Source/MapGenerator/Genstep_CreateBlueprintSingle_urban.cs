﻿using System;
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
            // New: don't do this one, if the biome is NOT XXX_UrbanRuins
            if (!map.TileInfo.biome.defName.ToLower().Contains("urbanruin"))
                return;

            base.Generate(map, parms);
        }

    }
}
