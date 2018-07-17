using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed
using Verse.AI; // Needed when you do something with the AI
//using Verse.Sound; // Needed when you do something with the Sound


// Reminder MoteMaker - Feedbacks:
// (From: Pawn_PlayerController.TakeOrderedJob(..))
    //MoteMaker.ThrowFeedback(newJob.targetA.Loc, "FeedbackAttack");
    //MoteMaker.ThrowFeedback(newJob.targetA.Loc, "FeedbackGoto");
    //MoteMaker.ThrowFeedback(newJob.targetA.Loc, "FeedbackEquip");

namespace ColonistSelections
{
    /// <summary>
    /// This is a MapComponent. 
    /// These will automatically be initialized at map creation!
    /// Caution: Does not work on saved games, where it wasn't available at map creation time 
    ///          as it will be saved and because of that isn't in the savegame!
    /// </summary>
    public class MapComponent_ColonistSelections : MapComponent
    {
        private const string tanslationGroupSaved = "ColonistGroups_GroupSaved";
        private const string tanslationGroupNotSet_PositionMode = "ColonistGroups_GroupNotSet_PositionMode";
        private const string translationGroupInvalid_PositionMode = "ColonistGroups_GroupInvalid_PositionMode";
        private const string tanslationGroupNotSet_SelectionMode = "ColonistGroups_GroupNotSet_SelectionMode";
        private const string translationGroupInvalid_SelectionMode = "ColonistGroups_GroupInvalid_SelectionMode";
        private const string tanslationGroupPositioned = "ColonistGroups_GroupPositioned";
        private const string tanslationGroupSelected = "ColonistGroups_GroupSelected";
        private const string tanslationPawnsUndrafted = "ColonistGroups_PawnsUndrafted";

        private const string translationGroupSelectionActive = "ColonistGroups_GroupSelectionActive";
        private const string translationGroupPositioningActive = "ColonistGroups_GroupPositioningActive";

        private const string translationLetterInfoOpenHelp = "ColonistGroups_Letter_InfoOpenHelp";
        private const string translationMessageInfoOpenHelp = "ColonistGroups_Message_InfoOpenHelp";

        private const string translationHelp_prt1 = "ColonistGroups_GameStartHelp_prt1";
        private const string translationHelp_prt2 = "ColonistGroups_GameStartHelp_prt2";
        private const string translationHelp_prt3 = "ColonistGroups_GameStartHelp_prt3";
        private const string translationHelp_prt4 = "ColonistGroups_GameStartHelp_openHelp";

        // Selection Groups (Shift+key => add drafted pawns to pawn and cell lists
        private bool keyPressed_Shift, keyPressed_Group1, keyPressed_Group2, keyPressed_Group3, keyPressed_Group4, keyPressed_Colony;  
        private KeyBindingDef kbDef_Group1, kbDef_Group2, kbDef_Group3, kbDef_Group4,kbDef_Colony; 
        public List<Pawn> PawnsKey_Group1, PawnsKey_Group2, PawnsKey_Group3, PawnsKey_Group4;
        public List<IntVec3> CellsKey_Group1, CellsKey_Group2, CellsKey_Group3, CellsKey_Group4;

        // Release all drafted pawns without active job
        private bool keyPressed_Release;
        private KeyBindingDef kbDef_Release;

        // SHIFT+K - Open the help window
        private bool keyPressed_Help;
        private KeyBindingDef kbDef_Help;

        private bool groupSelectionModeActive = false;

        private int showHelpTextCounter;
        private bool wasHelpTextShown = false;
        private const int showHelpAfterStartCounterMax = 200000; // show after 3 days and a few hours
        //private const int showHelpAfterStartCounterMax = 2000; // DEBUG !!!

        private bool inputAllowed;
        private bool inputAllowedOld;


        public MapComponent_ColonistSelections(Map map) : base(map)
        {

            kbDef_Help = DefDatabase<KeyBindingDef>.GetNamed("ColonistSelectionKeys_Help");
            kbDef_Release = DefDatabase<KeyBindingDef>.GetNamed("ColonistSelectionKeys_Release");

            kbDef_Group1 = DefDatabase<KeyBindingDef>.GetNamed("ColonistSelectionKeys_Group1");
            kbDef_Group2 = DefDatabase<KeyBindingDef>.GetNamed("ColonistSelectionKeys_Group2");
            kbDef_Group3 = DefDatabase<KeyBindingDef>.GetNamed("ColonistSelectionKeys_Group3");
            kbDef_Group4 = DefDatabase<KeyBindingDef>.GetNamed("ColonistSelectionKeys_Group4");
            
            kbDef_Colony = DefDatabase<KeyBindingDef>.GetNamed("ColonistSelectionKeys_CenterOnColony");

            PawnsKey_Group1 = new List<Pawn>();
            CellsKey_Group1 = new List<IntVec3>();
            PawnsKey_Group2 = new List<Pawn>();
            CellsKey_Group2 = new List<IntVec3>();
            PawnsKey_Group3 = new List<Pawn>();
            CellsKey_Group3 = new List<IntVec3>();
            PawnsKey_Group4 = new List<Pawn>();
            CellsKey_Group4 = new List<IntVec3>();

            //if (kbDef_Eat == null || kbDef_Sleep == null || kbDef_Help == null || kbDef_Release == null || kbDef_Group1 == null || kbDef_Group2 == null || kbDef_Group3 == null || kbDef_Group4 == null)
            //    Log.Error("Somethings null!!!");
            //else
            //    Log.Error("KeyBinding done.");
        }


        #region load / save

        /// <summary>
        /// load/save data lists
        /// </summary>
        public override void ExposeData()
        {
            Scribe_Values.Look<bool>(ref wasHelpTextShown, "wasHelpTextShown", false);

            Scribe_Values.Look<Boolean>(ref groupSelectionModeActive, "GroupSelectionModeActive", false);

            try
            {
                Scribe_Collections.Look<IntVec3>(ref CellsKey_Group1, "CellsGroup1", LookMode.Value);
                Scribe_Collections.Look<Pawn>(ref PawnsKey_Group1, "PawnsGroup1", LookMode.Reference);
            }
            catch (Exception ex)
            {
                string message = ex.Message + Environment.NewLine + ex.StackTrace;
                Log.Error("Error Group1:" + message);
                CellsKey_Group1 = new List<IntVec3>();
                PawnsKey_Group1 = new List<Pawn>();
            };


            try
            {
                Scribe_Collections.Look<IntVec3>(ref CellsKey_Group2, "CellsGroup2", LookMode.Value);
                Scribe_Collections.Look<Pawn>(ref PawnsKey_Group2, "PawnsGroup2", LookMode.Reference);
            }
            catch (Exception ex)
            {
                string message = ex.Message + Environment.NewLine + ex.StackTrace;
                Log.Error("Error Group2:" + message);
                CellsKey_Group2 = new List<IntVec3>();
                PawnsKey_Group2 = new List<Pawn>();
            };


            try
            {
                Scribe_Collections.Look<IntVec3>(ref CellsKey_Group3, "CellsGroup3", LookMode.Value);
                Scribe_Collections.Look<Pawn>(ref PawnsKey_Group3, "PawnsGroup3", LookMode.Reference);
            }
            catch (Exception ex)
            {
                string message = ex.Message + Environment.NewLine + ex.StackTrace;
                Log.Error("Error Group3:" + message);
                CellsKey_Group3 = new List<IntVec3>();
                PawnsKey_Group3 = new List<Pawn>();
            };


            try
            {
            Scribe_Collections.Look<IntVec3>(ref CellsKey_Group4, "CellsGroup4", LookMode.Value);
            Scribe_Collections.Look<Pawn>(ref PawnsKey_Group4, "PawnsGroup4", LookMode.Reference);
            }
            catch (Exception ex)
            {
                string message = ex.Message + Environment.NewLine + ex.StackTrace;
                Log.Error("Error Group4:" + message);
                CellsKey_Group4 = new List<IntVec3>();
                PawnsKey_Group4 = new List<Pawn>();
            };

        }

        #endregion



        public override void MapComponentTick()
        {
            DoStartupScreen();
        }

        public override void MapComponentUpdate()
        //public override void MapComponentOnGUI()
        {
            inputAllowed = Find.WindowStack.CurrentWindowGetsInput;

            // Test
            if (inputAllowed != inputAllowedOld)
            {

                //if (inputAllowed)
                //    Log.Error("Input changed: Allowed");
                //else
                //    Log.Error("Input changed: Not allowed");

                inputAllowedOld = inputAllowed;
            }

            if (Input.anyKeyDown)
            {

                keyPressed_Group1 = kbDef_Group1.IsDown; 
                keyPressed_Group2 = kbDef_Group2.IsDown; 
                keyPressed_Group3 = kbDef_Group3.IsDown; 
                keyPressed_Group4 = kbDef_Group4.IsDown; 

                keyPressed_Release = kbDef_Release.IsDown;

                //keyPressed_keyQ = kbDef_keyQ.IsDown;
                //keyPressed_keyE = kbDef_keyE.IsDown;

                keyPressed_Colony = kbDef_Colony.IsDown;

                keyPressed_Help = kbDef_Help.IsDown;

                keyPressed_Shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            }
            else
            {
                if (inputAllowed)
                {
                    if (keyPressed_Group1)
                        DoGroupPositioning(1, keyPressed_Shift, !groupSelectionModeActive);

                    else if (keyPressed_Group2)
                        DoGroupPositioning(2, keyPressed_Shift, !groupSelectionModeActive);

                    else if (keyPressed_Group3)
                        DoGroupPositioning(3, keyPressed_Shift, !groupSelectionModeActive);

                    else if (keyPressed_Group4)
                        DoGroupPositioning(4, keyPressed_Shift, !groupSelectionModeActive);

                    if (keyPressed_Release && !keyPressed_Shift)
                        DoReleaseAllColonists();

                    if (keyPressed_Release && keyPressed_Shift)
                        DoSwitchPositioningSelectionMode();

                    if (keyPressed_Help && keyPressed_Shift)
                        DoStartupScreen(true);

                    if (keyPressed_Colony)
                        DoCenterOnColony(this.map);
                }

                keyPressed_Group1 = false;
                keyPressed_Group2 = false;
                keyPressed_Group3 = false;
                keyPressed_Group4 = false;

                keyPressed_Release = false;

                keyPressed_Colony = false;

                keyPressed_Help = false;

                keyPressed_Shift = false;
            }
        }


        /// <summary>
        /// Show a help screen 10s after start
        /// </summary>
        /// <param name="forceShow"></param>
        private void DoStartupScreen( bool forceShow = false )
        {

            if (wasHelpTextShown && !forceShow)
                return;

            if (showHelpTextCounter < showHelpAfterStartCounterMax && !forceShow)
            {
                showHelpTextCounter++;
                return;
            }

            // First info message => Letter
            if (!wasHelpTextShown && !forceShow)
            {
                DoStartUpInfoMessage();
                wasHelpTextShown = true;
                return;
            }

            if (!wasHelpTextShown || forceShow)
            {
                StringBuilder dialogText = new StringBuilder();
                dialogText.Append(translationHelp_prt1.Translate());
                dialogText.AppendLine().AppendLine();
                dialogText.Append(translationHelp_prt2.Translate());
                dialogText.AppendLine().AppendLine();
                dialogText.Append(translationHelp_prt3.Translate());
                dialogText.AppendLine().AppendLine();
                dialogText.Append(translationHelp_prt4.Translate());
                
                Find.MusicManagerPlay.disabled = true;
                DiaNode diaNode = new DiaNode(dialogText.ToString());
                DiaOption diaOption = new DiaOption()
                {
                    resolveTree = true,
                    //action = () =>
                    //{
                    //    //Find.TickManager.curTimeSpeed = TimeSpeed.Paused;
                    //    //Find.MusicManagerMap.ForceSilenceFor(7f);
                    //    //Find.MusicManagerMap.disabled = false;
                    //},
                    clickSound = null,
                    
                };
                diaNode.options.Add(diaOption);
                Dialog_NodeTree dialogNodeTree = new Dialog_NodeTree(diaNode)
                {
                    //forcePause = true
                    //soundClose = SoundDef.Named("GameStartSting")
                };
                Find.WindowStack.Add(dialogNodeTree);

                Find.MusicManagerPlay.disabled = false;

                wasHelpTextShown = true;
            }
        }
        private void DoStartUpInfoMessage()
        {
            //Find.LetterStack.ReceiveLetter(translationLetterInfoOpenHelp.Translate(), translationMessageInfoOpenHelp.Translate(), LetterType.Good, null);

            //string msg = translationMessageInfoOpenHelp.Translate() + translationHelp_prt2.Translate() + "\n" + translationHelp_prt3.Translate();

            string msg = translationMessageInfoOpenHelp.Translate() + "\n\n\n...  ...  ...\n\n" + translationHelp_prt2.Translate() + "\n\n...  ...  ...";

            Find.LetterStack.ReceiveLetter(translationLetterInfoOpenHelp.Translate(), msg, LetterDefOf.PositiveEvent, null );
        }

        /// <summary>
        /// Switch the positioning
        /// </summary>
        private void DoSwitchPositioningSelectionMode()
        {
            groupSelectionModeActive = !groupSelectionModeActive;

            if (!groupSelectionModeActive)
                Messages.Message(translationGroupPositioningActive.Translate(new object[] { }), MessageTypeDefOf.CautionInput);
            else
                Messages.Message(translationGroupSelectionActive.Translate(new object[] { }), MessageTypeDefOf.CautionInput);

        }



        /// <summary>
        /// Work through group positionings
        /// </summary>
        /// <param name="key"></param>
        /// <param name="shiftPressed"></param>
        private void DoGroupPositioning(int key, bool shiftPressed, bool positionModeActive)
        {
            List<Pawn> pawns;
            List<IntVec3> cells;

            // Save marked pawns
            if (shiftPressed)
            {
                //Find drafted pawns for position saving
                List<Pawn> FreeColonists = map.mapPawns.FreeColonistsSpawned.ToList<Pawn>();
                List<Pawn> selection = new List<Pawn>();

                // Are pawns selected?
                bool pawnsSelected =
                    Find.Selector.SelectedObjectsListForReading != null &&
                    Find.Selector.SelectedObjectsListForReading.Count != 0 &&
                    Find.Selector.SelectedObjectsListForReading.FindIndex(obj => obj is Pawn) >= 0;

                for (int i = 0; i < FreeColonists.Count; i++)
                {
                    Pawn checkPawn = FreeColonists[i];
                    if (checkPawn == null || checkPawn.drafter == null || checkPawn.drafter.Drafted != true)
                        continue;

                    if (pawnsSelected)
                    {
                        if (Find.Selector.IsSelected(checkPawn))
                            selection.Add(checkPawn);
                    }
                    else
                    {
                        selection.Add(checkPawn);
                    }
                }
            

                pawns = new List<Pawn>();
                cells = new List<IntVec3>();

                for (int i = 0; i < selection.Count(); i++)
                {
                    Pawn p = selection.ElementAt(i) as Pawn;
                    if (p != null)
                    {
                        pawns.Add(p);
                        cells.Add(p.Position);
                    }
                }

                // No valid pawns found
                if (pawns.Count == 0)
                {
                    if (positionModeActive)
                        Messages.Message(translationGroupInvalid_PositionMode.Translate(new object[] { key.ToString() }), MessageTypeDefOf.RejectInput);
                    else
                        Messages.Message(translationGroupInvalid_SelectionMode.Translate(new object[] { (key).ToString() }), MessageTypeDefOf.RejectInput);

                    pawns = new List<Pawn>(); //null;
                    cells = new List<IntVec3>(); //null;
                    return;
                }

                // Save pawns and cells
                switch (key)
                {
                    case 1:
                        PawnsKey_Group1 = pawns;
                        CellsKey_Group1 = cells;
                        break;
                    case 2:
                        PawnsKey_Group2 = pawns;
                        CellsKey_Group2 = cells;
                        break;
                    case 3:
                        PawnsKey_Group3 = pawns;
                        CellsKey_Group3 = cells;
                        break;
                    case 4:
                        PawnsKey_Group4 = pawns;
                        CellsKey_Group4 = cells;
                        break;

                    default:
                        Log.Error("Invalid key reached. This shouldn't happen!");
                        return;
                }

                Messages.Message(tanslationGroupSaved.Translate(new object[] { key.ToString(), pawns.Count.ToString() }), MessageTypeDefOf.CautionInput);

                return;
            }
            else
            {

                // select saved pawns
                pawns = null;
                cells = null;
                KeyBindingDef kbd;

                switch (key)
                {
                    case 1:
                        pawns = PawnsKey_Group1;
                        cells = CellsKey_Group1;
                        kbd = kbDef_Group1;
                        break;

                    case 2:
                        pawns = PawnsKey_Group2;
                        cells = CellsKey_Group2;
                        kbd = kbDef_Group2;
                        break;

                    case 3:
                        pawns = PawnsKey_Group3;
                        cells = CellsKey_Group3;
                        kbd = kbDef_Group3;
                        break;

                    case 4:
                        pawns = PawnsKey_Group4;
                        cells = CellsKey_Group4;
                        kbd = kbDef_Group4;
                        break;

                    default:
                        Log.Error("Invalid key reached. This shouldn't happen!");
                        return;
                }

                if (pawns == null || pawns.Count == 0 || cells == null || cells.Count == 0)
                {
                    if (positionModeActive)
                        Messages.Message(tanslationGroupNotSet_PositionMode.Translate(new object[] { key.ToString(), kbd.MainKey.ToString() }), MessageTypeDefOf.RejectInput);
                    else
                        Messages.Message(tanslationGroupNotSet_SelectionMode.Translate(new object[] { key.ToString(), kbd.MainKey.ToString() }), MessageTypeDefOf.RejectInput);

                    return;
                }

                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn p = pawns[i];

                    if (!IsValidPawnForDrafting(p))
                        continue;

                    IntVec3 c = cells[i];

                    p.drafter.Drafted = true;

                    Find.Selector.Select(p, false);

                    if (positionModeActive)
                    {
                        //Job job = new Job(DefDatabase<JobDef>.GetNamed(JobDefName_GotoDraft), Gen.MouseWorldCell());
                        //Job job = new Job(DefDatabase<JobDef>.GetNamed(JobDefName_GotoDraft), c);
                        Job job = new Job(JobDefOf.Goto, c);
                        p.jobs.TryTakeOrderedJob(job);
                    }
                }

                if (positionModeActive)
                    Messages.Message(tanslationGroupPositioned.Translate(new object[] { key.ToString() }), MessageTypeDefOf.CautionInput);
                else
                    Messages.Message(tanslationGroupSelected.Translate(new object[] { key.ToString() }), MessageTypeDefOf.CautionInput);
            }
        }


        /// <summary>
        /// Check if the pawn is valid to be drafted
        /// </summary>
        private bool IsValidPawnForDrafting(Pawn pawnToCheck)
        {
            // Check if pawn is valid
            if (pawnToCheck == null || pawnToCheck.Destroyed || pawnToCheck.Dead)
                return false;

            // Check if pawn is available
            if (pawnToCheck.Downed || pawnToCheck.IsBurning() || pawnToCheck.Faction != Faction.OfPlayer)
                return false;

            // Check if pawn is in a broken state or prisoner 
            if (pawnToCheck.mindState.mentalStateHandler.InMentalState || pawnToCheck.IsPrisonerOfColony)
                return false;

            //Log.Error(pawnToCheck.Name + " is valid");

            return true;
        }


        /// <summary>
        /// Release all Colonists without an active Job
        /// </summary>
        private void DoReleaseAllColonists()
        {
            IEnumerable<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < colonists.Count(); i++)
            {
                Pawn p = colonists.ElementAt(i);

                // Don't release pawns with an active job
                if (p.CurJob == null || (p.CurJob.def != JobDefOf.Wait && p.CurJob.def != JobDefOf.Wait_Combat))
                    continue;

                if (p.drafter.Drafted == true)
                    p.drafter.Drafted = false;
            }

            Messages.Message(tanslationPawnsUndrafted.Translate(), MessageTypeDefOf.CautionInput);
        }


        /// <summary>
        /// Center camera to colony
        /// </summary>
        private void DoCenterOnColony(Map map)
        {
            IntVec3 cell = IntVec3.Invalid;
            if (map.areaManager.Home.ActiveCells.Count() > 0)
                cell = map.areaManager.Home.ActiveCells.RandomElement();

            // Only jump if valid cell was found
            if (cell != IntVec3.Invalid && cell.IsValid)
                Find.CameraDriver.JumpToCurrentMapLoc(cell);
        }

        private Pawn TryGetSelectedSinglePawn()
        {
            IEnumerable<Pawn> myPawns = TryGetSelectedPawns();
            if (myPawns.Count() == 1)
                return myPawns.First();

            return null;
        }
        private IEnumerable<Pawn> TryGetSelectedPawns()
        {
            Pawn myPawn = null;

            foreach (object obj in Find.Selector.SelectedObjects)
            {
                myPawn = obj as Pawn;
                if (myPawn == null || myPawn.Destroyed || myPawn.Dead || myPawn.Downed || myPawn.IsBurning() || myPawn.Faction != Faction.OfPlayer)
                    continue;

                yield return myPawn;
            }
        }



        // From: RCellFinder(..)
        private IntVec3 BestOrderedGotoDestNear(IntVec3 root, Pawn searcher)
        {
            return RCellFinder.BestOrderedGotoDestNear(root, searcher);
        }



        /// <summary>
        /// This function provides the info, if this component is available or not
        /// </summary>
        /// <param name="mc"></param>
        /// <returns></returns>
        public static bool IsMapComponentAvailable(Map map, out MapComponent_ColonistSelections mc)
        {
            mc = null;

            for (int i = 0; i < map.components.Count; i++)
            {
                mc = map.components[i] as MapComponent_ColonistSelections;
                if (mc != null)
                    break;
            }

            if (mc == null)
                return false;

            return true;
        }

        /// <summary>
        /// This function tries to add the mapcomponent if it isn't already available
        /// </summary>
        /// <returns></returns>
        public static bool TryAddMapComponent(Map map)
        {
            MapComponent_ColonistSelections mc;

            if (IsMapComponentAvailable(map, out mc))
                return true;

            mc = new MapComponent_ColonistSelections(map);
            map.components.Add(mc);

            return IsMapComponentAvailable(map, out mc);
        }








    }

}
