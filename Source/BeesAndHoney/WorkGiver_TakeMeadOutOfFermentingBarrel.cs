using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;

namespace BeeAndHoney
{
    public class WorkGiver_TakeMeadOutOfFermentingBarrel : WorkGiver_Scanner
    {
        private ThingDef barrelMead => DefDatabase<ThingDef>.GetNamed("FermentingBarrel_Mead");

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(barrelMead);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            List<Thing> list = pawn.Map.listerThings.ThingsOfDef(barrelMead);
            for (int i = 0; i < list.Count; i++)
            {
                if (((Building_FermentingBarrel)list[i]).Fermented)
                {
                    return false;
                }
            }
            return true;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_FermentingBarrel building_FermentingBarrel = t as Building_FermentingBarrel;
            if (building_FermentingBarrel == null || !building_FermentingBarrel.Fermented)
            {
                return false;
            }
            if (t.IsBurning())
            {
                return false;
            }
            if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
            {
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("BeeAndHoney_TakeMeadOutOfFermentingBarrel"), t);
        }
    }
}
