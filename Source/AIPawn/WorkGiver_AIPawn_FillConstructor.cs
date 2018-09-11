using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;


namespace AIPawn
{
    public class WorkGiver_AIPawn_FillConstructor : WorkGiver_Scanner
    {
        private string thingDefName = "AIPawn_ConstructionStation"; 

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            ThingDef thingDef = DefDatabase<ThingDef>.GetNamed(thingDefName);

            IEnumerable<Building> list = pawn.Map.listerBuildings.allBuildingsColonist.Where(b => b.def == thingDef && !b.DestroyedOrNull() && b.Spawned );
            foreach (Building b in list)
            {
                if ((b as Building_AIPawnConstructionStation).gatheringSuppliesActive)
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

        public virtual bool CanWorkThing(Thing t)
        {
            return (t is Building_AIPawnConstructionStation) && (t as Building_AIPawnConstructionStation).gatheringSuppliesActive;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            int count;
            Thing thing = FindBestThing(pawn, t, out count);
            return this.CanWorkThing(t) && pawn.CanReserve(t) && thing != null && count > 0 && pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, Danger.Some);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return FillJob(pawn, t, forced);
        }
        
        public static Job FillJob(Pawn pawn, Thing t, bool forced = false)
        {
            Building_AIPawnConstructionStation t1 = (t as Building_AIPawnConstructionStation);
            if (t1 == null) return null;
            int count;
            Thing t2 = FindBestThing(pawn, t, out count);

            if (t2 == null)
                return null;

            IntVec3 p3 = (t as Building_AIPawnConstructionStation).RefillPosition;
            Job j = new Job(DefDatabase<JobDef>.GetNamed("AIPawn_FillConstructor"), t2, t1, p3);
            j.targetQueueA = new List<LocalTargetInfo>();
            j.targetQueueA.Add(t2);
            j.count = count;
            return j;
        }

        private static Thing FindBestThing(Pawn pawn, Thing t, out int count)
        {
            Building_AIPawnConstructionStation constructor = (t as Building_AIPawnConstructionStation);

            count = 0;

            if (constructor == null)
                return null;

            int requiredSilver = constructor.SilverAmountRequired;
            int requiredSteel = constructor.SteelAmountRequired;

            ThingDef targetDef;
            if (requiredSteel > 0)
            {
                targetDef = ThingDefOf.Steel;
                count = requiredSteel;
            }
            else
            {
                targetDef = ThingDefOf.Silver;
                count = requiredSilver;
            }

            Predicate<Thing> validator = delegate (Thing x)
            {
                if (x.def == targetDef)
                    if (!x.IsForbidden(pawn) && pawn.CanReserve(x, 1, -1, null, false))
                        return true;

                return false;
            };
            IntVec3 position = pawn.Position;
            Map map = pawn.Map;

            ThingRequest bestThingRequest = ThingRequest.ForDef(targetDef);

            PathEndMode peMode = PathEndMode.ClosestTouch;
            TraverseParms traverseParams = TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false);
            return GenClosest.ClosestThingReachable(position, map, bestThingRequest, peMode, traverseParams, 9999f, validator, null, 0, -1, false, RegionType.Set_Passable, false);
        }
    }
}
