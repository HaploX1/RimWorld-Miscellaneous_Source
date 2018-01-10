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
    public class X2_MainTabWindow_Robots : MainTabWindow_PawnTable
    {
        private static PawnTableDef pawnTableDef = null;
        protected override PawnTableDef PawnTableDef
        {
            get
            {
                if (pawnTableDef == null)
                    pawnTableDef = DefDatabase<PawnTableDef>.GetNamed("AIRobots");
                return pawnTableDef;
            }
        }

        protected override IEnumerable<Pawn> Pawns
        {
            get
            {
                return from p in Find.VisibleMap.mapPawns.PawnsInFaction(Faction.OfPlayer)
                              where p is X2_AIRobot
                              orderby p.def.label
                              select p;
            }
        }

        public override void PostOpen()
        {
            base.PostOpen();
            Find.World.renderer.wantedMode = WorldRenderMode.None;
        }
    }
}
