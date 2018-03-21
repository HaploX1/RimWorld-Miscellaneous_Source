using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;
//using RimWorld.Planet;
//using RimWorld.SquadAI;


namespace WeaponBase
{
    public class CompRemainingLifetime : ThingComp
    {
        private int startGameTick = 0;

        public CompProperties_RemainingLifetime Props
        {
            get
            {
                return (CompProperties_RemainingLifetime)this.props;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.startGameTick, "startGameTick", 0, false);
        }

        public override void CompTick()
        {
            base.CompTick();

            if (startGameTick == 0)
                startGameTick = Find.TickManager.TicksGame;
            
            // If time is up, destroy parent
            if (Find.TickManager.TicksGame > startGameTick + GenTicks.SecondsToTicks(this.Props.lifetime))
                parent.Destroy(DestroyMode.Vanish);
        }
        public override string CompInspectStringExtra()
        {
            string s = base.CompInspectStringExtra();
            if (s != null)
                s= s.TrimEndNewlines().TrimEnd();
            else
                s = "";

            if (s != "")
                s = s + "\n";

            s = s + "TimeLeft".Translate() + ": " + GetRemainingSeconds() + "s";
            return s.TrimEndNewlines();
        }

        private int GetRemainingSeconds()
        {
            return (int)GenTicks.TicksToSeconds((startGameTick + GenTicks.SecondsToTicks(this.Props.lifetime)) - Find.TickManager.TicksGame);
        }
    }
}
