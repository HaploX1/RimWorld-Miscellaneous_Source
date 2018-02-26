using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace AIRobot
{
    public class X2_PawnColumnWorker_Charge : PawnColumnWorker_Label
    {
        // Base is from RimWorld.PawnColumnWorker_Label

        private const int LeftMargin = 3;

        private static Dictionary<string, string> labelCache = new Dictionary<string, string>();

        private static float labelCacheForWidth = -1f;

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            Rect rect2 = new Rect(rect.x, rect.y, rect.width, Mathf.Min(rect.height, 30f));
            //if (pawn.health.summaryHealth.SummaryHealthPercent < 0.99f)
            //{
            //    Rect rect3 = new Rect(rect2);
            //    rect3.xMin -= 4f;
            //    rect3.yMin += 4f;
            //    rect3.yMax -= 6f;
            //    Widgets.FillableBar(rect3, pawn.health.summaryHealth.SummaryHealthPercent, GenMapUI.OverlayHealthTex, BaseContent.ClearTex, false);
            //}
            if (Mouse.IsOver(rect2))
            {
                GUI.DrawTexture(rect2, TexUI.HighlightTex);
            }
            string str;
            if ( pawn.needs != null && pawn.needs.rest != null )
            {
                str = pawn.needs.rest.CurLevelPercentage.ToStringPercent();
            }
            else
            {
                str = "????";
            }
            Rect rect4 = rect2;
            rect4.xMin += 3f;
            if (rect4.width != labelCacheForWidth)
            {
                labelCacheForWidth = rect4.width;
                labelCache.Clear();
            }
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.WordWrap = false;
            Widgets.Label(rect4, str.Truncate(rect4.width, labelCache));
            Text.WordWrap = true;
            Text.Anchor = TextAnchor.MiddleCenter;
            if (Widgets.ButtonInvisible(rect2, false))
            {
                X2_AIRobot robot = pawn as X2_AIRobot;
                if (robot != null && robot.needs != null && robot.needs.rest != null && robot.rechargeStation != null)
                {
                    // Recall robot and shut down -> Does not respawn after recharge!
                    //robot.rechargeStation.Notify_CallBotForShutdown();

                    // Recall robot for recharge only -> Respawned!
                    robot.rechargeStation.Notify_CallBotForRecharge();

                    //CameraJumper.TryJumpAndSelect(robot.rechargeStation);
                    CameraJumper.TryJumpAndSelect(pawn);
                    //if (Current.ProgramState == ProgramState.Playing && Event.current.button == 0)
                    //{
                    //    Find.MainTabsRoot.EscapeCurrentTab(false);
                    //}
                }
                return;
            }
            TipSignal tooltip = pawn.Label;
            tooltip.text = "AIRobot_MainTab_SendRobotToRecharge".Translate() + "\n" + tooltip.text;
            TooltipHandler.TipRegion(rect2, tooltip);
        }

        public override int GetMinWidth(PawnTable table)
        {
            return Mathf.Max(base.GetMinWidth(table), 40);
        }

        public override int GetOptimalWidth(PawnTable table)
        {
            return Mathf.Clamp(75, this.GetMinWidth(table), this.GetMaxWidth(table));
        }

    }
}
