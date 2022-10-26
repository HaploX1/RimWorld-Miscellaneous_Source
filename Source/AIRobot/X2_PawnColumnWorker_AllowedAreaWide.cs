using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace AIRobot
{
    [StaticConstructorOnStartup]
    public class X2_PawnColumnWorker_AllowedAreaWide : PawnColumnWorker_AllowedAreaWide
    {
        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if (pawn.Faction == Faction.OfPlayer)
            {
                    AreaAllowedGUI.DoAllowedAreaSelectors(rect, pawn);
            }
        }
    }
}
