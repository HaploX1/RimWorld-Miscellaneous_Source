using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EndGame
{

    public class CompEndGame : ThingComp
    {
        private bool isActive = false;
        private int ticksUntilNextIncident = -1;
        private int deactivateAtGameTick = -1;

        // internal fixed value
        private float reducedDanger_RaidPointMultiplier = 0.66f;

        private CompPowerTrader powerComp;

        public CompProperties_EndGame Props
        {
            get
            {
                return (CompProperties_EndGame)props;
            }
        }

        private List<IncidentDef> GetPossibleIncidentDefs
        {
            get
            {
                return Props.possibleIncidents;
            }
        }

        public int GetNewTicksUntilNextIncident
        {
            get
            {
                // Between day 26 and max --> Increase danger : half the ticks between incidents
                if (IsDangerIncreased)
                    return Props.ticksBetweenIncidents.RandomInRange / 2;

                // Between day 0 and 10 --> Reduced danger : double the ticks between incidents
                if (IsDangerReduced)
                    return Props.ticksBetweenIncidents.RandomInRange * 2;

                // normal : between day 10 and 26
                return Props.ticksBetweenIncidents.RandomInRange;
            }
        }

        public bool IsActivatingPossible
        {
            get
            {
                return !IsActive && parent.Spawned && (powerComp == null || powerComp.PowerOn) && parent.Faction == Faction.OfPlayer;
            }
        }

        public bool IsActive
        {
            get
            {
                return isActive;
            }
            set
            {
                if (value && !isActive)
                    deactivateAtGameTick = Mathf.RoundToInt((float)Find.TickManager.TicksGame + Props.maxDaysActive * GenDate.TicksPerDay);
                
                isActive = value;
            }
        }

        public bool IsDangerIncreased
        {
            get
            {
                // Between day 26 and max --> Increase danger : half the ticks between incidents
                return IsActive && GetRemainingTicks < (Props.maxDaysActive - Props.dangerIncreaseOnDay) * GenDate.TicksPerDay;
            }
        }
        public bool IsDangerReduced
        {
            get
            {
                // Between day 0 and 10 --> Reduced danger : double the ticks between incidents
                return !IsActive || GetRemainingTicks > (Props.maxDaysActive - Props.dangerLowUntilDay) * GenDate.TicksPerDay;
            }
        }

        public int GetRemainingTicks
        {
            get
            {
                return deactivateAtGameTick - Find.TickManager.TicksGame;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.GetComp<CompPowerTrader>();

            if (ticksUntilNextIncident < 0)
                ticksUntilNextIncident = GetNewTicksUntilNextIncident;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look<bool>(ref isActive, "isActive");
            Scribe_Values.Look<int>(ref deactivateAtGameTick, "deactivateAtGameTick", -1);
            Scribe_Values.Look<int>(ref ticksUntilNextIncident, "ticksUntilNextIncident", -1);

            //if (Scribe.mode == LoadSaveMode.PostLoadInit)
            //{
            //}
        }

        public override void CompTick()
        {
            base.CompTick();
            DoEndGameHandling(1);
        }

        private void DoEndGameHandling(int interval)
        {
            if ( !IsActive && !IsActivatingPossible )
                return;

            if ( IsActive )
            {
                int remainingTicks = GetRemainingTicks;
                if (remainingTicks <= 1200)
                {
                    Find.ActiveLesson.Deactivate();
                    if (remainingTicks <= 1200)
                    {
                        // last 20s: every second a sound
                        if (remainingTicks % 60 == 0)
                            SoundDefOf.Click.PlayOneShotOnCamera(null);
                    }
                    if (remainingTicks == 300)
                    {
                        ScreenFader.StartFade(Color.white, 5f);
                    }
                }

                if (remainingTicks <= 0)
                {
                    // Endgame finished
                    IsActive = false;

                    Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;

                    TaleRecorder.RecordTale(DefDatabase<TaleDef>.GetNamedSilentFail("LaunchedShip"));

                    StringBuilder stringBuilder = new StringBuilder();
                    string victoryText = "EndGame_Victory".Translate(GameVictoryUtility.PawnsLeftBehind());

                    GameVictoryUtility.ShowCredits(victoryText);

                    //GenGameEnd.EndGameDialogMessage(victoryText, true);

                    // Last: Destroy this building --> disabled
                    //parent.Destroy(DestroyMode.Vanish);
                    return;
                }

                if (ticksUntilNextIncident < 0)
                {
                    Try2InitiateRaid();
                    ticksUntilNextIncident = GetNewTicksUntilNextIncident;
                    return;
                }

                ticksUntilNextIncident -= interval;
                return;
            } 
        }


        public override string CompInspectStringExtra()
        {
            if (IsActive)
            {
                string str = "EndGame_RemainingTime".Translate( GetTimeString(GetRemainingTicks) );

                if (DebugSettings.godMode)
                    str += "\n" + "DEBUG: Next Raid in " + (ticksUntilNextIncident).ToStringTicksToDays();

                bool isReduced = IsDangerReduced;
                bool isIncreased = IsDangerIncreased;

                if (DebugSettings.godMode && isReduced)
                    str += "\n *-- Reduced Danger --*";
                if (DebugSettings.godMode && !isReduced && !isIncreased)
                    str += "\n **- Normal Danger -**";
                if (DebugSettings.godMode && isIncreased)
                    str += "\n *** Increased Danger ***";

                return str;
            }
            else
            {
                return null;
            }
        }

        private string GetTimeString(int ticks)
        {
            if (ticks < GenDate.TicksPerHour)
                return GenTicks.TicksToSeconds(ticks).ToString("F0") + "s";
            if (ticks < GenDate.TicksPerDay)
                return (ticks / GenDate.TicksPerHour).ToString("F0") + "h";

            return ticks.ToStringTicksToDays("F1");
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
                yield return gizmo;

            yield return new Command_Action
            {
                action = delegate ()
                {
                    string text = "EndGameWarning";
                    //if (!Find.Storyteller.difficulty.allowBigThreats)
                    //    text = "EndGameWarningPacifist";

                    DiaNode diaNode = new DiaNode(text.Translate());
                    DiaOption diaOption = new DiaOption("Confirm".Translate());
                    diaOption.action = delegate ()
                    {
                        StartEndGame();
                    };
                    diaOption.resolveTree = true;
                    diaNode.options.Add(diaOption);
                    DiaOption diaOption2 = new DiaOption("GoBack".Translate());
                    diaOption2.resolveTree = true;
                    diaNode.options.Add(diaOption2);
                    Find.WindowStack.Add(new Dialog_NodeTree(diaNode, true, false, null));
                },
                defaultLabel = "CommandStartEndGame".Translate(),
                defaultDesc = "CommandStartEndGameDesc".Translate(),
                hotKey = KeyBindingDefOf.Misc1,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/EndGame_Transmit_MenuIcon", true),
                disabled = !IsActivatingPossible,
                disabledReason = "CommandStartEndGame_DisabledReason".Translate(),
            };

            if (!Prefs.DevMode || !DebugSettings.godMode)
                yield break;

            yield return new Command_Action
            {
                defaultLabel = "Debug: Initiate Raid",
                defaultDesc = "Initiate a new Raid.",
                action = delegate
                {
                    Try2InitiateRaid();
                }
            };

            yield return new Command_Action
            {
                defaultLabel = "Debug: Reduce time",
                defaultDesc = "Reduce the remaining time by 1/2 day.",
                action = delegate { deactivateAtGameTick -= (int)(GenDate.TicksPerDay / 2); },
                disabled = !IsActive,
                disabledReason = "Not Active..."
            };
        }


        public void StartEndGame()
        {
            IsActive = true;
        }

        public void Try2InitiateRaid()
        {
            Map map = parent.Map;
            if (map == null)
                return;

            Faction faction;
            if (!TryFindEnemyFaction(out faction))
                return;

            //IntVec3 spawnSpot;
            //if (!TryFindSpawnSpot(map, out spawnSpot))
            //    return;


            IncidentParms raidParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
            raidParms.forced = true;
            raidParms.faction = faction;
            raidParms.raidStrategy = null;
            raidParms.raidArrivalMode = null;
            //raidParms.spawnCenter = spawnSpot;
            raidParms.points = Mathf.Max(raidParms.points * Props.raidPointsFactorRange.RandomInRange, faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat));
            //raidParms.points = 20000.0f; // DEBUG
            raidParms.pawnGroupMakerSeed = Rand.Int;

            // Between day 0 and 10 --> Reduced danger : reduced raid points
            if (IsDangerReduced)
                raidParms.points = (float)(raidParms.points * reducedDanger_RaidPointMultiplier);

            QueuedIncident qi = new QueuedIncident(new FiringIncident(Props.possibleIncidents.RandomElement(), null, raidParms), Find.TickManager.TicksGame + 10, 0);
            Find.Storyteller.incidentQueue.Add(qi);
        }

        private bool TryFindEnemyFaction(out Faction enemyFac)
        {
            float rnd = Rand.Value;

            if (rnd < 0.25)
            {
                //Mechanoids
                enemyFac = Faction.OfMechanoids;
                return true;
            }  

            return (from f in Find.FactionManager.AllFactions
                    where !f.def.hidden && !f.defeated && f.HostileTo(Faction.OfPlayer)
                    select f).TryRandomElement(out enemyFac);
        }

        //private IncidentDef FindRandomIncident(Map map)
        //{
        //    return (from def in DefDatabase<IncidentDef>.AllDefs
        //            where def.TargetAllowed(map) && def.category == IncidentCategoryDefOf.ThreatBig
        //            select def).RandomElement();
        //}
    }
}
