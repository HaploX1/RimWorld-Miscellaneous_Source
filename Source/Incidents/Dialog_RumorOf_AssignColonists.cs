using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;


namespace Incidents
{
    public class Dialog_RumorOf_AssignColonists : Window
    {

        private const float EntryHeight = 35f;
        private List<Pawn> pawnsToSend = new List<Pawn>(); 

        private Vector2 scrollPosition;

        private MapComponent_ColonistsOutsideMap_RumorOf mc;

        public Dialog_RumorOf_AssignColonists()
        {
            //base.SetCentered(620f, 500f);
            //this.drawPriority = 2000;
            this.closeOnEscapeKey = false;
            this.doCloseX = true;
            this.doCloseButton = false;
            this.forcePause = true;
            this.absorbInputAroundWindow = false;
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(620f, 500f);
            }
        }

        private static string JobDefName_LeaveMap = "LeaveMapForRumorOf";


        public override void DoWindowContents(Rect inRect)
        {
            if (!MapComponent_ColonistsOutsideMap_RumorOf.IsMapComponentAvailable(out mc))
            {
                base.Close();
                return;
            }

            List<Pawn> validPawns = new List<Pawn>(Find.MapPawns.FreeColonistsSpawned.Where<Pawn>(p => p != null && !p.Destroyed && !p.Downed));
            if (validPawns == null || validPawns.Count == 0)
            {
                base.Close();
                return;
            }

            Text.Font = GameFont.Small;

            Rect rectData = new Rect(inRect)
            {
                yMin = inRect.yMin + 50f,
                yMax = inRect.yMax - 75f
            };

            float single = 32f;

            Rect rectHeader = new Rect(0f, 0f, inRect.width * 0.8f, 60f);
            Widgets.Label(rectHeader, mc.def.SelectPawnsMessageVariable.Translate(new object[] { Mathf.CeilToInt(mc.TraveltimeInDays).ToString() }));


            Rect rectScroll = new Rect(0f, single, inRect.width - 16f, (float)validPawns.Count * 35f + 100f);

            // Start Scrollview
            Widgets.BeginScrollView(rectData, ref this.scrollPosition, rectScroll);

            for ( int i = 0; i < validPawns.Count; i++ )
            {
                Pawn pawn = validPawns[i];
                Rect rect2 = new Rect(0f, single, rectScroll.width * 0.6f, 32f);

                // Make label per colonist
                Widgets.Label(rect2, pawn.LabelCap);
                rect2.x = rect2.xMax;
                rect2.width = rectScroll.width * 0.4f;

                if (!pawnsToSend.Contains(pawn))
                {
                    // Make button per colonist
                    if (!Widgets.ButtonText(rect2, mc.def.SelectPawnsMessage_ButtonAssignVariable.Translate(), true, false))
                    {
                        // Normal operation
                        single = single + 35f;
                    }
                    else
                    {
                        // Button was pressed
                        if (pawnsToSend.Count + 1 >= validPawns.Count)
                            // Don't add, throw message
                            Messages.Message(mc.def.SelectPawnsMessage_ButtonAssignErrorVariable.Translate(), MessageSound.RejectInput);
                        else
                            // Add to list
                            pawnsToSend.Add(pawn);
                    }
                }
                else
                {
                    if (!Widgets.ButtonText(rect2, mc.def.SelectPawnsMessage_ButtonUnassignVariable.Translate(), true, false))
                    {
                        // Normal operation
                        single = single + 35f;
                    }
                    else
                    {
                        // Button was pressed
                        pawnsToSend.Remove(pawn);
                    }
                }
            }

            Widgets.EndScrollView();


            mc.DialogDelayActive = true;
            mc.DialogDelayTicks = GenDate.TicksPerHour * 2;

            // Close button
            Vector2 butAbortSize = new Vector2(140f, 40f); // 60px smaller than the send button
            Text.Font = GameFont.Small;
            Rect rectAbortBut = new Rect((inRect.width / 3f) - (inRect.width / 6f) - (butAbortSize.x / 2f) - 5f, inRect.height - 55f, butAbortSize.x, butAbortSize.y);
            if (Widgets.ButtonText(rectAbortBut, mc.def.SelectPawnsMessage_ButtonAbortVariable.Translate(), true, false))
            {
                mc.DialogDelayActive = false;
                mc.DialogDelayTicks = -1;
                //mc.def = null;

                Close(true);
                Event.current.Use();
            }

            // Prepare button
            Vector2 butPrepareSize = new Vector2(140f, 40f); // 60px smaller than the send button
            Text.Font = GameFont.Small;
            Rect rectPrepareBut = new Rect((inRect.width / 3) + (inRect.width / 6f) - (butPrepareSize.x / 2f) - 25f, inRect.height - 55f, butPrepareSize.x, butPrepareSize.y);
            if (Widgets.ButtonText(rectPrepareBut, mc.def.SelectPawnsMessage_ButtonPostponeVariable.Translate(), true, false))
            {
                mc.DialogDelayActive = true;
                mc.DialogDelayTicks = -1;

                Close(true);
                Event.current.Use();
            }

            // SendColonists button
            Vector2 butSendSize = new Vector2(200f, 40f);
            Rect rectSendBut = new Rect((inRect.width / 3) * 2 + (inRect.width / 6f) - (butSendSize.x / 2f) - 15f, inRect.height - 55f, butSendSize.x, butSendSize.y);
            if (Widgets.ButtonText(rectSendBut, mc.def.SelectPawnsMessage_ButtonSendVariable.Translate(), true, false))
            {
                if (pawnsToSend.Count == 0)
                {
                    // Don't add, throw message
                    Messages.Message(mc.def.SelectPawnsMessage_ButtonSendErrorVariable.Translate(), MessageSound.RejectInput);
                }
                else
                {
                    mc.DialogDelayActive = false;
                    mc.DialogDelayTicks = -1;

                    SendSelectedTeam();
                    Close(true);
                    Event.current.Use();
                }
            }
        }


        private void SendSelectedTeam()
        {
            IntVec3 exitMapCell = mc.ExitMapCell;

            mc.Active = true;

            for (int i = 0; i < pawnsToSend.Count; i++)
            {
                Pawn pawn = pawnsToSend[i];

                // Check if pawn is valid
                if (pawn == null || pawn.Destroyed || pawn.Dead || !pawn.Spawned || pawn.health.Downed)
                    continue;

                // Release pawn
                pawn.drafter.Drafted = false;

                // Delete allowed area for this pawn
                pawn.playerSettings.AreaRestriction = null;

                // Give Pawn the job to exit map and go to the anomaly
                Job job = new Job(DefDatabase<JobDef>.GetNamed(JobDefName_LeaveMap), exitMapCell);

                if (job == null)
                    return;

                pawn.jobs.StopAll();
                pawn.jobs.StartJob(job);
            }

            pawnsToSend.Clear();
        }

        public static void CreateColonistSelectionDialog()
        {
            // Create colonists selection dialog
            Dialog_RumorOf_AssignColonists dialog = new Dialog_RumorOf_AssignColonists();
            dialog.forcePause = true;
            dialog.onlyOneOfTypeAllowed = true;
            Find.WindowStack.Add(dialog);
        }

    }
}
