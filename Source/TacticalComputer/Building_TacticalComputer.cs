using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
//using Verse.Sound;
using RimWorld;

using CommonMisc; // needed for Radar


namespace TacticalComputer
{
    [StaticConstructorOnStartup]
    public class Building_TacticalComputer : Building
    {

        #region Variables

        private CompPowerTrader power;

        private IEnumerable<Pawn> founds_PawnColonist;  // 0
        private IEnumerable<Pawn> founds_PawnPrisoner;  // 1
        private IEnumerable<Pawn> founds_PawnEnemy;     // 2
        private IEnumerable<Pawn> founds_PawnFriendly;  // 3
        private IEnumerable<Pawn> founds_PawnColonyAnimal;  // 4

        private int founds_ActiveCount = 0;

        // Grafic-data filled in SpawnSetup()
        private static Texture2D texUI_PawnColonist;
        private static Texture2D texUI_PawnPrisoner;
        private static Texture2D texUI_PawnEnemy;
        private static Texture2D texUI_PawnFriendly;
        private static Texture2D texUI_PawnAnimal;
        private static Texture2D texUI_AnomalyAssignPawns;
        private static Texture2D texUI_AnomalySendPawns;

        private string txtOffline = "-= Offline =-";
        private string txtColonist = "Colonist";
        private string txtPrisoner = "Prisoner";
        private string txtEnemy = "Enemy";
        private string txtFriendly = "Friendly";
        private string txtAnimal = "Animal";
        private string txtSwitch2Colonist = "Switch to next colonist.";
        private string txtSwitch2Prisoner = "Switch to next prisoner.";
        private string txtSwitch2Enemy = "Switch to next enemy.";
        private string txtSwitch2Friendly = "Switch to next friendly.";
        private string txtSwitch2Animal = "Switch to next colony animal.";

        private static string JobDefName_CallToArms = "GotoCellAndDraft";
        
        public bool HasPower
        {
            get
            {
                return (power == null || power.PowerOn);
            }
        }

        public List<Pawn> pawnsToSendToAnomaly = new List<Pawn>();

        #endregion


        #region Setup


        /// <summary>
        /// Do something after the object is spawned
        /// </summary>
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            LongEventHandler.ExecuteWhenFinished(SpawnSetup_Part2);

        }

        /// <summary>
        /// This is called seperately when the Mod-Thread is done.
        /// It is needed to be seperately from SpawnSetup, so that the graphics can be found
        /// </summary>
        private void SpawnSetup_Part2()
        {

            power = GetComp<CompPowerTrader>();

            texUI_PawnColonist = ContentFinder<Texture2D>.Get("UI/Commands/TacticalComputer/UI_FindPawnColonist", true);
            texUI_PawnPrisoner = ContentFinder<Texture2D>.Get("UI/Commands/TacticalComputer/UI_FindPawnPrisoner", true);
            texUI_PawnEnemy = ContentFinder<Texture2D>.Get("UI/Commands/TacticalComputer/UI_FindPawnEnemy", true);
            texUI_PawnFriendly = ContentFinder<Texture2D>.Get("UI/Commands/TacticalComputer/UI_FindPawnFriendly", true);
            texUI_PawnAnimal = ContentFinder<Texture2D>.Get("UI/Commands/TacticalComputer/UI_FindPawnAnimal", true);

            texUI_AnomalyAssignPawns = ContentFinder<Texture2D>.Get("UI/Commands/TacticalComputer/UI_AnomalyAssignPawns", true);
            texUI_AnomalySendPawns = ContentFinder<Texture2D>.Get("UI/Commands/TacticalComputer/UI_AnomalySendPawns", true);

            txtOffline = "TacticalComputer_Offline".Translate(); // "-= Offline =-";
            txtColonist = "TacticalComputer_Colonist".Translate(); // "Colonist";
            txtPrisoner = "TacticalComputer_Prisoner".Translate(); // "Prisoner";
            txtEnemy = "TacticalComputer_Enemy".Translate(); // "Enemy";
            txtFriendly = "TacticalComputer_Friendly".Translate(); // "Friendly";
            txtAnimal = "TacticalComputer_Animal".Translate(); // "Animal";
            txtSwitch2Colonist = "TacticalComputer_SwitchToNextColonist".Translate(); // "Switch to next colonist.";
            txtSwitch2Prisoner = "TacticalComputer_SwitchToNextPrisoner".Translate(); // "Switch to next prisoner.";
            txtSwitch2Enemy = "TacticalComputer_SwitchToNextEnemy".Translate(); // "Switch to next enemy.";
            txtSwitch2Friendly = "TacticalComputer_SwitchToNextFriendly".Translate(); // "Switch to next friendly.";
            txtSwitch2Animal = "TacticalComputer_SwitchToNextColonyAnimal".Translate(); // "Switch to next colony animal.";
            

        }

        #endregion


        #region Inspection

        public override string GetInspectString()
        {
            string baseString = base.GetInspectString();

            StringBuilder stringBuilder = new StringBuilder();

            if (!baseString.NullOrEmpty())
            {
                stringBuilder.Append(base.GetInspectString());
                stringBuilder.AppendLine();
            }
            
            if (SpawnedOrAnyParentSpawned && Map != null && (power == null || power.PowerOn))
            {

                stringBuilder.Append(txtColonist + ": ");
                stringBuilder.Append(Radar.FindColonistPawnsCount(Map).ToString("000"));

                stringBuilder.Append("  ");
                stringBuilder.Append(txtPrisoner + ": ");
                stringBuilder.Append(Radar.FindPrisonerPawnsCount(Map).ToString("000"));

                stringBuilder.Append("  ");
                stringBuilder.Append(txtAnimal + ": ");
                stringBuilder.Append(Radar.FindColonyAnimalsCount(Map).ToString("000"));

                stringBuilder.AppendLine();

                stringBuilder.Append(txtEnemy + ": ");
                stringBuilder.Append(Radar.FindEnemyPawnsCount(Map).ToString("000"));
                stringBuilder.Append("  ");

                stringBuilder.Append(txtFriendly + ": ");
                stringBuilder.Append(Radar.FindFriendlyPawnsCount(Map).ToString("000"));
            }
            else
            {
                stringBuilder.Append(txtOffline);
            }

            //Debug.LogError("0");

            return stringBuilder.ToString();
        }


        /// <summary>
        /// This creates float menus for the building
        /// </summary>
        /// <param name="myPawn"></param>
        /// <returns></returns>
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            // do nothing if not of colony
            if (myPawn.Faction != Faction.OfPlayer)
                yield break;

            // base float menus
            foreach (FloatMenuOption fmo in base.GetFloatMenuOptions(myPawn))
                yield return fmo;
        }



        /// <summary>
        /// This creates new selection buttons with a new graphic
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo c in base.GetGizmos())
                yield return c;

            int baseGroupKey = 31317600;

            // Key-Binding 2 - Switch to colonist
            Command_Action opt2;
            opt2 = new Command_Action();
            opt2.icon = texUI_PawnColonist;
            opt2.defaultDesc = txtSwitch2Colonist;
            opt2.hotKey = KeyBindingDefOf.Misc2; //H
            opt2.activateSound = SoundDef.Named("Click");
            opt2.action = JumpTarget0;
            opt2.disabled = !(power == null || power.PowerOn);
            opt2.disabledReason = txtOffline;
            opt2.groupKey = baseGroupKey + 0;
            yield return opt2;

            // Key-Binding J - Switch to prisoner
            Command_Action opt5;
            opt5 = new Command_Action();
            opt5.icon = texUI_PawnPrisoner;
            opt5.defaultDesc = txtSwitch2Prisoner;
            opt5.hotKey = KeyBindingDefOf.Misc5; //J
            opt5.activateSound = SoundDef.Named("Click");
            opt5.action = JumpTarget1;
            opt5.disabled = !(power == null || power.PowerOn);
            opt5.disabledReason = txtOffline;
            opt5.groupKey = baseGroupKey + 1;
            yield return opt5;

            // Key-Binding K - Switch to enemy
            Command_Action opt8;
            opt8 = new Command_Action();
            opt8.icon = texUI_PawnEnemy;
            opt8.defaultDesc = txtSwitch2Enemy;
            opt8.hotKey = KeyBindingDefOf.Misc8; //K
            opt8.activateSound = SoundDef.Named("Click");
            opt8.action = JumpTarget2;
            opt8.disabled = !(power == null || power.PowerOn);
            opt8.disabledReason = txtOffline;
            opt8.groupKey = baseGroupKey + 2;
            yield return opt8;

            // Key-Binding L - Switch to friendly
            Command_Action opt10;
            opt10 = new Command_Action();
            opt10.icon = texUI_PawnFriendly;
            opt10.defaultDesc = txtSwitch2Friendly;
            opt10.hotKey = KeyBindingDefOf.Misc10; //L
            opt10.activateSound = SoundDef.Named("Click");
            opt10.action = JumpTarget3;
            opt10.disabled = !(power == null || power.PowerOn);
            opt10.disabledReason = txtOffline;
            opt10.groupKey = baseGroupKey + 3;
            yield return opt10;

            // Key-Binding 12 - Switch to animal
            Command_Action opt12;
            opt12 = new Command_Action();
            opt12.icon = texUI_PawnAnimal;
            opt12.defaultDesc = txtSwitch2Animal;
            opt12.hotKey = KeyBindingDefOf.Misc11; //O
            opt12.activateSound = SoundDef.Named("Click");
            opt12.action = JumpTarget4;
            opt12.disabled = !(power == null || power.PowerOn);
            opt12.disabledReason = txtOffline;
            opt12.groupKey = baseGroupKey + 4;
            yield return opt12;
            
        }

        #endregion


        #region Functions

        private void JumpTarget0()
        {
            JumpToTarget(0);
        }
        private void JumpTarget1()
        {
            JumpToTarget(1);
        }
        private void JumpTarget2()
        {
            JumpToTarget(2);
        }
        private void JumpTarget3()
        {
            JumpToTarget(3);
        }
        private void JumpTarget4()
        {
            JumpToTarget(4);
        }

        /// <summary>
        /// Make the camera jump to selected pawn
        /// </summary>
        /// <param name="Selection"></param>
        private void JumpToTarget(int Selection)
        {
            int founds_count;
            IEnumerable<Pawn> founds_Pawns;
            
            switch (Selection)
            {
                case 0:
                    founds_PawnColonist = Radar.FindColonistPawns(Map);
                    founds_Pawns = founds_PawnColonist;
                    break;
                case 1:
                    founds_PawnPrisoner = Radar.FindPrisonerPawns(Map);
                    founds_Pawns = founds_PawnPrisoner;
                    break;
                case 2:
                    founds_PawnEnemy = Radar.FindEnemyPawns(Map);
                    founds_Pawns = founds_PawnEnemy;
                    break;
                case 3:
                    founds_PawnFriendly = Radar.FindFriendlyPawns(Map);
                    founds_Pawns = founds_PawnFriendly;
                    break;
                case 4:
                    founds_PawnColonyAnimal = Radar.FindColonyAnimals(Map);
                    founds_Pawns = founds_PawnColonyAnimal;
                    break;
                default:
                    return;
            }

            if (founds_Pawns == null || founds_Pawns.Count() == 0)
            {
                founds_ActiveCount = 0;
                return;
            }

            founds_count = founds_Pawns.Count();

            founds_ActiveCount += 1;
            if (founds_ActiveCount >= founds_count)
                founds_ActiveCount = 0;

            Pawn selectedPawn = founds_Pawns.ElementAt(founds_ActiveCount);

            // Jump to selected pawn
            Find.CameraDriver.JumpToCurrentMapLoc(selectedPawn.Position);

            //// Mark selected pawns => Not good, deselects the tactical computer!
            //Find.Selector.ClearSelection();
            //Find.Selector.Select(selectedPawn);

        }


        /// <summary>
        /// Call colonists with weapons to me and draft them
        /// </summary>
        private void CallColonistsToArms(bool callArmed = true)
        {
            CallColonistsToArmsToThing(this, callArmed);
        }
        private void CallColonistsToArms()
        {
            CallColonistsToArmsToThing(this, true);
        }
        public static void CallColonistsToArmsToThing(Thing thing, bool callArmed = true)
        {

            IEnumerable<Pawn> foundPawns = thing.Map.mapPawns.FreeColonistsSpawned.Where(p => !p.Dead && !p.Downed && !p.IsBurning());
            if (foundPawns == null || foundPawns.Count() == 0)
                return;

            Pawn lookTarget = null;

            int countPawnsCalled = 0;
            foreach (Pawn workPawn in foundPawns)
            {
                // don't call pawns without weapon
                if (callArmed && workPawn.equipment.Primary == null)
                    continue;

                // don't call pawns with weapon
                if (!callArmed && workPawn.equipment.Primary != null)
                    continue;

                // don't call pawn if incapacitated or in medical bed
                if (workPawn.Downed || workPawn.Dead || (workPawn.InBed() && workPawn.CurrentBed().Medical))
                    continue;

                Job job = new Job(DefDatabase<JobDef>.GetNamed(JobDefName_CallToArms), thing);
                workPawn.jobs.TryTakeOrderedJob(job);
                countPawnsCalled++;

                if (lookTarget == null)
                    lookTarget = workPawn;
            }

            if (countPawnsCalled > 0)
                Messages.Message("TacticalComputer_MessageCalledColonists".Translate(new object[] { countPawnsCalled.ToString() }), lookTarget, MessageTypeDefOf.NeutralEvent);


        }

        #endregion


    }


}
