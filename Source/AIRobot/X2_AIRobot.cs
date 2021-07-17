﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace AIRobot
{
    public class X2_AIRobot : Pawn
    {
        public X2_Building_AIRobotRechargeStation rechargeStation;
        public X2_ThingDef_AIRobot def2;

        private static int activePawns = 0;

        public bool ignoreSpawnRename = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref activePawns, "activePawns", 0);
        }

        private static void SetBasename(Pawn pawn)
        {
            if ((NameTriple)pawn.Name == null)
            {
                activePawns++;
                pawn.Name = new NameTriple("AIRobot_Basename_first".Translate(), pawn.def.label + " " + activePawns.ToString(), "AIRobot_Basename_last".Translate());
            }
        }

        #region spawn/destroy
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            def2 = def as X2_ThingDef_AIRobot;

            if (def2 == null)
                Log.Error("X2_AIRobot -- def2 is null. Missing class definition in xml file?");

            //InitPawn_Setup();
            LongEventHandler.ExecuteWhenFinished(InitPawn_Setup);
        }
        private void InitPawn_Setup()
        {
            // Do not init when scribe is working? Should be reinitialised from savegame!
            if (Scribe.mode != LoadSaveMode.Inactive)
                return;

            this.equipment = new Pawn_EquipmentTracker(this);
            this.apparel = new Pawn_ApparelTracker(this);

            // Add base shielding
            //X2_Building_AIRobotRechargeStation.GenerateBaseApparel(this);

            // Skills are needed for some skills (like Cleaning)
            this.skills = new Pawn_SkillTracker(this);
            SetSkills();

            // Story is needed for some skills (like Growing)
            this.story = new Pawn_StoryTracker(this);
            if (this.gender == Gender.Male)
                this.story.bodyType = BodyTypeDefOf.Male;
            else
                this.story.bodyType = BodyTypeDefOf.Female;
            this.story.crownType = CrownType.Average;

            this.Drawer.renderer.graphics.ResolveApparelGraphics();

            // To allow the robot to be drafted -> Still not possible to draft, because 1. not humanlike and 2. the GetGizmos in Pawn_Drafter is internal! 
            //this.drafter = new Pawn_DraftController(this); // Not needed because not usable

            if (this.relations == null)
                this.relations = new Pawn_RelationsTracker(this);
            this.relations.ClearAllRelations();

            if (!this.ignoreSpawnRename)
                SetBasename(this);
            ignoreSpawnRename = false;

            // Robots are not allowed to have JOY like partying!
            timetable = new Pawn_TimetableTracker(this);
            for (int i = 0; i < 24; i++)
                timetable.SetAssignment(i, TimeAssignmentDefOf.Work);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            IntVec3 oldPos = this.Position != IntVec3.Invalid ? this.Position : this.PositionHeld;
            Map oldMap = this.Map != null ? this.Map : this.MapHeld;
            X2_Building_AIRobotRechargeStation oldRechargeStation = this.rechargeStation;

            ThingDef wDef = null;
            if (this != null && this.def2 != null && this.def2.destroyedDef != null)
                wDef = this.def2.destroyedDef;
            
            try
            {
                base.Destroy(DestroyMode.Vanish);
            } catch (Exception ex)
            {
                Log.Warning(ex.Message + "\n" + ex.StackTrace);
            }



            // --- !!! NEW Approach !!! ---
            TrySpawnResources(oldMap, oldPos);

            try
            {
                IEnumerable<Thing> corpses = oldMap.listerThings.AllThings.Where<Thing>(t => t.Spawned && t == this.Corpse);
                foreach (Thing corpse in corpses)
                    corpse.Destroy(DestroyMode.Vanish);
            } catch (Exception ex)
            {
                Log.Warning("Couldn't destroy corpses.\n"+ex.StackTrace);
            }



            // --- !!! OLD Approach !!! ---
            //// spawn destroyed object here
            //if (mode != DestroyMode.Vanish && wDef != null)
            //{
            //    X2_AIRobot_disabled thing = (X2_AIRobot_disabled)GenSpawn.Spawn(wDef, oldPos, oldMap);
            //    thing.stackCount = 1;
            //    thing.rechargestation = oldRechargeStation;
            //    thing.SetFactionDirect(this.Faction);

            //    // set the disabled robot in the recharge station
            //    oldRechargeStation.disabledRobot = thing;
            //}

        }

        private void TrySpawnResources(Map oldMap, IntVec3 oldPos)
        {
            float returnMulti;

            if (this.rechargeStation != null && rechargeStation.def2 != null && rechargeStation.def2.robotRepairCosts != null)
            {
                returnMulti = 0.4f;
                foreach (ThingDefCountClass tDefCC in rechargeStation.def2.robotRepairCosts)
                {
                    int count = (int)Math.Floor(tDefCC.count * returnMulti);
                    if (count < 1)
                        continue;

                    Thing t = GenSpawn.Spawn(tDefCC.thingDef, oldPos, oldMap);
                    t.stackCount = count;
                }
            }
            else if (this.rechargeStation != null && rechargeStation.def2 != null && rechargeStation.def2.costList != null)
            {
                returnMulti = 0.2f;
                foreach (ThingDefCountClass tDefCC in rechargeStation.def2.costList)
                {
                    int count = (int)Math.Floor(tDefCC.count * returnMulti);
                    if (count < 1)
                        continue;

                    Thing t = GenSpawn.Spawn(tDefCC.thingDef, oldPos, oldMap);
                    t.stackCount = count;
                }
            }
            else
            {
                returnMulti = 0.3f;
                foreach (ThingDefCountClass tDefCC in def.costList)
                {
                    int count = (int)Math.Floor(tDefCC.count * returnMulti);
                    if (count < 1)
                        continue;

                    Thing t = GenSpawn.Spawn(tDefCC.thingDef, oldPos, oldMap);
                    t.stackCount = count;
                }
            }
        }

        #endregion


        #region Tick
        public override void Tick()
        {

            // Prevent ticks, if you aren't living anymore!
            if (this.DestroyedOrNull())
            {
                //Log.Error("I'm destroyed but ticking..");
                return;
            }
            // Downed only destroyes, if health is < 5%
            if (Spawned && (Dead || (Downed && health.summaryHealth.SummaryHealthPercent < 0.05f) || needs.rest.CurLevel <= 0.01f))
            {
                this.Destroy(DestroyMode.KillFinalize);
                return;
            }

            base.Tick();
            
            // needed?
            if (needs.food != null && needs.food.CurLevel < 1.0f)
                needs.food.CurLevel = 1f;

            // Learning disabled --> reset skills every x ticks
            if (def2 == null || !def2.allowLearning)
            {
                if (this.IsHashIntervalTick(4800))
                    SetSkills(true);
            }

            if (this.Spawned)
            {
                if (rechargeStation == null)
                    rechargeStation = TryFindRechargeStation(this, Map);

                // Remove unwanted HeDiffs
                if (Gen.IsHashIntervalTick(this, 500))
                    RemoveUnwantedHediffs(this);
            }
        }
        #endregion

        #region inspection

        public override string GetInspectString()
        {
            string workString = base.GetInspectString() ;
            if (DebugSettings.godMode)
            {
                if (Position != null && Position != IntVec3.Invalid &&
                    rechargeStation != null && rechargeStation.Position != null && rechargeStation.Position != IntVec3.Invalid)
                {
                    if (!workString.NullOrEmpty())
                        workString += "\n";
                    workString += "Distance to base: " + AIRobot_Helper.GetDistance(Position, rechargeStation.Position).ToString("0") + " cells";
                    workString += " -- ";
                    workString += "Remaining charge: " + needs.rest.CurLevel.ToStringPercent();
                }
            }
            return workString.TrimEndNewlines();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
                yield return gizmo;

            if (rechargeStation != null && rechargeStation.Spawned && !rechargeStation.Destroyed)
            {
                // Key-Binding 0
                Command_Action opt0;
                opt0 = new Command_Action();
                opt0.icon = X2_Building_AIRobotRechargeStation.UI_ButtonForceRecharge;
                opt0.defaultLabel = X2_Building_AIRobotRechargeStation.lbSendOwnerToRecharge.Translate();
                opt0.defaultDesc = X2_Building_AIRobotRechargeStation.txtSendOwnerToRecharge.Translate();
                opt0.hotKey = KeyBindingDefOf.Misc4;
                opt0.activateSound = SoundDef.Named("Click");
                opt0.action = delegate { rechargeStation.Notify_CallBotForShutdown(); };
                opt0.disabled = rechargeStation == null;
                opt0.disabledReason = "Error: No recharge station assigned.";
                opt0.groupKey = 1234567 + 0;
                yield return opt0;
            }

            //if (DebugSettings.godMode)
            {
                // Key-Binding 1
                Command_Action opt1;
                opt1 = new Command_Action();
                opt1.icon = X2_Building_AIRobotRechargeStation.UI_ButtonGoLeft;
                opt1.defaultLabel = "";// "LEFT";
                opt1.defaultDesc = "Go 4 left";
                opt1.hotKey = KeyBindingDefOf.Misc5;
                opt1.activateSound = SoundDef.Named("Click");
                opt1.action = delegate { Debug_ForceGotoDistance(-4,0); };
                opt1.disabled = false;
                opt1.disabledReason = "";
                opt1.groupKey = 1234567 + 1;
                yield return opt1;

                // Key-Binding 3
                Command_Action opt3;
                opt3 = new Command_Action();
                opt3.icon = X2_Building_AIRobotRechargeStation.UI_ButtonGoUp;
                opt3.defaultLabel = "";// "UP";
                opt3.defaultDesc = "Go 4 up";
                opt3.hotKey = KeyBindingDefOf.Misc9;
                opt3.activateSound = SoundDef.Named("Click");
                opt3.action = delegate { Debug_ForceGotoDistance(0, 4); };
                opt3.disabled = false;
                opt3.disabledReason = "";
                opt3.groupKey = 1234567 + 3;
                yield return opt3;

                // Key-Binding 2
                Command_Action opt2;
                opt2 = new Command_Action();
                opt2.icon = X2_Building_AIRobotRechargeStation.UI_ButtonGoDown;
                opt2.defaultLabel = "";// "DOWN";
                opt2.defaultDesc = "Go 4 down";
                opt2.hotKey = KeyBindingDefOf.Misc8;
                opt2.activateSound = SoundDef.Named("Click");
                opt2.action = delegate { Debug_ForceGotoDistance(0, -4); };
                opt2.disabled = false;
                opt2.disabledReason = "";
                opt2.groupKey = 1234567 + 2;
                yield return opt2;

                // Key-Binding 4
                Command_Action opt4;
                opt4 = new Command_Action();
                opt4.icon = X2_Building_AIRobotRechargeStation.UI_ButtonGoRight;
                opt4.defaultLabel = "";// "RIGHT";
                opt4.defaultDesc = "Go 4 right";
                opt4.hotKey = KeyBindingDefOf.Misc10;
                opt4.activateSound = SoundDef.Named("Click");
                opt4.action = delegate { Debug_ForceGotoDistance(4, 0); };
                opt4.disabled = false;
                opt4.disabledReason = "";
                opt4.groupKey = 1234567 + 4;
                yield return opt4;
            }
        }
        private void Debug_ForceGotoDistance(int distX, int distZ)
        {

            IntVec3 target = this.Position + new IntVec3(distX,0,distZ);
            target = RCellFinder.BestOrderedGotoDestNear(target, this);
            Job job = new Job(JobDefOf.Goto, target);
            this.jobs.TryTakeOrderedJob(job);
        }

        #endregion


        #region Functions

        public static X2_Building_AIRobotRechargeStation TryFindRechargeStation(X2_AIRobot bot, Map map)
        {
            X2_Building_AIRobotRechargeStation foundBase;

            if (map == null && bot.rechargeStation != null)
                map = bot.rechargeStation.Map;
            if (map == null)
                map = Find.CurrentMap;
            if (map == null)
                return default(X2_Building_AIRobotRechargeStation);

            IEnumerable<X2_Building_AIRobotRechargeStation> allBases = map.listerBuildings.AllBuildingsColonistOfClass<X2_Building_AIRobotRechargeStation>();
            if (allBases == null)
                return default(X2_Building_AIRobotRechargeStation);

            foundBase = allBases.Where(t => t.robot == bot).FirstOrDefault();

            return foundBase;
        }


        #region Skills / WorkGivers / WorkTags
        private List<WorkGiver> workGiversEmergencyCache = null;
        private List<WorkGiver> workGiversNonEmergencyCache = null;
        public List<WorkGiver> GetWorkGivers(bool emergency)
        {
            if (emergency && workGiversEmergencyCache != null)
                return workGiversEmergencyCache;
            if (!emergency && workGiversNonEmergencyCache != null)
                return workGiversNonEmergencyCache;

            List<WorkTypeDef> wtsByPrio = new List<WorkTypeDef>();
            List<WorkTypeDef> allDefsListForReading = DefDatabase<WorkTypeDef>.AllDefsListForReading;
            int num = 999;
            for (int i = 0; i < allDefsListForReading.Count; i++)
            {
                WorkTypeDef workTypeDef = allDefsListForReading[i];
                int priority = GetPriority(workTypeDef);
                if (priority > 0)
                {
                    if (priority < num)
                    {
                        if (workTypeDef.workGiversByPriority.Any((WorkGiverDef wg) => wg.emergency == emergency))
                        {
                            num = priority;
                        }
                    }
                    wtsByPrio.Add(workTypeDef);
                }
            }
            wtsByPrio.InsertionSort(delegate (WorkTypeDef a, WorkTypeDef b)
            {
                float value = (float)(a.naturalPriority + (4 - this.GetPriority(a)) * 100000);
                return ((float)(b.naturalPriority + (4 - this.GetPriority(b)) * 100000)).CompareTo(value);
            });
            List<WorkGiver> workGivers = new List<WorkGiver>();
            for (int j = 0; j < wtsByPrio.Count; j++)
            {
                WorkTypeDef workTypeDef2 = wtsByPrio[j];
                for (int k = 0; k < workTypeDef2.workGiversByPriority.Count; k++)
                {
                    WorkGiver worker = workTypeDef2.workGiversByPriority[k].Worker;
                    workGivers.Add(worker);
                }
            }

            // Fill cache
            if (emergency)
                workGiversEmergencyCache = workGivers;
            else
                workGiversNonEmergencyCache = workGivers;

            return workGivers;
        }

        public bool CanDoWorkType(WorkTypeDef workTypeDef)
        {
            if (def2 == null)
                return false;

            int neededSkillsCount;
            if (workTypeDef == null || workTypeDef.relevantSkills == null)
                neededSkillsCount = 0;
            else
                neededSkillsCount = workTypeDef.relevantSkills.Count;

            foreach (SkillDef skillDef in workTypeDef.relevantSkills)
            {
                foreach (X2_ThingDef_AIRobot.RobotSkills robotSkills in def2.robotSkills)
                    if (robotSkills.skillDef == skillDef)
                        neededSkillsCount--;

                if (neededSkillsCount == 0)
                    break;
            }

            WorkTags workTags = (def2.robotWorkTags & workTypeDef.workTags);
            if (neededSkillsCount == 0 && workTags != WorkTags.None)
                return true;

            return false;
        }

        private int GetPriority(WorkTypeDef workTypeDef)
        {
            if (def2 == null)
                return 0;
            
            foreach (X2_ThingDef_AIRobot.RobotWorkTypes robotWorkTypes in def2.robotWorkTypes)
            {
                if (robotWorkTypes.workTypeDef == workTypeDef)
                    return robotWorkTypes.priority;
            }

            return 0;
        }
        private void SetSkills(bool isTickUpdate = false)
        {
            if (def2 == null)
                return;
            
            foreach (SkillRecord skill in this.skills.skills)
            {
                foreach (X2_ThingDef_AIRobot.RobotSkills robotSkills in def2.robotSkills)
                {
                    if (skill.def == robotSkills.skillDef)
                    {
                        skill.xpSinceLastLevel = skill.XpRequiredForLevelUp / 3;
                        skill.xpSinceMidnight = 0f;

                        skill.levelInt = robotSkills.level;

                        if (!isTickUpdate)
                            skill.passion = robotSkills.passion;
                    }
                }
            }
        }
        #endregion

        public static List<Type> removeablehediffs = new List<Type>(new Type[]
{
              typeof(Hediff_Alcohol),
              typeof(Hediff_Hangover),
              typeof(Hediff_Addiction),
              typeof(Hediff_HeartAttack),
              typeof(Hediff_Pregnant),
              typeof(Hediff_PsychicLove),
              typeof(Hediff_Psylink),
});
        public static List<Type> nonremoveablehediffs = new List<Type>(new Type[]
{
              typeof(Hediff_AddedPart),
              typeof(Hediff_Implant),
              typeof(Hediff_Injury),
              typeof(Hediff_MissingPart),
});

        // removing hediffs, like alcohol etc.
        // Provided by SkyArchAngel
        public static void RemoveUnwantedHediffs(Pawn p)
        {
            var hediffs = p.health.hediffSet.hediffs;
            for (var i = 0; i < hediffs.Count; i++)
            {
                // Whitelist approach
                if (removeablehediffs.Contains(hediffs[i].def.hediffClass))
                {
                    p.health.RemoveHediff(hediffs[i]);
                }

                // Blacklist approach
                //if (nonremoveablehediffs.Contains(hediffs[i].def.hediffClass) == false)
                //{
                //    p.health.RemoveHediff(hediffs[i]);
                //}
            }
        }

        #endregion

    }

}
