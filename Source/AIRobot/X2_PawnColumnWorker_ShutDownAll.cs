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
    public class X2_PawnColumnWorker_ShutDownAll : PawnColumnWorker_Label
    {
        public static readonly Texture2D texShutDown = ContentFinder<Texture2D>.Get("UI/Buttons/Robots/ShutDownAll", true);


        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if (Widgets.ButtonImage(rect, texShutDown))
            {
                List<X2_Building_AIRobotRechargeStation> buildings = pawn.Map.listerThings.AllThings.OfType<X2_Building_AIRobotRechargeStation>().ToList();
                for (int i = buildings.Count; i > 0; i--)
                {
                    X2_Building_AIRobotRechargeStation building = buildings[i - 1];
                    building.Notify_CallBotForShutdown();
                }
            }
            if (Mouse.IsOver(rect))
            {
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }
            TipSignal tooltip = pawn.Label;
            tooltip.text = "AIRobot_ShutDownAllRobots".Translate();
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
