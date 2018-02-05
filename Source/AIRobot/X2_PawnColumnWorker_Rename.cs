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
    public class X2_PawnColumnWorker_Rename : PawnColumnWorker_Label
    {
        public static readonly Texture2D texRename = ContentFinder<Texture2D>.Get("UI/Buttons/Rename", true);
        public static string nameFirst = "AIRobot_Basename_first";
        //public static string nameNick = "AIRobot_Basename_nick";
        //public static string nameLast = "AIRobot_Basename_last";

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if (Widgets.ButtonImage(rect, texRename))
            {

                if (pawn.Name == null)
                {
                    NameTriple name = new NameTriple(nameFirst.Translate(), pawn.Label, pawn.Label);
                    pawn.Name = name;
                }

                //Log.Error(pawn == null ? "pawn==null" : pawn.Label);
                //Log.Error(pawn.Name == null ? "pawn.Name==null" : pawn.Name as NameTriple == null ? "pawn.Name as NameTriple==null" : (pawn.Name as NameTriple).Nick);

                //Find.WindowStack.Add( new Dialog_ChangeNameTriple(pawn) );
                Find.WindowStack.Add(new X2_Dialog_ChangeNameTriple_Robots(pawn));
            }
            if (Mouse.IsOver(rect))
            {
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }
            TipSignal tooltip = pawn.Label;
            tooltip.text = "AIRobot_RenameRobot".Translate();
            TooltipHandler.TipRegion(rect, tooltip);
        }

        public override int GetMinWidth(PawnTable table)
        {
            return 30;
        }

        public override int GetOptimalWidth(PawnTable table)
        {
            return Mathf.Clamp(30, this.GetMinWidth(table), this.GetMaxWidth(table));
        }

        public override int GetMaxWidth(PawnTable table)
        {
            return Mathf.Min(base.GetMaxWidth(table), this.GetMinWidth(table));
        }

    }
}
