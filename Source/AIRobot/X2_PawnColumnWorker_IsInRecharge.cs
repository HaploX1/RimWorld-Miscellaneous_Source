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
    [StaticConstructorOnStartup]
    public class X2_PawnColumnWorker_IsInRecharge : PawnColumnWorker_Label
    {
        public static readonly Texture2D texRecharging = ContentFinder<Texture2D>.Get("UI/Buttons/Robots/Recharging", true);
        public static readonly Texture2D texNotRecharging = ContentFinder<Texture2D>.Get("UI/Buttons/Robots/NotRecharging", true);


        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            TipSignal tooltip = pawn.Label;

            // normal mode
            if (pawn != null && !pawn.Destroyed && pawn.Spawned)
            {
                if (Widgets.ButtonImage(rect, texNotRecharging))
                {
                    X2_AIRobot robot = pawn as X2_AIRobot;
                    //if (robot != null && robot.rechargeStation != null)
                    //    robot.rechargeStation.Notify_CallBotForShutdown();

                    CameraJumper.TryJumpAndSelect(robot);
                }
            }
            // Is recharging
            else if (pawn != null && !pawn.Destroyed && !pawn.Spawned)
            {
                if (Widgets.ButtonImage(rect, texRecharging))
                {
                    X2_AIRobot robot = pawn as X2_AIRobot;
                    //if (robot != null && robot.rechargeStation != null)
                    //    robot.rechargeStation.Notify_SpawnBot();  <-- ERROR: Creates a NEW robot wich causes ERRORS!!!
                    
                    if (robot != null && robot.rechargeStation != null)
                    {
                        if (Current.ProgramState == ProgramState.Playing && Event.current.button == 0)
                            Find.MainTabsRoot.EscapeCurrentTab(false);

                        CameraJumper.TryJumpAndSelect(robot.rechargeStation);
                    }
                }
                tooltip.text = "AIRobot_RobotIsRecharging".Translate() + "\n" + pawn.Label;
            }
            if (Mouse.IsOver(rect))
            {
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }

            TooltipHandler.TipRegion(rect, tooltip);
        }

        public override int GetMinWidth(PawnTable table)
        {
            return 20;
        }

        public override int GetOptimalWidth(PawnTable table)
        {
            return Mathf.Clamp(20, this.GetMinWidth(table), this.GetMaxWidth(table));
        }

        public override int GetMaxWidth(PawnTable table)
        {
            return Mathf.Min(base.GetMaxWidth(table), this.GetMinWidth(table));
        }

    }
}
