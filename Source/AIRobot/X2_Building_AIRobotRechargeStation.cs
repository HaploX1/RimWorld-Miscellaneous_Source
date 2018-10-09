using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace AIRobot
{
    [StaticConstructorOnStartup]
    public class X2_Building_AIRobotRechargeStation : Building
    {
        #region Variables

        public X2_ThingDef_AIRobot_Building_RechargeStation def2 = null;

        public static string txtSendOwnerToRecharge = "AIRobot_SendOwnerToRecharge";
        public static string lbSendOwnerToRecharge = "AIRobot_Label_SendOwnerToRecharge";
        public static string txtSpawnOwner = "AIRobot_SpawnRobot";
        public static string lbSpawnOwner = "AIRobot_Label_SpawnRobot";
        public static string txtNoPower = "AIRobot_NoPower";
        public static string lbRecallAllRobots = "AIRobot_Label_RecallAllRobots";
        public static string txtRecallAllRobots = "AIRobot_RecallAllRobots";
        public static string lbActivateAllRobots = "AIRobot_Label_ActivateAllRobots";
        public static string txtActivateAllRobots = "AIRobot_ActivateAllRobots";

        public static string txtDisabledBecauseNotHomeMap = "AIRobot_Disabled_OnlyStartOnHomeMap";

        public static string lbFindRobot = "AIRobot_Label_FindRobot";
        public static string txtFindRobot = "AIRobot_FindRobot";

        public Graphic PrimaryGraphic;
        public Graphic SecondaryGraphic;
        public string SecondaryGraphicPath;

        public static Texture2D UI_ButtonForceRecharge = ContentFinder<Texture2D>.Get("UI/Commands/Robots/UI_ShutDown");
        public static Texture2D UI_ButtonStart = ContentFinder<Texture2D>.Get("UI/Commands/Robots/UI_Start");
        public static Texture2D UI_ButtonForceRechargeAll = ContentFinder<Texture2D>.Get("UI/Commands/Robots/UI_ShutDownAll");
        public static Texture2D UI_ButtonForceActivateAll = ContentFinder<Texture2D>.Get("UI/Commands/Robots/UI_StartAll");
        public static Texture2D UI_ButtonSearch = ContentFinder<Texture2D>.Get("UI/Commands/Robots/UI_Search");

        private string spawnThingDef = "";

        public List<X2_AIRobot> container;

        public X2_AIRobot robot;
        private bool robotSpawnedOnce = false;
        private bool robotIsDestroyed = false;
        public bool SpawnRobotAfterRecharge = true;

        public bool isRechargeActive = false;

        private float rechargeEfficiency = 1.0f;
        private float calcDistanceRestCheck = -1f;

        private bool notify_spawnRequested = false;
        //private bool notify_recallRequested = false;

        public CompPowerTrader powerComp;

        public X2_AIRobot_disabled disabledRobot;

        public X2_AIRobot GetRobot
        {
            get
            {
                if (!robotSpawnedOnce || robotIsDestroyed)
                    return null;

                if (robot != null)
                    return robot;

                if (container != null && container.Count > 0)
                    return container.FirstOrDefault(); //[0];

                return null;
            }
        }

        #endregion

        #region Initialization

        private void ReadXMLData()
        {
            def2 = def as X2_ThingDef_AIRobot_Building_RechargeStation;
            if (def2 == null)
                return;

            spawnThingDef = def2.spawnThingDef;
            SecondaryGraphicPath = def2.secondaryGraphicPath;
            rechargeEfficiency = def2.rechargeEfficiency;
        }

        public void GetGraphics()
        {
            if (def2 == null)
                ReadXMLData();

            if (PrimaryGraphic == null || PrimaryGraphic == BaseContent.BadGraphic)
            {
                PrimaryGraphic = base.Graphic;
                //PrimaryGraphic = GraphicDatabase.Get<Graphic_Multi>(def.graphicData.texPath, def.graphic.Shader, def.graphic.drawSize, def.graphic.Color, def.graphic.ColorTwo);
            }

            if (SecondaryGraphic == null || SecondaryGraphic == BaseContent.BadGraphic)
                SecondaryGraphic = GraphicDatabase.Get<Graphic_Multi>(SecondaryGraphicPath, def2.graphic.Shader, def2.graphic.drawSize, def2.graphic.Color, def2.graphic.ColorTwo);

            if (UI_ButtonForceRecharge == null)
                UI_ButtonForceRecharge = ContentFinder<Texture2D>.Get("UI/Commands/Robots/UI_ShutDown");

            if (UI_ButtonStart == null)
                UI_ButtonStart = ContentFinder<Texture2D>.Get("UI/Commands/Robots/UI_Start");

        }

        public override void PostMake()
        {
            base.PostMake();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            ReadXMLData();

            base.SpawnSetup(map, respawningAfterLoad);

            this.powerComp = base.GetComp<CompPowerTrader>();
            
            if (container == null)
                ClearContainer();

            LongEventHandler.ExecuteWhenFinished(Setup_Part2);

        }

        /// <summary>
        /// This is called seperately when the Mod-Thread is done.
        /// It is needed to be seperately from SpawnSetup, so that the graphics can be found
        /// </summary>
        public void Setup_Part2()
        {
            GetGraphics();
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            DespawnRobot(robot, true);
            ClearContainer();

            base.Destroy(mode);
        }

        public void DespawnRobot(X2_AIRobot bot, bool destroying = false)
        {
            if (bot != null && !bot.Destroyed)
            {
                if (destroying)
                    bot.Destroy(DestroyMode.Vanish);
                else
                    if (bot.Spawned)
                        bot.DeSpawn();
            }
            robot = null;

            isRechargeActive = false;
        }

        public void AddRobotToContainer(X2_AIRobot bot)
        {
            if (bot.HasAttachment(ThingDefOf.Fire))
                bot.GetAttachment(ThingDefOf.Fire).Destroy(DestroyMode.Vanish);

            isRechargeActive = false;

            bot.stances.CancelBusyStanceHard();
            bot.jobs.StopAll(false);
            bot.pather.StopDead();
            if (bot.Drafted)
                bot.drafter.Drafted = false;

            if (!container.Contains(bot))
                container.Add(bot);
            
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
                maps[i].designationManager.RemoveAllDesignationsOn(bot, false);

            DespawnRobot(bot, false);
        }
        

        public override void ExposeData()
        {
            try
            {

                base.ExposeData();

                Scribe_Values.Look<bool>(ref this.robotSpawnedOnce, "robotSpawned", false);
                Scribe_Values.Look<bool>(ref this.robotIsDestroyed, "robotDestroyed", false);
                Scribe_Values.Look<bool>(ref this.SpawnRobotAfterRecharge, "autospawn", true);

                try
                {
                    Scribe_References.Look<X2_AIRobot>(ref robot, "robot", true); // must be before Scribe_Collections -> Else errors!
                }
                catch (Exception ex)
                {
                    Log.Warning("X2_Building_AIRobot_RechargeStation -- Error while loading 'robot':\n" + ex.Message + "\n" + ex.StackTrace);
                }

                try
                {
                    Scribe_Collections.Look<X2_AIRobot>(ref this.container, "container", LookMode.Deep, null); // new object[0]); -> Throws errors!
                }
                catch (Exception ex)
                {
                    Log.Warning("X2_Building_AIRobot_RechargeStation -- Error while loading 'container':\n" + ex.Message + "\n" + ex.StackTrace);
                }
            }
            catch (Exception ex)
            {
                Log.Error("X2_Building_AIRobot_RechargeStation -- Unknown error while loading:\n" + ex.Message + "\n" + ex.StackTrace);
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                updateGraphicForceNeeded= true;

                if (container == null)
                    ClearContainer();
            }
        }

        #endregion

        #region Graphics

        public override Graphic Graphic
        {
            get
            {
                if (PrimaryGraphic == null)
                {
                    GetGraphics();
                    if (PrimaryGraphic == null)
                        return base.Graphic;
                }

                if (robot == null && !robotIsDestroyed)
                {
                    return PrimaryGraphic;
                }
                else
                {
                    if (SecondaryGraphic == null)
                    {
                        GetGraphics();
                        if (SecondaryGraphic == null)
                            return PrimaryGraphic;

                        return SecondaryGraphic;
                    }
                    else
                    {
                        return SecondaryGraphic;
                    }
                }
            }
        }

        private Graphic graphicOld;
        private bool updateGraphicForceNeeded = false;
        private void UpdateGraphic()
        {
            if (Graphic != graphicOld || updateGraphicForceNeeded)
            {
                updateGraphicForceNeeded = false;

                //Log.Error("Update Graphic");
                graphicOld = Graphic;
                Notify_ColorChanged();
                Map.mapDrawer.MapMeshDirty(this.Position, MapMeshFlag.Things, true, false);
            }
        }

        #endregion

        #region Ticker

        public override void Tick()
        {
            base.Tick();

            //Handle graphic update
            UpdateGraphic();

            // robot in container => recharge or release needed?
            if (robot == null && IsRobotInContainer())
            {
                X2_AIRobot containedRobot = container[0] as X2_AIRobot;
                if (containedRobot == null)
                {
                    container.Remove(container[0]);
                    return;
                }

                if (notify_spawnRequested)
                {
                    notify_spawnRequested = false;
                    Button_SpawnBot();
                    return;
                }

                if (SpawnRobotAfterRecharge && containedRobot.needs.rest.CurLevel >= 0.99f)
                {
                    Button_SpawnBot();
                }
                else if (containedRobot.needs.rest.CurLevel < 1f)
                {
                    containedRobot.needs.rest.CurLevel += (0.1f / GenDate.TicksPerHour) * rechargeEfficiency;
                    if (containedRobot.needs.rest.CurLevel > 1f)
                        containedRobot.needs.rest.CurLevel = 1f;

                    TryThrowBatteryMote(containedRobot);
                }

                // Try to heal robot
                TryHealDamagedBodyPartOfRobot(containedRobot);
                return;
            }
            notify_spawnRequested = false;

            if (robotIsDestroyed)
            {
                // TODO: What do we do, when the robot is destroyed?


                // Last step: do nothing more!
                return;
            }

            if (robot == null && (!robotSpawnedOnce || !IsRobotInContainer()))
                return;

            // if the robot is dead...
            if (robotSpawnedOnce && !IsRobotInContainer() && (robot == null || robot.Destroyed || robot.Dead))
            {
                if ((robot.Destroyed || robot.Dead) && robot.Corpse != null)
                    robot.Corpse.Destroy( DestroyMode.Vanish );
                
                robotIsDestroyed = true;
                return;
            }

            if (isRechargeActive && robot != null && Gen.IsHashIntervalTick(robot, 30) && !AIRobot_Helper.IsInDistance(robot.Position, this.Position, 3))
            {
                robot.jobs.ClearQueuedJobs();
                isRechargeActive = false;
            }

            if (isRechargeActive)
            {
                if (robot.needs.rest.CurLevel < 1f)
                {
                    robot.needs.rest.CurLevel += (0.1f / GenDate.TicksPerHour) * rechargeEfficiency * 2;
                    if (robot.needs.rest.CurLevel > 1f)
                        robot.needs.rest.CurLevel = 1f;

                    TryThrowBatteryMote(robot);
                }
                else
                {
                    robot.jobs.ClearQueuedJobs();
                    robot.jobs.EndCurrentJob(JobCondition.Succeeded, true);
                    isRechargeActive = false;
                }
                return;
            }


            if (!Gen.IsHashIntervalTick(robot, 250))
                return;

            TryUpdateAllowedArea(robot);

            if (calcDistanceRestCheck == -1)
                calcDistanceRestCheck = AIRobot_Helper.GetSlopePoint(robot.GetStatValue(StatDefOf.MoveSpeed, true), 1f, 6f, 15f, 40f); // movementspeed slope: speed 1 -> 30 cells, speed 6 -> 50 cells

            //Log.Error("Max allowed distance: " + calcDistanceRestCheck.ToString("0.##") + " / MoveSpeed: " + robot.GetStatValue(StatDefOf.MoveSpeed, true).ToString());

            // If battery of robot is < 40% and distance > 25 cells => try to recall him
            // Also recall if battery is < 10% (emergency if ThinkTree isn't working)
            if ((robot.needs.rest.CurLevel < 0.40f && !AIRobot_Helper.IsInDistance(this.Position, robot.Position, calcDistanceRestCheck)) ||
                 robot.needs.rest.CurLevel < 0.10f)
            {
                Notify_CallBotForRecharge();
            }
        }

        private int timerMoteThrow = 0;
        private void TryThrowBatteryMote(X2_AIRobot robot)
        {
            if (robot == null)
                return;


            timerMoteThrow--;
            if (timerMoteThrow > 0)
                return;
            timerMoteThrow = 300;

            
            float batteryLevel = robot.needs.rest.CurLevel;

            if (batteryLevel > 0.99f)
                return;

            if (batteryLevel > 0.90f)
                //MoteMaker.ThrowMetaIcon(this.Position, DefDatabase<ThingDef>.GetNamed("Mote_BatteryGreen"));
                MoteThrowHelper.ThrowBatteryGreen(this.Position.ToVector3(), Map, 0.8f);
            else if (batteryLevel > 0.70f)
                //MoteMaker.ThrowMetaIcon(this.Position, DefDatabase<ThingDef>.GetNamed("Mote_BatteryYellowYellow"));
                MoteThrowHelper.ThrowBatteryYellowYellow(this.Position.ToVector3(), Map, 0.8f);
            else if (batteryLevel > 0.35f)
                //MoteMaker.ThrowMetaIcon(this.Position, DefDatabase<ThingDef>.GetNamed("Mote_BatteryYellow"));
                MoteThrowHelper.ThrowBatteryYellow(this.Position.ToVector3(), Map, 0.8f);
            else
                //MoteMaker.ThrowMetaIcon(this.Position, DefDatabase<ThingDef>.GetNamed("Mote_BatteryRed"));
                MoteThrowHelper.ThrowBatteryRed(this.Position.ToVector3(), Map, 0.8f);
        }
        private void TryUpdateAllowedArea(X2_AIRobot robot)
        {
            if (robot.DestroyedOrNull() || !robot.Spawned)
                return;
            if (ForbidUtility.InAllowedArea(this.Position, robot))
                return;

            Messages.Message("AIRobot_MessageRechargeStationOutsideAreaRestriction".Translate(), robot, MessageTypeDefOf.RejectInput);

            //Remove area from robot
            robot.playerSettings.Notify_AreaRemoved(robot.playerSettings.AreaRestriction);
        }


        // Self healing
        //private int timerRepairDamage = 0;
        private void TryHealDamagedBodyPartOfRobot(X2_AIRobot robot)
        {

            //timerRepairDamage--;
            //if (timerRepairDamage > 0)
            //    return;
            //timerRepairDamage = 300;

            if (robot == null || !Verse.Gen.IsHashIntervalTick(robot, 300))
                return;


            IEnumerable<Hediff_Injury> hediff_injuries = (from x in robot.health.hediffSet.GetHediffs<Hediff_Injury>()
                                                          where x.CanHealFromTending() || x.CanHealNaturally()
                                                          select x);


            // Apply Treated, but not healing!
            if (robot.health.HasHediffsNeedingTend(false))
            {
                float quality = (Rand.Value);
                int batchPosition = 0;
                foreach (Hediff_Injury injury in from x in robot.health.hediffSet.GetInjuriesTendable()
                                                 orderby x.Severity descending
                                                 select x)
                {
                    injury.Tended(quality, batchPosition);
                    batchPosition++;
                    if (batchPosition >= 1)
                        break;
                }
            }

            // Apply healing
            if (hediff_injuries != null && hediff_injuries.Count() > 0)
            {
                Hediff_Injury hediff_Injury2 = hediff_injuries.RandomElement();

                float tendQuality = hediff_Injury2.TryGetComp<HediffComp_TendDuration>().tendQuality;
                float num2 = GenMath.LerpDouble(0f, 1f, 0.5f, 1.5f, Mathf.Clamp01(tendQuality));

                ////hediff_Injury2.Heal(22f * num2 * robot.HealthScale * 0.01f); -> At quality 0.5 --> 0.066 healed.
                //Log.Error("Calculation: " + (GenMath.LerpDouble(0f, 1f, 0.5f, 1.5f, Mathf.Clamp01(tendQuality)).ToString()));
                //Log.Error("Healing: " + (22f * num2 * robot.HealthScale * 0.1f).ToString());
                //Log.Error("PRE:" + hediff_Injury2.Severity.ToString());

                //hediff_Injury2.Heal(1f);
                hediff_Injury2.Heal(22f * num2 * robot.HealthScale * 0.1f * 0.5f);

                //Log.Error("POST:" + hediff_Injury2.Severity.ToString());

                // Throw Healing Mote
                MoteMaker.ThrowMetaIcon(this.Position, this.Map, ThingDefOf.Mote_HealingCross);
            }
        }

        #endregion

        #region Inspections

        public override string GetInspectString()
        {
            StringBuilder sb = new StringBuilder();

            string baseString = base.GetInspectString();

            if (baseString.NullOrEmpty())
                sb.Append("...");
            else
                sb.Append( base.GetInspectString() );

            if (robot != null && robot.Spawned)
                sb.AppendLine().Append("AIRobot_RobotIs".Translate() + " " + robot.LabelShort);
            else if (robotIsDestroyed)
                sb.AppendLine().Append("AIRobot_RobotIsDestroyed".Translate());

            return sb.ToString();
        }

        //private static Designator_Deconstruct designatorDeconstruct = new Designator_Deconstruct();

        /// <summary>
        /// This creates new selection buttons with a new graphic
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<Gizmo> GetGizmos()
        {
            int groupBaseKey = 31367676;

            foreach (Gizmo gizmo in base.GetGizmos())
                yield return gizmo;


            if (robot == null && !robotIsDestroyed)
            {
                // Key-Binding N - Start robot
                Command_Action act2;
                act2 = new Command_Action();
                act2.defaultLabel = lbSpawnOwner.Translate();
                act2.defaultDesc = txtSpawnOwner.Translate();
                act2.icon = UI_ButtonStart;
                act2.hotKey = KeyBindingDefOf.Misc4;
                act2.activateSound = SoundDef.Named("Click");
                act2.action = Button_SpawnBot;
                if (!Map.IsPlayerHome || Map.IsTempIncidentMap)
                {
                    act2.disabled = !Map.IsPlayerHome || Map.IsTempIncidentMap;
                    act2.disabledReason = txtDisabledBecauseNotHomeMap.Translate();
                }
                else
                {
                    act2.disabled = powerComp != null && !powerComp.PowerOn;
                    act2.disabledReason = txtNoPower.Translate();
                }
                act2.groupKey = groupBaseKey + 1;
                yield return act2;
            }

            if ((robot != null || robotSpawnedOnce) && !robotIsDestroyed)
            {
                // Key-Binding M - Deactivate robot
                Command_Action act1;
                act1 = new Command_Action();
                //act1.disabled = owner == null;
                //act1.disabledReason = txtNoOwner.Translate();
                act1.defaultLabel = lbSendOwnerToRecharge.Translate();
                act1.defaultDesc = txtSendOwnerToRecharge.Translate();
                act1.icon = UI_ButtonForceRecharge;
                act1.hotKey = KeyBindingDefOf.Misc7;
                act1.activateSound = SoundDef.Named("Click");
                act1.action = Notify_CallBotForShutdown;
                act1.disabled = powerComp != null && !powerComp.PowerOn;
                act1.disabledReason = txtNoPower.Translate();
                act1.groupKey = groupBaseKey + 2;
                yield return act1;
            }

            {
                // Key-Binding K - Deactivate ALL robots
                Command_Action act3;
                act3 = new Command_Action();
                act3.defaultLabel = lbRecallAllRobots.Translate();
                act3.defaultDesc = txtRecallAllRobots.Translate();
                act3.icon = UI_ButtonForceRechargeAll;
                act3.hotKey = KeyBindingDefOf.Misc8;
                act3.activateSound = SoundDef.Named("Click");
                act3.action = Button_CallAllBotsForShutdown;
                act3.disabled = powerComp != null && !powerComp.PowerOn;
                act3.disabledReason = txtNoPower.Translate();
                act3.groupKey = groupBaseKey + 3;
                yield return act3;
            }

            {
                // Key-Binding L - Activate ALL robots
                Command_Action act4;
                act4 = new Command_Action();
                act4.defaultLabel = lbActivateAllRobots.Translate();
                act4.defaultDesc = txtActivateAllRobots.Translate();
                act4.icon = UI_ButtonForceActivateAll;
                act4.hotKey = KeyBindingDefOf.Misc10;
                act4.activateSound = SoundDef.Named("Click");
                act4.action = Button_SpawnAllAvailableBots;
                act4.disabled = powerComp != null && !powerComp.PowerOn;
                act4.disabledReason = txtNoPower.Translate();
                act4.groupKey = groupBaseKey + 4;
                yield return act4;
            }

            {
                // Key-Binding O - Find robot
                Command_Action act5;
                act5 = new Command_Action();
                act5.defaultLabel = lbFindRobot.Translate();
                act5.defaultDesc = txtFindRobot.Translate();
                act5.icon = UI_ButtonSearch;
                act5.hotKey = KeyBindingDefOf.Misc11;
                act5.activateSound = SoundDef.Named("Click");
                act5.action = Button_FindRobot;
                act5.disabled = powerComp != null && !powerComp.PowerOn;
                act5.disabledReason = txtNoPower.Translate();
                act5.groupKey = groupBaseKey + 5;
                yield return act5;
            }


            if (DebugSettings.godMode)
            {
                // Key-Binding  - Reset robot
                Command_Action act9;
                act9 = new Command_Action();
                act9.defaultLabel = "(DEBUG) Reset destroyed robot";
                act9.defaultDesc = "";
                act9.icon = BaseContent.BadTex;
                act9.hotKey = null;
                act9.activateSound = SoundDef.Named("Click");
                act9.action = Button_ResetDestroyedRobot;
                act9.disabled = false;
                act9.disabledReason = "";
                act9.groupKey = groupBaseKey + 9;
                yield return act9;
            }

        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption fmo in base.GetFloatMenuOptions(selPawn))
                yield return fmo;

            if (this.robotIsDestroyed && this.disabledRobot != null)
                foreach (FloatMenuOption fmo in X2_AIRobot_disabled.GetFloatMenuOptions(selPawn, disabledRobot))
                    yield return fmo;
        }


        public void Button_FindRobot()
        {
            IntVec3 target;
            if (robotIsDestroyed && disabledRobot != null && disabledRobot.Spawned)
            {
                target = disabledRobot.Position;
                if (target != IntVec3.Invalid)
                    Find.CameraDriver.JumpToCurrentMapLoc(target);
                return;
            }

            if (robot != null && robot.Spawned)
            {
                target = robot.Position;
                if (target != IntVec3.Invalid)
                    Find.CameraDriver.JumpToCurrentMapLoc(target);
                return;
            }
            target = this.Position;
            if (target != IntVec3.Invalid)
                Find.CameraDriver.JumpToCurrentMapLoc(target);
            return;
        }


        public void Notify_RobotRepaired()
        {
            Button_ResetDestroyedRobot(false);
        }
        private void Button_ResetDestroyedRobot()
        {
            Button_ResetDestroyedRobot(true);
        }
        private void Button_ResetDestroyedRobot(bool spawn = true)
        {
            if (robot != null && !robot.Destroyed)
                robot.Destroy(DestroyMode.Vanish);

            this.robot = null;
            robotIsDestroyed = false;
            if (spawn)
                Button_SpawnBot();

            disabledRobot = null;
        }
        public void Notify_SpawnBot()
        {
            notify_spawnRequested = true;
        }
        private void Button_SpawnBot()
        {
            if (this.robot != null || robotIsDestroyed)
                return;

            if (spawnThingDef.NullOrEmpty())
            {
                Log.Error("Robot Recharge Station: Wanted to spawn robot, but spawnThingDef is null or empty!");
                return;
            }

            X2_AIRobot spawnedRobot = null;
            if (!IsRobotInContainer())
            {
                spawnedRobot = X2_Building_AIRobotCreator.CreateRobot(spawnThingDef, this.Position, this.Map, Faction.OfPlayer);
            }
            else
            {
                spawnedRobot = container[0] as X2_AIRobot;
                container.Remove(spawnedRobot);
                spawnedRobot = GenSpawn.Spawn(spawnedRobot, this.Position, this.Map) as X2_AIRobot;
            }

            this.robot = spawnedRobot;
            this.robot.rechargeStation = this;
            this.robotSpawnedOnce = true;

            this.SpawnRobotAfterRecharge = true;
        }
        private void Button_SpawnAllAvailableBots()
        {
            List<X2_Building_AIRobotRechargeStation> buildings = Map.listerThings.AllThings.OfType<X2_Building_AIRobotRechargeStation>().ToList();
            for (int i = buildings.Count; i > 0; i--)
            {
                X2_Building_AIRobotRechargeStation building = buildings[i - 1];
                building.Notify_SpawnBot();
            }
        }

        public void Notify_CallBotForRecharge()
        {
            Button_CallBotForShutdown(true);
            SpawnRobotAfterRecharge = true;
        }
        public void Notify_CallBotForShutdown()
        {
            Button_CallBotForShutdown(false);
        }
        private void Button_CallBotForShutdown(bool onlyRecharging)
        {
            SpawnRobotAfterRecharge = onlyRecharging;

            if (robot == null || robotIsDestroyed)
                return;

            if (!robot.Spawned) // || robot.HostFaction != null || robot.Faction != Faction.OfPlayer)
                return;

            // preparation: stop all other jobs
            if (robot.jobs == null)
            {
                Log.Error("Robot has no job driver!");
                return;
            }

            ThinkResult jobPackage;

            if (onlyRecharging)
            {
                // Go Recharging
                X2_JobGiver_RechargeEnergy getRecharge = new X2_JobGiver_RechargeEnergy();
                jobPackage = getRecharge.TryIssueJobPackage(robot, new JobIssueParams());
            }
            else
            {
                // Go Despawning
                X2_JobGiver_Return2BaseDespawn getRest = new X2_JobGiver_Return2BaseDespawn();
                jobPackage = getRest.TryIssueJobPackage(robot, new JobIssueParams());
            }

            // Do nothing more, if the jobPackage or job is null, or if the current job is already the target job
            if (jobPackage == null || jobPackage.Job == null || (robot.CurJob != null && robot.CurJob.def == jobPackage.Job.def))
                return;
            

            robot.jobs.StopAll();

            // Do Job with JobGiver  -> Force owner to recharge
            robot.jobs.StartJob(jobPackage.Job);
        }
        private void Button_CallAllBotsForShutdown()
        {
            List<X2_Building_AIRobotRechargeStation> buildings = Map.listerThings.AllThings.OfType<X2_Building_AIRobotRechargeStation>().ToList();
            for (int i = buildings.Count; i > 0; i--)
            {
                X2_Building_AIRobotRechargeStation building = buildings[i - 1];
                building.Notify_CallBotForShutdown();
            }
        }

        private void ClearContainer()
        {
            if (container != null && container.Count > 0)
                container.Clear();

            container = new List<X2_AIRobot>();
        }

        private ThingDef thingDefSpawn; 
        private bool IsRobotInContainer()
        {
            if (container == null)
            {
                ClearContainer();
                return false;
            }

            if (spawnThingDef.NullOrEmpty())
                return false;

            if (thingDefSpawn == null)
                thingDefSpawn = DefDatabase<ThingDef>.GetNamedSilentFail(spawnThingDef);

            if (thingDefSpawn == null || NumContained(container, thingDefSpawn) == 0)
                return false;

            return true;
        }

        // extracted from ThingContainer
        public int NumContained(List<X2_AIRobot> workList, ThingDef def)
        {
            int num = 0;
            for (int i = 0; i < workList.Count; i++)
            {
                if (workList[i].def == def)
                {
                    num += workList[i].stackCount;
                }
            }
            return num;
        }

        #endregion
        


    }
}
