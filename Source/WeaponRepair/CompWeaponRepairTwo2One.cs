using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WeaponRepair
{
    // Most of this is based on the CompLongRangeMineralScanner
    public class CompWeaponRepairTwo2One : ThingComp
    {

        public bool CanBeRepaired
        {
            get
            {
                return (IsParentAndMapValid && InNeedOfRepairs && parent.Map.listerThings.ThingsOfDef(parent.def).CountAllowNull() >= 2);
            }
        }
        private bool IsParentAndMapValid
        {
            get
            {
                return (parent != null && parent.Spawned && parent.Map != null);
            }
        }
        public bool InNeedOfRepairs
        {
            get
            {
                // Repair only possible until 95% of max health
                return parent.HitPoints < parent.MaxHitPoints * Props.maxRepair;
            }
        }

        public CompProperties_WeaponRepairTwo2One Props
        {
            get
            {
                return (CompProperties_WeaponRepairTwo2One)props;
            }
        }
        

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
        }

        public override string CompInspectStringExtra()
        {
            //return "repair active. CanBeRepaired=" + CanBeRepaired.ToString() + ". InNeedOfRepairs="+ InNeedOfRepairs.ToString();
            return null;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
                yield return gizmo;



            // Debug Mode + God Mode required
            if (!Prefs.DevMode || !DebugSettings.godMode)
                yield break;

            yield return new Command_Action
            {
                defaultLabel = "Debug: Damage Weapon",
                action = delegate
                {
                    parent.HitPoints -= 10;
                }
            };
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption fmo in base.CompFloatMenuOptions(selPawn))
                yield return fmo;

            //Log.Error("1 " + Props.allowedDefSubName.NullOrEmpty().ToString() + " 2 " + parent.def.defName);

            // Only show float menu for ThingDef-names 'Gun_xxxx'
            if (!(Props.allowedDefSubName.NullOrEmpty() || Props.allowedDefSubName == "*") && !parent.def.defName.Contains(Props.allowedDefSubName))
                yield break;

            if (!InNeedOfRepairs)
            {
                //string debugstring = " => " + parent.HitPoints.ToString() + " / " + parent.MaxHitPoints.ToString();
                //yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("WeaponRepair_NoRepairNeeded".Translate(), null), selPawn, parent);
                yield break;
            }

            if (GetAvailableTwinThing(selPawn) == null)
            {
                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("WeaponRepair_NoWeaponTwinFound".Translate(), null), selPawn, parent);
                yield break;
            }

            if (GetValidWorkTableCount(selPawn) == 0)
            {
                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("WeaponRepair_NoRepairTableFound".Translate(), null), selPawn, parent);
                yield break;
            }


            // Check if this is reservable by the pawn
            if (!selPawn.CanReserve(parent, 1))
            {
                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("CannotUseReserved".Translate(), null), selPawn, parent);
                yield break;
            }
            // Check if this is reachable by the pawn
            if (!selPawn.CanReach(parent, PathEndMode.Touch, Danger.Deadly))
            {
                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("CannotUseNoPath".Translate(), null), selPawn, parent);
                yield break;
            }

            if (selPawn.workSettings.GetPriority(Props.workTypeDef) == 0)
            {
                string label = "WeaponRepair_NotAssignedToWorkType".Translate(new object[] { Props.workTypeDef.gerundLabel });

                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, null), selPawn, parent);
                yield break;
            }


            Action hoverAction = delegate
            {
                Thing twin = GetAvailableTwinThing(selPawn);
                MoteMaker.MakeStaticMote(twin.Position, parent.Map, ThingDefOf.Mote_FeedbackGoto);
            };
            Action giveRepairJob = delegate { TryGiveWeaponRepairJobToPawn(selPawn); };
            yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("WeaponRepair_RepairWeapon".Translate(), giveRepairJob, MenuOptionPriority.Default, hoverAction), selPawn, parent);
         
        }


        private bool TryGiveWeaponRepairJobToPawn(Pawn pawn)
        {
            if (!CanBeRepaired)
                return false;

            Building workTable = GetClosestValidWorktable(pawn);
            Thing thingMain = parent;
            Thing thingIngredient = GetAvailableTwinThing(pawn);

            if (workTable == null || thingMain == null || thingIngredient == null)
                return false;

            if (JobDriver_WeaponRepairTwo2One.maxAllowedRepair != Props.maxRepair)
                JobDriver_WeaponRepairTwo2One.maxAllowedRepair = Props.maxRepair;

            Job job = new Job(Props.jobDef, workTable, thingMain, thingIngredient);
            job.count = 1;
            return pawn.jobs.TryTakeOrderedJob(job);
        }

        private Thing GetAvailableTwinThing(Pawn pawn)
        {
            if (!CanBeRepaired || pawn == null || !pawn.Spawned || pawn.Downed || pawn.Map == null)
                return null;


            bool checkQuality = true;
            QualityCategory qcParent;
            checkQuality = parent.TryGetQuality(out qcParent);

            List<Thing> possibleThings = new List<Thing>();
            foreach (Thing currentThing in parent.Map.listerThings.ThingsOfDef(parent.def))
            {
                if (currentThing == parent)
                    continue;

                if (Props.compareQuality)
                {
                    QualityCategory qc;
                    currentThing.TryGetQuality(out qc);
                    if (checkQuality && (!currentThing.TryGetQuality(out qc) || ((int)qcParent < (int)qc)))
                        continue;
                }

                if (currentThing.IsBurning() || currentThing.IsBrokenDown() || currentThing.IsForbidden(pawn))
                    continue;

                if (!currentThing.IsInAnyStorage())
                    continue;

                if (!pawn.Map.reservationManager.CanReserve(pawn, currentThing, 1, -1, null, false))
                    continue;

                possibleThings.Add(currentThing);
            }

            //string log1 = "";
            //string log2 = "";
            //string log3 = "";

            Thing bestThing = null;
            foreach (Thing currentThing in possibleThings)
            {
                //log1 += currentThing.LabelMouseover;

                // Check if quality of thing is lower than quality of t
                QualityCategory qcBestThing;
                QualityCategory qcCurrentThing;
                bestThing.TryGetQuality(out qcBestThing);
                currentThing.TryGetQuality(out qcCurrentThing);

                //select lowest quality item
                if (checkQuality && bestThing != null)
                {
                    if ( (int)qcCurrentThing > (int)qcBestThing )
                        continue;

                    if ( (int)qcCurrentThing < (int)qcBestThing )
                    {
                        bestThing = currentThing;
                        continue;
                    }
                }


                //log2 += currentThing.LabelMouseover;

                if (bestThing == null)
                    bestThing = currentThing;
                else
                {
                    // take the thing with the lower hitpoints
                    if (bestThing.HitPoints > currentThing.HitPoints )
                        bestThing = currentThing;
                }
            }
            //log3 = bestThing.LabelMouseover;
            //Log.Error(log1 + "\n" + log2 + "\n" + log3);

            return bestThing;
        }

        private Building GetClosestValidWorktable(Pawn pawn)
        {
            List<Building> validWorkTables = GetValidWorktables(pawn);
            if (validWorkTables == null)
                return null;

            Building closestWorkTable = null;
            double distanceSquared = double.MaxValue;
            foreach (Building workTable in validWorkTables)
            {
                double workDistanceSquared = GetDistanceSquared(pawn.Position, workTable.Position);
                if (workDistanceSquared < distanceSquared)
                {
                    closestWorkTable = workTable;
                    distanceSquared = workDistanceSquared;
                }
            }
            return closestWorkTable;
        }
        private List<Building> GetValidWorktables(Pawn pawn)
        {
            if (!parent.Spawned || parent.Map == null || !pawn.Spawned || pawn.Map == null)
                return null;

            Map map = pawn.Map;

            List<Building> foundWorkTables = new List<Building>();
            foreach (ThingDef buildingDef in Props.worktableDefs)
            {
                IEnumerable<Building> foundBuildings = map.listerBuildings.AllBuildingsColonistOfDef(buildingDef);
                foreach (Building building in foundBuildings)
                {
                    if (building.Spawned && !building.IsBurning() && !building.IsBrokenDown() && !building.IsForbidden(pawn) && !building.IsDangerousFor(pawn) &&
                            map.reservationManager.CanReserve(pawn, building, 1, -1, null, false))
                        foundWorkTables.Add(building);
                }
            }
            return foundWorkTables.Count > 0 ? foundWorkTables : null;
        }
        private int GetValidWorkTableCount(Pawn pawn)
        {
            List<Building> worktables = GetValidWorktables(pawn);
            return (worktables == null ? 0 : worktables.Count);
        }

        public static double GetDistanceSquared(IntVec3 p1, IntVec3 p2)
        {
            int X = Math.Abs(p1.x - p2.x);
            int Y = Math.Abs(p1.y - p2.y);
            int Z = Math.Abs(p1.z - p2.z);

            return (X * X + Y * Y + Z * Z);
        }

    }
}
