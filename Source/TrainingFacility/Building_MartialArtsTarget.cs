using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;


namespace TrainingFacility
{
    public class Building_MartialArtsTarget : Building
    {
        
        private JoyGiverDef GetJoyGiverDef()
        {
            return DefDatabase<JoyGiverDef>.GetNamed("PracticeMartialArts");
        }
        private JobDef GetJobDef()
        {
            return DefDatabase<JobDef>.GetNamed("UseMartialArtsTarget_NonJoy");
        }


        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            // do nothing if not of colony
            if (selPawn.Faction != Faction.OfPlayer)
                yield break;

            // base float menus
            foreach (FloatMenuOption fmo in base.GetFloatMenuOptions(selPawn))
                yield return fmo;

            // do shooting menu
            // ================

            // Check Reachability
            if (!selPawn.CanReserve(this, 1))
            {
                yield return new FloatMenuOption("CannotUseReserved".Translate(), null);
                yield break;
            }
            if (!selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption("CannotUseNoPath".Translate(), null);
                yield break;
            }

            // Check weapon status
            Verb attackVerb = null;
            if (selPawn != null)
                attackVerb = selPawn.TryGetAttackVerb(this, false);

            if (attackVerb != null && attackVerb.verbProps != null && attackVerb.verbProps.IsMeleeAttack)
            {
                Thing chair;
                JoyGiverDef joyGiverDef = this.GetJoyGiverDef();
                IntVec3 standCell = Utility_PositionFinder.TryFindWatchBuildingPosition(selPawn, this, this.def.building.watchBuildingStandDistanceRange, joyGiverDef.desireSit, out chair);

                Action action_PracticeMartialArts = delegate
                {
                    selPawn.drafter.Drafted = false;

                    Job job = new Job(this.GetJobDef(), this, standCell, chair);

                    if (job != null)
                        selPawn.jobs.TryTakeOrderedJob(job);
                };
                yield return new FloatMenuOption("TrainingFacility_PracticeMartialArts".Translate(), action_PracticeMartialArts);
            }
            else
            {
                yield return new FloatMenuOption("TrainingFacility_MeleeWeaponRequired".Translate(), null);
            }
        }
    }

}
