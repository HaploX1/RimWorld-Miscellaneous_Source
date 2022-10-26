using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace WeaponBase
{
    public class Projectile_ThingSpawner : Bullet
    {
        public ThingDef_ThingSpawner Def
        {
            get
            {
                return this.def as ThingDef_ThingSpawner;
            }
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            base.Impact(hitThing, blockedByShield);
            
            Map map = this.launcher.Map;
            IntVec3 cell;
            RCellFinder.TryFindRandomCellNearWith(base.Position, ((IntVec3 x) => x.GetTerrain(map).affordances.Contains(TerrainAffordanceDefOf.Light) && 
                                                                                 x.GetEdifice(map) == null && x.Standable(map)),
                                                                                map, out cell, 2);

            Thing thing = GenSpawn.Spawn(this.Def.spawnDef, cell, map);
            thing.SetFactionDirect( launcher.Faction );
        }



    }
}
