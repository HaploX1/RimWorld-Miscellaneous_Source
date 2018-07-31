using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;


namespace Miscellaneous
{
    public class Projectile_SnowMaker : Projectile
    {
        protected override void Impact(Thing hitThing)
        {
            base.Impact(hitThing);

            ThingDef def = this.def;
            GenExplosion.DoExplosion(Position, Map, this.def.projectile.explosionRadius, this.def.projectile.damageDef, launcher, -1, -1, null, def, this.equipmentDef, null, null, 0f, 1, false, null, 0f, 1, 0f, false);
            CellRect cellRect = CellRect.CenteredOn(base.Position, 3);
            cellRect.ClipInsideMap(base.Map);
            for (int i = 0; i < 5; i++)
            {
                IntVec3 randomCell = cellRect.RandomCell;
                this.IceExplosion(randomCell, 2.9f);
            }
        }

        protected void IceExplosion(IntVec3 pos, float radius)
        {
            ThingDef def = this.def;
            GenExplosion.DoExplosion(Position, Map, radius, DamageDefOf.Frostbite, launcher, -1, -1, null, def, this.equipmentDef, null, null, 0f, 1, false, null, 0f, 1, 0f, false);

            float depth = 3f;
            IEnumerable<IntVec3> iceCells = GenRadial.RadialPatternInRadius(radius);
            foreach (IntVec3 cell in iceCells)
            {
                Map.snowGrid.AddDepth(cell + pos, depth);
            }
        }
    }
}
