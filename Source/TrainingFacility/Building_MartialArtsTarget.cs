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
        
        private bool repairingDisabled = false;
        private readonly string textRepairing = "Text_Building_Repairing_RepairingHomezone";
        private readonly string textRepairing_disabled = "Text_Building_Repairing_disabled";
        private readonly string textRepairing_enabled = "Text_Building_Repairing_enabled";

        public JoyGiverDef GetJoyGiverDef()
        {
            return DefDatabase<JoyGiverDef>.GetNamed("PracticeMartialArts");
        }
        public JobDef GetJobDef()
        {
            return DefDatabase<JobDef>.GetNamed("UseMartialArtsTarget_NonJoy");
        }


        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (!respawningAfterLoad)
                LongEventHandler.ExecuteWhenFinished(Setup_Part2);
        }

        private void Setup_Part2()
        {
            repairingDisabled = !this.Map.areaManager.Home[this.Position];

            // Check if we are the first of the training kind
            foreach (Building b in Map.listerBuildings.allBuildingsColonist)
            {
                if (b == this)
                    continue;

                if (b is Building_MartialArtsTarget || b is Building_ShootingRange)
                    return;
            }

            // First: Disable Worktype 'Training' for all pawns
            WorkTypeDef workTypeDef = DefDatabase<WorkTypeDef>.GetNamedSilentFail("MiscTraining_CombatTraining");
            if (workTypeDef == null)
                return;

            List<Pawn> pawns = this.Map.mapPawns.AllPawns;
            foreach (Pawn pawn in pawns)
            {
                if (pawn.DestroyedOrNull() || !pawn.Spawned || pawn.workSettings == null)
                    continue;
                
                pawn.workSettings.EnableAndInitializeIfNotAlreadyInitialized();
                pawn.workSettings.SetPriority(workTypeDef, 0);
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
                yield return gizmo;

            yield return new Command_Action
            {
                icon = ContentFinder<Texture2D>.Get(
                                (!repairingDisabled ? "UI/Commands/UI_Repair_Active" : "UI/Commands/UI_Repair_NotActive"), true),
                defaultLabel = ( repairingDisabled
                               ? textRepairing_disabled.Translate() 
                               : textRepairing_enabled.Translate() ),
                defaultDesc = textRepairing.Translate() + ": " + (repairingDisabled
                               ? textRepairing_disabled.Translate()
                               : textRepairing_enabled.Translate()),
                action = delegate
                {
                    repairingDisabled = !repairingDisabled;
                    if (Map != null)
                        this.Map.areaManager.Home[this.Position] = !repairingDisabled; 
                }
            };
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            // do nothing if not of colony
            if (selPawn.Faction != Faction.OfPlayer && !selPawn.IsSlaveOfColony)
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
            if (Utility_Tired.IsTooTired(selPawn))
            {
                yield return new FloatMenuOption("TrainingFacility_CannotUseTooTired".Translate(), null);
                yield break;
            }

            // Check weapon status
            Verb attackVerb = null;
            if (selPawn != null)
                attackVerb = selPawn.TryGetAttackVerb(this, true);

            //if (attackVerb != null && attackVerb.verbProps != null && attackVerb.verbProps.IsMeleeAttack)
            //{
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
            //}
            //else
            //{
            //    yield return new FloatMenuOption("TrainingFacility_MeleeWeaponRequired".Translate(), null);
            //}
        }
    }

}
