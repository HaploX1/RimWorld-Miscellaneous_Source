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
    public class CompWeaponRepairWithThings : ThingComp
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

        public int NeededComponentCount
        {
            get
            {
                float neededRepairPercent = (parent.MaxHitPoints * Props.maxRepair) - parent.HitPoints;

                if (neededRepairPercent <= 0)
                    return 0;

                return (int)Math.Ceiling(CostPerPercent * neededRepairPercent);
            }
        }
        public double CostPerPercent
        {
            get
            {
                return (parent.MarketValue / Props.repairRatioPrice2Thing) / 100;
            }
        }

        public CompProperties_WeaponRepairWithThings Props
        {
            get
            {
                return (CompProperties_WeaponRepairWithThings)props;
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
            if (DebugSettings.godMode)
            {
                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Debug-Info: WeaponRepair active.", null), selPawn, parent);
            }

            foreach (FloatMenuOption fmo in base.CompFloatMenuOptions(selPawn))
                yield return fmo;

            //Log.Error("1 " + Props.allowedDefSubName.NullOrEmpty().ToString() + " 2 " + parent.def.defName);


            //// Only show float menu for ThingDef-names 'Gun_xxxx'
            //if (!(Props.allowedDefSubName.NullOrEmpty() || Props.allowedDefSubName == "*") && !parent.def.defName.Contains(Props.allowedDefSubName))
            //    yield break;
            ThingCategoryDef grenadesCategory = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("Grenades");
            if (!parent.def.IsRangedWeapon || grenadesCategory != null && parent.def.thingCategories.Any(t => t == grenadesCategory))
                yield break;

            if (!InNeedOfRepairs)
            {
                //string debugstring = " => " + parent.HitPoints.ToString() + " / " + parent.MaxHitPoints.ToString();
                //yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("WeaponRepair_NoRepairNeeded".Translate(), null), selPawn, parent);
                yield break;
            }

            List<ThingDef> repairMaterialsRaw = GetNeededRepairMaterialsRaw();

            if (GetAvailableRepairThings(selPawn, repairMaterialsRaw) == null)
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
                string label = "WeaponRepair_NotAssignedToWorkType".Translate(Props.workTypeDef.gerundLabel);

                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, null), selPawn, parent);
                yield break;
            }

            Action hoverAction = null;
            //Action hoverAction = delegate
            //{
            //    Thing twin = GetAvailableRepairThings(selPawn);
            //    MoteMaker.MakeStaticMote(twin.Position, parent.Map, ThingDefOf.Mote_FeedbackGoto);
            //};
            Action giveRepairJob = delegate { TryGiveWeaponRepairJob2ToPawn(selPawn); };
            yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("WeaponRepair_RepairWeapon".Translate(), giveRepairJob, MenuOptionPriority.Default, hoverAction), selPawn, parent);
         
        }


        private bool TryGiveWeaponRepairJob2ToPawn(Pawn pawn)
        {
            if (!CanBeRepaired)
                return false;

            Building workTable = GetClosestValidWorktable(pawn);
            Thing thingMain = parent;
            List<Thing> thingIngredients = GetAvailableRepairThings(pawn, GetNeededRepairMaterialsRaw());

            if (workTable == null || thingMain == null || thingIngredients == null || thingIngredients.Count == 0)
                return false;

            if (JobDriver_WeaponRepairTwo2One.maxAllowedRepair != Props.maxRepair)
                JobDriver_WeaponRepairTwo2One.maxAllowedRepair = Props.maxRepair;

            Job job = new Job(Props.jobDef, workTable, thingMain, thingIngredients[0]);
            foreach (Thing thingIngredient in thingIngredients)
            {
                if (thingIngredient == thingIngredients[0])
                    continue;
                job.AddQueuedTarget(TargetIndex.C, thingIngredient);
            }
            job.count = 1;
            return pawn.jobs.TryTakeOrderedJob(job);
        }

        private List<RepairCurveValues> GetNeededRepairMaterials()
        {
            QualityCategory qc;
            if (!parent.TryGetQuality(out qc))
                qc = QualityCategory.Normal;

            // No repair when over repair threshold
            if (parent.HitPoints > parent.MaxHitPoints * Props.maxRepair)
                return null;

            // Find next lowest fitting repair-cost element
            if (Props.repairCostCurve == null || Props.repairCostCurve.Count == 0)
                return null;
            if (Props.repairCostCurve.ContainsKey(qc))
                return Props.repairCostCurve[qc];

            List<RepairCurveValues> repairValues = null;
            for (int i = 0; i <= (int)QualityCategory.Legendary; i++)
            {
                if (i > (int)qc)
                    break;
                if (Props.repairCostCurve.ContainsKey(qc))
                    repairValues = Props.repairCostCurve[(QualityCategory)i];
            }
            return repairValues;
        }
        private List<ThingDef> GetNeededRepairMaterialsRaw()
        {
            List<RepairCurveValues> repairMaterials = GetNeededRepairMaterials();
            List<ThingDef> repairMaterialsRaw = null;
            if (repairMaterials != null)
            {
                repairMaterialsRaw = new List<ThingDef>();
                foreach (RepairCurveValues rcv in repairMaterials)
                    repairMaterialsRaw.Add(rcv.thingDef);
            }
            return repairMaterialsRaw;
}

        private List<Thing> GetAvailableRepairThings(Pawn pawn, List<ThingDef> tDefs)
        {
            if (!CanBeRepaired || pawn == null || !pawn.Spawned || pawn.Downed || pawn.Map == null)
                return null;


            bool checkQuality = true;
            QualityCategory qcParent;
            checkQuality = parent.TryGetQuality(out qcParent);

            List<Thing> possibleThings = new List<Thing>();
            foreach (ThingDef tDef in tDefs)
            {
                foreach (Thing currentThing in parent.Map.listerThings.ThingsOfDef(tDef))
                {
                    if (currentThing == parent)
                        continue;

                    if (currentThing.IsBurning() || currentThing.IsBrokenDown() || currentThing.IsForbidden(pawn))
                        continue;

                    if (!currentThing.IsInAnyStorage())
                        continue;

                    if (!pawn.Map.reservationManager.CanReserve(pawn, currentThing, 1, -1, null, false))
                        continue;

                    possibleThings.Add(currentThing);
                }
            }

            return possibleThings;
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
