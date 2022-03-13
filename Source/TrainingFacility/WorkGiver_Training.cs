using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;


namespace TrainingFacility
{
    public class WorkGiver_Training : WorkGiver_Scanner
    {

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            IEnumerable<Building> list;
            list = pawn.Map.listerBuildings.allBuildingsColonist.Where(b => (b is Building_MartialArtsTarget || b is Building_ShootingRange) && !b.DestroyedOrNull() && b.Spawned );
            foreach (Building b in list)
            {
                if (!CanPawnWorkThisJob(pawn, b))
                    continue;

                if (!pawn.CanReserveAndReach(b, PathEndMode, Danger.Some))
                    continue;

                yield return b;
            }
            yield break;
        }

        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.InteractionCell;
            }
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return TryCreateJob(pawn, t, forced);
        }

        public bool CanPawnWorkThisJob(Pawn pawn, Thing t, bool forced = false)
        {
            JoyGiverDef def = null;
            int typeOfTarget = 0;

            if (!pawn.CanReserve(t, 1))
                return false;

            if (!pawn.CanReach(t, PathEndMode, Danger.Deadly))
                return false;

            if (t as Building_ShootingRange != null)
            {
                def = (t as Building_ShootingRange).GetJoyGiverDef();
                typeOfTarget = 1; // Shooting Range
            }
            if (t as Building_MartialArtsTarget != null)
            {
                def = (t as Building_MartialArtsTarget).GetJoyGiverDef();
                typeOfTarget = 2; // Martial Arts
            }

            if (def == null)
                return false;

            Verb attackVerb = null;
            if (pawn != null)
                attackVerb = pawn.TryGetAttackVerb(t, false);

            // Shooting range + Melee -> No Go
            if ((attackVerb == null || attackVerb.verbProps == null || attackVerb.verbProps.IsMeleeAttack) && typeOfTarget == 1)
                return false;

            // Martial Arts + Weapon -> No Go
            if (!attackVerb.verbProps.IsMeleeAttack && typeOfTarget == 2)
                return false;

            return true;
        }

        public Job TryCreateJob(Pawn pawn, Thing t, bool forced = false)
        {
            JoyGiverDef def = null;
            JobDef defj = null;

            if (t as Building_ShootingRange != null)
            {
                def = (t as Building_ShootingRange).GetJoyGiverDef();
                defj = DefDatabase<JobDef>.GetNamed("UseShootingRange_NonJoy_Work");
            }
            if (t as Building_MartialArtsTarget != null)
            {
                def = (t as Building_MartialArtsTarget).GetJoyGiverDef();
                defj = DefDatabase<JobDef>.GetNamed("UseMartialArtsTarget_NonJoy_Work");
            }

            if (!CanPawnWorkThisJob(pawn, t, forced))
                return null;

            return DoTryGiveJob(pawn, t, def, defj);
        }

        public Job DoTryGiveJob(Pawn pawn, Thing t, JoyGiverDef def, JobDef defj)
        {
            if (!WatchBuildingUtility.TryFindBestWatchCell(t, pawn, def.desireSit, out var result, out var chair))
            {
                return null;
            }
            return JobMaker.MakeJob(defj, t, result, chair);
        }
    }
}
