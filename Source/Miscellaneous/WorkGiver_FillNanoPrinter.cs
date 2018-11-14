using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;


namespace NanoPrinter
{
    public class WorkGiver_FillNanoPrinter : WorkGiver_Scanner
    {
        private const string thingDefName = "NanoPrinter";
        private const string jobDefName = "FillNanoPrinter";

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            ThingDef thingDef = DefDatabase<ThingDef>.GetNamed(thingDefName);

            IEnumerable<Building> list = pawn.Map.listerBuildings.allBuildingsColonist.Where( b => !b.DestroyedOrNull() && b.def == thingDef && b.Spawned );
            foreach (Building b in list)
            {
                if (HasThingPossibleWork(b))
                    yield return b;
            }
            yield break;
        }
        
        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                ThingRequest tr = new ThingRequest();
                tr.singleDef = DefDatabase<ThingDef>.GetNamed(thingDefName);
                return tr;
            }
        }

        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.Touch;
            }
        }

        public virtual bool HasThingPossibleWork(Thing t)
        {
            return (t is Building_NanoPrinter) && (t as Building_NanoPrinter).status == Building_NanoPrinter.NanoPrinterStatus.Gathering;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            int count;
            Thing thing = FindBestThing(pawn, t, out count);
            return this.HasThingPossibleWork(t) && pawn.CanReserve(t) && thing != null && count > 0 && pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, Danger.Some);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return FillJob(pawn, t, forced);
        }
        
        public static Job FillJob(Pawn pawn, Thing t, bool forced = false)
        {
            Building_NanoPrinter t1 = (t as Building_NanoPrinter);
            if (t1 == null) return null;
            int count;
            Thing t2 = FindBestThing(pawn, t, out count);

            if (t2 == null)
                return null;

            IntVec3 p3 = (t as Building_NanoPrinter).collectorPos;
            Job j = new Job(DefDatabase<JobDef>.GetNamed(jobDefName), t2, t1, p3);
            j.targetQueueA = new List<LocalTargetInfo>();
            j.targetQueueA.Add(t2);
            j.count = count;
            return j;
        }

        private static Thing FindBestThing(Pawn pawn, Thing t, out int count)
        {
            Building_NanoPrinter building = (t as Building_NanoPrinter);

            count = 0;

            if (building == null)
                return null;

            Dictionary<ThingDef, int> requiredMaterial = building.neededMaterial;
            
            foreach (ThingDef tDef in requiredMaterial.Keys)
            {
                Predicate<Thing> validator = delegate (Thing x)
                {
                    if (x.def == tDef)
                        if (!x.IsForbidden(pawn) && pawn.CanReserve(x, 1, -1, null, false))
                            return true;

                    return false;
                };
                IntVec3 position = pawn.Position;
                Map map = pawn.Map;

                ThingRequest bestThingRequest = ThingRequest.ForDef(tDef);

                PathEndMode peMode = PathEndMode.ClosestTouch;
                TraverseParms traverseParams = TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false);
                Thing foundThing = GenClosest.ClosestThingReachable(position, map, bestThingRequest, peMode, traverseParams, 9999f, validator, null, 0, -1, false, RegionType.Set_Passable, false);

                if (foundThing == null)
                    continue;

                count = requiredMaterial[tDef];

                return foundThing;
            }
            return null;
        }
    }
}
