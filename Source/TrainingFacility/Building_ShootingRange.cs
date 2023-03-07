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
    public class Building_ShootingRange : Building
    {

        public JoyGiverDef GetJoyGiverDef()
        {
            return DefDatabase<JoyGiverDef>.GetNamed("PracticeShooting");
        }
        public JobDef GetJobDef()
        {
            return DefDatabase<JobDef>.GetNamed("UseShootingRange_NonJoy");
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (!respawningAfterLoad)
                LongEventHandler.ExecuteWhenFinished(Setup_Part2 );

        }

        private void Setup_Part2()
        {
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
            if (JobDriver_Archery.isTooTired(selPawn))
            {
                yield return new FloatMenuOption("TrainingFacility_CannotUseTooTired".Translate(), null);
                yield break;
            }

            // Check weapon status
            Verb attackVerb = null;
            if (selPawn != null)
                attackVerb = selPawn.TryGetAttackVerb(this, false);

            if (attackVerb != null && attackVerb.verbProps != null && !attackVerb.verbProps.IsMeleeAttack)
            {
                Thing chair;
                JoyGiverDef joyGiverDef = this.GetJoyGiverDef();
                IntVec3 standCell = Utility_PositionFinder.TryFindWatchBuildingPosition(selPawn, this, this.def.building.watchBuildingStandDistanceRange, joyGiverDef.desireSit, out chair);

                Action action_PracticeShooting = delegate
                {
                    selPawn.drafter.Drafted = false;
                    
                    Job job = new Job(this.GetJobDef(), this, standCell, chair);

                    if (job != null)
                        selPawn.jobs.TryTakeOrderedJob(job);
                };

                if (standCell != IntVec3.Invalid)
                    yield return new FloatMenuOption("TrainingFacility_PracticeShooting".Translate(), action_PracticeShooting);
            }
            else
            {
                yield return new FloatMenuOption("TrainingFacility_RangedWeaponRequired".Translate(), null);
            }
        }

        public IEnumerable<IntVec3> CalculateShootingCells()
        {
            JoyGiverDef joyGiverDef = this.GetJoyGiverDef();

            return Utility_PositionFinder.FindAllWatchBuildingCells(this, this.def.building.watchBuildingStandDistanceRange);
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref autoRearm, "autoRearm", true);
        }

        // From Building_Trap
        private bool autoRearm = true;
        private bool CanSetAutoRearm
        {
            get
            {
                return base.Faction == Faction.OfPlayer && this.def.blueprintDef != null; // && this.def.IsResearchFinished;
            }
        }
        public override void Kill(DamageInfo? dinfo = null, Hediff exactCulprit = null)
        {
            bool spawned = base.Spawned;
            Map map = base.Map;
            base.Kill(dinfo, exactCulprit);
            if (spawned)
            {
                this.CheckAutoRebuild(map);
            }
        }
        private void CheckAutoRebuild(Map map)
        {
            if (this.autoRearm && this.CanSetAutoRearm && map != null && GenConstruct.CanPlaceBlueprintAt(this.def, base.Position, base.Rotation, map, false, null, null, base.Stuff).Accepted)
            {
                GenConstruct.PlaceBlueprintForBuild(this.def, base.Position, map, base.Rotation, Faction.OfPlayer, base.Stuff);
            }
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
                yield return gizmo;

            if (this.CanSetAutoRearm)
                yield return new Command_Toggle
                {
                    defaultLabel = "CommandAutoRearm".Translate(),
                    defaultDesc = "CommandAutoRearmDesc".Translate(),
                    hotKey = KeyBindingDefOf.Misc3,
                    icon = TexCommand.RearmTrap,
                    isActive = (() => this.autoRearm),
                    toggleAction = delegate ()
                    {
                        this.autoRearm = !this.autoRearm;
                    }
                };
            
            yield break;
        }
    }
}
