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
//using RimWorld.Planet;

using CommonMisc; // needed for Radar


namespace TacticalComputer
{
    [StaticConstructorOnStartup]
    public class Building_TacticalComputerExtensionTerminal : Building
    {

        #region Variables

        private CompPowerTrader power;

        private static Texture2D texUI_CallToArmsToTacticalComputer;
        private static Texture2D texUI_CallToArmsToMe;
        private static Texture2D texUI_CallNotArmedToTacticalComputer;
        private static Texture2D texUI_CallNotArmedToMe;


        private string txtCallColonistsToArmsToMe = "Call colonists to arms.";
        private string txtCallColonistsToArmsToTacticalComputer = "Call colonists to arms.";
        private string txtCallColonistsNotArmedToMe = "Call not armed colonists.";
        private string txtCallColonistsNotArmedToTacticalComputer = "Call not armed colonists.";
        private string txtOffline = "-= Offline =-";

        private string txtFloatMenu_OrderToSleep = "Order colonist to sleep";
        private string txtFloatMenu_OrderToEat = "Order colonist to eat";
        private string txtFloatMenu_GetNewDoorKey = "Take new door key";

        private bool jumpCameraBack;

        public bool HasPower
        {
            get
            {
                return (power == null || power.PowerOn);
            }
        }

        #endregion


        #region Setup

        /// <summary>
        /// Do something after the object is spawned
        /// </summary>
        public override void SpawnSetup( Map map, bool respawningAfterLoad)
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

            texUI_CallToArmsToTacticalComputer = ContentFinder<Texture2D>.Get("UI/Commands/TacticalComputer/UI_CallArmedToMainComputer", true);
            texUI_CallToArmsToMe = ContentFinder<Texture2D>.Get("UI/Commands/TacticalComputer/UI_CallArmed", true);
            texUI_CallNotArmedToTacticalComputer = ContentFinder<Texture2D>.Get("UI/Commands/TacticalComputer/UI_CallNotArmedToMainComputer", true);
            texUI_CallNotArmedToMe = ContentFinder<Texture2D>.Get("UI/Commands/TacticalComputer/UI_CallNotArmed", true);

            txtOffline = "TacticalComputer_Offline".Translate(); // "-= Offline =-";
            txtCallColonistsToArmsToMe = "TacticalComputer_CallColonistsToArmsToMe".Translate();
            txtCallColonistsToArmsToTacticalComputer = "TacticalComputer_CallColonistsToArmsToMainComputer".Translate();
            txtCallColonistsNotArmedToMe = "TacticalComputer_CallColonistsNotArmedToMe".Translate();
            txtCallColonistsNotArmedToTacticalComputer = "TacticalComputer_CallColonistsNotArmedToMainComputer".Translate();

            txtFloatMenu_OrderToSleep = "TacticalComputer_Float_OrderToSleep".Translate();
            txtFloatMenu_OrderToEat = "TacticalComputer_Float_OrderToEat".Translate();
            txtFloatMenu_GetNewDoorKey = "TacticalComputer_Float_GetNewDoorKey".Translate();
        }

        #endregion


        #region Inspection

        /// <summary>
        /// This creates float menus for the building
        /// </summary>
        /// <param name="myPawn"></param>
        /// <returns></returns>
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            // base float menus
            foreach (FloatMenuOption fmo in base.GetFloatMenuOptions(myPawn))
                yield return fmo;

            if (myPawn.Faction != Faction.OfPlayer)
                yield break;

            //// Only allow to work further, if a tactical computer (PawnScanner) is found
            //IEnumerable<Building_TacticalComputer> foundTacticalComputers = myPawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_TacticalComputer>().Where(tc => tc.HasPower);
            //if (foundTacticalComputers == null || foundTacticalComputers.Count() == 0)
            //    yield break;

        }




        /// <summary>
        /// This creates new selection buttons with a new graphic
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Command c in base.GetGizmos())
                yield return c;

            int baseGroupKey = 31317600;

            IEnumerable<Building_TacticalComputer> foundTacticalComputers = Map.listerBuildings.AllBuildingsColonistOfClass<Building_TacticalComputer>().Where(tc => tc.HasPower);

            // Key-Binding B - Call to arms (call to me)
            Command_Action opt1;
            opt1 = new Command_Action();
            opt1.icon = texUI_CallToArmsToMe;
            opt1.defaultDesc = txtCallColonistsToArmsToMe;
            opt1.hotKey = KeyBindingDefOf.Misc4; //N
            opt1.activateSound = SoundDef.Named("Click");
            opt1.action = CallColonistsToArmsToThisTerminal;
            opt1.disabled = (!(power == null || power.PowerOn)) ||
                            (foundTacticalComputers == null || foundTacticalComputers.Count() == 0);
            opt1.disabledReason = txtOffline;
            opt1.groupKey = baseGroupKey + 5;
            yield return opt1;

            // Key-Binding H - Call to arms (call to tactical computer)
            Command_Action opt2;
            opt2 = new Command_Action();
            opt2.icon = texUI_CallToArmsToTacticalComputer;
            opt2.defaultDesc = txtCallColonistsToArmsToTacticalComputer;
            opt2.hotKey = KeyBindingDefOf.Misc5; //J
            opt2.activateSound = SoundDef.Named("Click");
            opt2.action = CallColonistsToArmsToTacticalComputer;
            opt2.disabled = (!(power == null || power.PowerOn)) ||
                            (foundTacticalComputers == null || foundTacticalComputers.Count() == 0);
            opt2.disabledReason = txtOffline;
            opt2.groupKey = baseGroupKey + 6;
            yield return opt2;

            // Key-Binding N - Call not armed (call to me)
            Command_Action opt4;
            opt4 = new Command_Action();
            opt4.icon = texUI_CallNotArmedToMe;
            opt4.defaultDesc = txtCallColonistsNotArmedToMe;
            opt4.hotKey = KeyBindingDefOf.Misc7; //M
            opt4.activateSound = SoundDef.Named("Click");
            opt4.action = CallColonistsNotArmedToThisTerminal;
            opt4.disabled = (!(power == null || power.PowerOn)) ||
                            (foundTacticalComputers == null || foundTacticalComputers.Count() == 0);
            opt4.disabledReason = txtOffline;
            opt4.groupKey = baseGroupKey + 7;
            yield return opt4;

            // Key-Binding J - Call not armed (call to tactical computer)
            Command_Action opt5;
            opt5 = new Command_Action();
            opt5.icon = texUI_CallNotArmedToTacticalComputer;
            opt5.defaultDesc = txtCallColonistsNotArmedToTacticalComputer;
            opt5.hotKey = KeyBindingDefOf.Misc8; //K
            opt5.activateSound = SoundDef.Named("Click");
            opt5.action = CallColonistsNotArmedToTacticalComputer;
            opt5.disabled = (!(power == null || power.PowerOn)) ||
                            (foundTacticalComputers == null || foundTacticalComputers.Count() == 0);
            opt5.disabledReason = txtOffline;
            opt5.groupKey = baseGroupKey + 8;
            yield return opt5;

        }

        #endregion


        #region Functions

        private void CallColonistsToArmsToTacticalComputer()
        {
            Find.Selector.ClearSelection();
            CallColonistsToArms(true, true);
        }
        private void CallColonistsToArmsToThisTerminal()
        {
            Find.Selector.ClearSelection();
            CallColonistsToArms(false, true);
        }
        private void CallColonistsNotArmedToTacticalComputer()
        {
            Find.Selector.ClearSelection();
            CallColonistsToArms(true, false);
        }
        private void CallColonistsNotArmedToThisTerminal()
        {
            Find.Selector.ClearSelection();
            CallColonistsToArms(false, false);
        }

        private void CallColonistsToArms(bool sendToTacticalComputer, bool callArmedPawns)
        {
            // Only allow to work further, if a tactical computer is found
            IEnumerable<Building_TacticalComputer> foundTacticalComputers = Map.listerBuildings.AllBuildingsColonistOfClass<Building_TacticalComputer>().Where(tc => tc.HasPower);
            if (foundTacticalComputers == null || foundTacticalComputers.Count() == 0)
                return;

            // If more than one, select one randomly
            Building_TacticalComputer selectedTacticalComputer = foundTacticalComputers.RandomElement();

            // Call the colonists to the selected tactical computer
            if (sendToTacticalComputer)
            {
                Building_TacticalComputer.CallColonistsToArmsToThing(selectedTacticalComputer, callArmedPawns);

                // Jump to selected pawn
                if (!jumpCameraBack)
                    Find.CameraDriver.JumpToVisibleMapLoc(selectedTacticalComputer.Position);
                else
                    Find.CameraDriver.JumpToVisibleMapLoc(this.Position);

                jumpCameraBack = !jumpCameraBack;

                jumpCameraBack = false; // disabled for now...
            }
            else
            {
                Building_TacticalComputer.CallColonistsToArmsToThing(this, callArmedPawns);
                jumpCameraBack = false;
            }

        }

        #endregion
    }
}



