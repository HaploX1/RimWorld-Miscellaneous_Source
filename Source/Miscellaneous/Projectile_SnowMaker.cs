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
            GenExplosion.DoExplosion(base.Position, base.Map, this.def.projectile.explosionRadius, this.def.projectile.damageDef, this.launcher, null, def, this.equipmentDef, null, 0f, 1, false, null, 0f, 1);
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
            GenExplosion.DoExplosion(pos, base.Map, radius, DamageDefOf.Frostbite, this.launcher, null, def, this.equipmentDef, null, 0f, 1, false, null, 0f, 1);

            float depth = 3f;
            IEnumerable<IntVec3> iceCells = GenRadial.RadialPatternInRadius(radius);
            foreach (IntVec3 cell in iceCells)
            {
                Map.snowGrid.AddDepth(cell + pos, depth);
            }
        }
    }
}
