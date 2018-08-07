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

        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                ThingRequest tr = new ThingRequest();
                tr.singleDef = DefDatabase<ThingDef>.GetNamed("AIPawn_ConstructionStation");
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
            return this.CanWorkThing(t) && RefuelWorkGiverUtility.CanRefuel(pawn, t, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return FillJob(pawn, t, forced);
        }





        public static Job FillJob(Pawn pawn, Thing t, bool forced = false)
        {
            Thing t2 = FindBestThing(pawn, t);
            return new Job(DefDatabase<JobDef>.GetNamed("AIPawn_FillConstructor"), t, t2);
        }
        private static Thing FindBestThing(Pawn pawn, Thing t)
        {
            Building_AIPawnConstructionStation constructor = (t as Building_AIPawnConstructionStation);

            int requiredSilver = constructor.SilverAmountRequired;
            int requiredSteel = constructor.SteelAmountRequired;
            

            Predicate<Thing> predicate = delegate (Thing x)
            {
                if (x.def == ThingDefOf.Silver || x.def == ThingDefOf.Steel)
                {
                    if (!x.IsForbidden(pawn) && pawn.CanReserve(x, 1, -1, null, false))
                        return true;
                }
                return false;
            };
            IntVec3 position = pawn.Position;
            Map map = pawn.Map;

            ThingRequest bestThingRequest;
            if (requiredSteel > 0)
                bestThingRequest = ThingRequest.ForDef(ThingDefOf.Steel);
            else
                bestThingRequest = ThingRequest.ForDef(ThingDefOf.Silver);

            PathEndMode peMode = PathEndMode.ClosestTouch;
            TraverseParms traverseParams = TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false);
            Predicate<Thing> validator = predicate;
            return GenClosest.ClosestThingReachable(position, map, bestThingRequest, peMode, traverseParams, 9999f, validator, null, 0, -1, false, RegionType.Set_Passable, false);
        }
    }
}
