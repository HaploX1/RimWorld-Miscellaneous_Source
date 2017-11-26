using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed
using Verse.AI;

namespace BeeAndHoney
{

    public class WorkGiver_Honey : WorkGiver_Scanner
    {

        protected JobDef JobDef
        {
            get
            {
                return DefDatabase<JobDef>.GetNamed("HarvestHoney");
            }
        }

        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.Touch;
            }
        }

        protected CompHasGatherableBodyResource GetComp(Thing thing)
        {
            return thing.TryGetComp<CompBeeHive>();
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            List<Building> list = pawn.Map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < list.Count; i++)
            {
                if ( list[i].TryGetComp<CompBeeHive>() != null )
                    yield return list[i];
            }
            yield break;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            //if (t != null && this.GetComp(t) != null && this.GetComp(t).ActiveAndFull && pawn.CanReserve(t, 1))
            //    Log.Error("HasJobOnBeeHive!");
            return t != null && this.GetComp(t) != null && this.GetComp(t).ActiveAndFull && pawn.CanReserve(t, 1);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return new Job(this.JobDef, t);
        }
    }
}
