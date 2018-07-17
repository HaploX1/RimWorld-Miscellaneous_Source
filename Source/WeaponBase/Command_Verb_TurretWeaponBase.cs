using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace TurretWeaponBase
{
    // Base: Command_VerbTarget because it is internal!
    public class Command_VerbTarget_TurretWeaponBase : Command
    {
        public Verb verb;

        public override Color IconDrawColor
        {
            get
            {
                if (this.verb.ownerEquipment != null)
                {
                    return this.verb.ownerEquipment.DrawColor;
                }
                return base.IconDrawColor;
            }
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            Targeter targeter = Find.Targeter;
            if (this.verb.CasterIsPawn && targeter.targetingVerb != null && targeter.targetingVerb.verbProps == this.verb.verbProps)
            {
                Pawn casterPawn = this.verb.CasterPawn;
                if (!targeter.IsPawnTargeting(casterPawn))
                {
                    targeter.targetingVerbAdditionalPawns.Add(casterPawn);
                }
            }
            else
            {
                Find.Targeter.BeginTargeting(this.verb);
            }
        }
    }
}
