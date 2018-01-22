using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using Verse.AI;
using Verse.AI.Group;

namespace MapGenerator
{
    public class BiomeWorker_TemperateForest_UrbanRuins : BiomeWorker_TemperateForest
    {
        public override float GetScore(Tile tile)
        {
            float chance = 0.0008f;
            float baseValue = base.GetScore(tile);

            if (baseValue <= 0 || Rand.Value > chance)
                return baseValue - 1f;

            return baseValue + 0.1f;
        }
    }

    public class BiomeWorker_TemperateSwamp_UrbanRuins : BiomeWorker_TemperateSwamp
    {
        public override float GetScore(Tile tile)
        {
            float chance = 0.002f;
            float baseValue = base.GetScore(tile);

            if (baseValue <= 0 || Rand.Value > chance)
                return baseValue - 1f;

            return baseValue + 0.1f;
        }
    }

    public class BiomeWorker_TropicalRainforest_UrbanRuins : BiomeWorker_TropicalRainforest
    {
        public override float GetScore(Tile tile)
        {
            float chance = 0.0008f;
            float baseValue = base.GetScore(tile);

            if (baseValue <= 0 || Rand.Value > chance)
                return baseValue - 1f;

            return baseValue + 0.1f;
        }
    }

    public class BiomeWorker_TropicalSwamp_UrbanRuins : BiomeWorker_TropicalSwamp
    {
        public override float GetScore(Tile tile)
        {
            float chance = 0.002f;
            float baseValue = base.GetScore(tile);

            if (baseValue <= 0 || Rand.Value > chance)
                return baseValue - 1f;

            return baseValue + 0.1f;
        }
    }

    public class BiomeWorker_BorealForest_UrbanRuins : BiomeWorker_BorealForest
    {
        public override float GetScore(Tile tile)
        {
            float chance = 0.0008f;
            float baseValue = base.GetScore(tile);

            if (baseValue <= 0 || Rand.Value > chance)
                return baseValue - 1f;

            return baseValue + 0.1f;
        }
    }
    

}
