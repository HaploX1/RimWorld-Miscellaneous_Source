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

namespace Patches_Misc_CE
{
    public class WeaponBase_CE_Building_TurretWeaponBase : TurretWeaponBase.Building_TurretWeaponBase
    {

        protected override void BeginBurst()
        {
            if (this._sourceVerb == null)
                this._sourceVerb = GunCompEq.PrimaryVerb;

            //GunCompEq.PrimaryVerb.TryStartCastOn(CurrentTarget, false, true);
            this.TryStartCastOn(Verb, CurrentTarget, false, true);
            base.OnAttackedTarget(this.CurrentTarget);
        }

        public bool TryStartCastOn(Verb verb, LocalTargetInfo castTarg, bool surpriseAttack = false, bool canHitNonTargetPawns = true)
        {
            if (verb.caster == null)
            {
                Log.Error("Verb " + this.GetUniqueLoadID() + " needs caster to work (possibly lost during saving/loading).", false);
                return false;
            }
            if (!verb.caster.Spawned)
            {
                return false;
            }
            if (verb.state == VerbState.Bursting || !verb.CanHitTarget(castTarg))
            {
                return false;
            }

            Patches_Misc_CE.Reflect<Boolean>.SetValue(verb, "surpriseAttack", surpriseAttack);
            Patches_Misc_CE.Reflect<Boolean>.SetValue(verb, "canHitNonTargetPawnsNow", canHitNonTargetPawns);
            Patches_Misc_CE.Reflect<LocalTargetInfo>.SetValue(verb, "currentTarget", castTarg);

            //verb.surpriseAttack = surpriseAttack;
            //verb.canHitNonTargetPawnsNow = canHitNonTargetPawns;
            //verb.currentTarget = castTarg;

            if (verb.CasterIsPawn && verb.verbProps.warmupTime > 0f)
            {
                ShootLine newShootLine;
                if (!verb.TryFindShootLineFromTo(verb.caster.Position, castTarg, out newShootLine))
                {
                    return false;
                }
                verb.CasterPawn.Drawer.Notify_WarmingCastAlongLine(newShootLine, verb.caster.Position);
                float statValue = verb.CasterPawn.GetStatValue(StatDefOf.AimingDelayFactor, true);
                int ticks = (verb.verbProps.warmupTime * statValue).SecondsToTicks();
                verb.CasterPawn.stances.SetStance(new Stance_Warmup(ticks, castTarg, verb));
            }
            else
            {
                verb.WarmupComplete();
            }
            return true;
        }


        private Verb _sourceVerb;
        private Verb_Shoot _verb;
        public Verb_Shoot Verb
        {
            get
            {
                if (_verb == null && _sourceVerb != null)
                {
                    _verb = new Verb_Shoot();
                    this.InitVerb(this._verb, _sourceVerb.verbProps, _sourceVerb.verbTracker, null, null, null);
                }

                return _verb;
            }
            set
            {
                _verb = value;
            }
        }

        private void InitVerb(Verb verb, VerbProperties properties, VerbTracker verbTracker, Tool tool, ManeuverDef maneuver, string id)
        {
            verb.loadID = id;
            verb.verbProps = properties;
            verb.verbTracker = verbTracker;
            verb.tool = tool;
            verb.maneuver = maneuver;
            verb.caster = this;
        }

    }
}
