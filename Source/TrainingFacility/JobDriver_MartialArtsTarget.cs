using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using Verse.Sound;


namespace TrainingFacility
{
    public class JobDriver_MartialArtsTarget : JobDriver_WatchBuilding
    {
        private const int UpdateInterval = 250;
        protected bool joyCanEndJob = true;

        public float restReductionForcedPerTick = 0.0015f * GenTicks.SecondsPerTick; // around 6h training possible

        private int nearbyPawnSearchCountdown = 0;
        private bool nearbyPawnFound = false;
        private int nearbyPawnDistance = 5;

        public JobDriver_MartialArtsTarget() {}

        protected override void WatchTickAction()
        {

            if (this.pawn.IsHashIntervalTick(UpdateInterval))
            {
                FightTarget(this.pawn, base.TargetA.Cell);

                nearbyPawnSearchCountdown += 1;
                if (nearbyPawnSearchCountdown >= 3)
                {
                    nearbyPawnSearchCountdown = 0;
                    nearbyPawnFound = false;
                    foreach (Pawn p in this.pawn.MapHeld.mapPawns.AllHumanlikeSpawned)
                    {
                        if (Utility_PositionFinder.IsCellInRadius(this.pawn.PositionHeld, p.PositionHeld, nearbyPawnDistance))
                        {
                            nearbyPawnFound = true;
                            break;
                        }
                    }
                }
            }

            //base.StandTickAction(); // disabled and fully extracted. Changes are needed because of the second usage by FloatMenu -> NonJoy

            if (pawn.rotationTracker != null)
                this.pawn.rotationTracker.FaceCell(base.TargetA.Cell);

            this.pawn.GainComfortFromCellIfPossible();
            
            //JoyUtility.JoyTickCheckEnd(this.pawn, false, 1f); // changed; => needs to be disabled when not joy activity or it will end the job!

            Job curJob = pawn.CurJob;

            // added joyCanEndJob her, so that only non-forced jobs can get joy from it
            if (joyCanEndJob && pawn.needs != null && pawn.needs.joy != null && pawn.needs.joy.CurLevel <= 0.9999f) // changed, else it would throw an error if joy is full: joyKind NullRef ???
            {
                pawn.needs.joy.GainJoy(1f * curJob.def.joyGainRate * 0.000144f, curJob.def.joyKind);
            }
            if (curJob.def != null && curJob.def.joySkill != null && curJob.def.joyXpPerTick != 0 && pawn.skills != null && pawn.skills.GetSkill(curJob.def.joySkill) != null)
            {
                if (!nearbyPawnFound)
                {
                    if (pawn.skills.GetSkill(curJob.def.joySkill).GetLevel() <= Utility_MaxAllowedTrainingLevel.GetMaxAllowedTrainingLevel(pawn))
                    {
                        // normal leveling
                        pawn.skills.GetSkill(curJob.def.joySkill).Learn(joyCanEndJob ? curJob.def.joyXpPerTick * 1.1f : curJob.def.joyXpPerTick * 0.8f);
                    }
                    else
                    {
                        // this pawn level is > max -> REDUCE skill
                        pawn.skills.GetSkill(curJob.def.joySkill).Learn(-(curJob.def.joyXpPerTick / 2f));
                    }
                }
                if (nearbyPawnFound)
                {
                    if (pawn.skills.GetSkill(curJob.def.joySkill).GetLevel() <= Utility_MaxAllowedTrainingLevel.GetMaxAllowedTrainingLevel(pawn))
                    {
                        // 2nd pawn is nearby > increase skill gain
                        pawn.skills.GetSkill(curJob.def.joySkill).Learn(joyCanEndJob ? curJob.def.joyXpPerTick * 2.5f : curJob.def.joyXpPerTick * 1.8f);
                    }
                }
            }

            // If forced, this job will reduce the rest of the pawn
            if (!joyCanEndJob)
            {
                if (pawn?.needs?.rest != null)
                {
                    pawn.needs.rest.CurLevel -= restReductionForcedPerTick;
                }
            }

            if (Utility_Tired.IsTooTired(pawn) || Utility_Hungry.IsTooHungry(pawn))
                pawn.jobs.curDriver.EndJobWith(JobCondition.InterruptForced);

            if (joyCanEndJob)
            {
                if (pawn.GetTimeAssignment() != null && !pawn.GetTimeAssignment().allowJoy) // changed => disable TimeAssignment
                {
                    pawn.jobs.curDriver.EndJobWith(JobCondition.InterruptForced);
                }
                if (pawn.needs != null && pawn.needs.joy == null || pawn.needs.joy.CurLevel > 0.9999f) // changed => disable Max Joy
                {
                    pawn.jobs.curDriver.EndJobWith(JobCondition.Succeeded);
                }
            }
        }
        

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //TargetA is the building
            //TargetB is the place to stand to watch

            this.EndOnDespawnedOrNull(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.A);
            Utility_Tired.EndOnTired(this);

            yield return Toils_Reserve.Reserve(TargetIndex.A, 1);

            yield return Toils_Reserve.Reserve(TargetIndex.B);
            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
            yield return GetToil_MeleeFightTarget(this.pawn, TargetA);
            
        }

        private Toil GetToil_MeleeFightTarget(Pawn pawn, LocalTargetInfo targetInfo)
        {
            Toil toil = new Toil();

            toil.tickAction = () => WatchTickAction();
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = this.job.def.joyDuration;
            toil.AddFinishAction(() => JoyUtility.TryGainRecRoomThought(pawn));
            toil.AddFailCondition(() => Utility_Tired.IsTooTired(this.GetActor()));
            toil.socialMode = RandomSocialMode.SuperActive;
            return toil;

        }
        

        private void FightTarget(Pawn fighter, LocalTargetInfo targetInfo)
        {

            Verb attackVerb = null;
            if (fighter != null)
                attackVerb = fighter.TryGetAttackVerb(targetInfo.Thing, false);

            if (attackVerb != null)
            {

                // Only added because the TryStartCast does throw an error that I can't find..
                // This is the WorkAround for the unexplainable Melee-Verb-Error
                // ================================
                if (fighter.stances.FullBodyBusy)
                    return;

                SoundDef soundDef;
                if (Rand.Value >= 0.6f)
                {
                    soundDef = SoundDef.Named("Pawn_Melee_Punch_HitBuilding");
                    fighter.skills.Learn(SkillDefOf.Melee, 25f);
                }
                else
                {
                    soundDef = SoundDef.Named("Pawn_Melee_Punch_Miss");
                    fighter.skills.Learn(SkillDefOf.Melee, 10f);
                }
                
                soundDef.PlayOneShot(new TargetInfo(targetInfo.Cell, Map, false));

                Stance_Cooldown stance_Cooldown = fighter.stances.curStance as Stance_Cooldown;
                if (stance_Cooldown == null || stance_Cooldown.ticksLeft >= 50)
                {
                    fighter.stances.SetStance(new Stance_Cooldown(50, targetInfo, attackVerb));
                }
                // ================================


                // Original:
                // This would be the original code, if the Melee-Verb-Error wouldn't come..
                //attackVerb.TryStartCastOn(targetInfo); // Throws NullReference -> Why, can't locate origin???  -  Also throws error 'PAWN meleed OBJECT from out of melee position.' -> WHY, I'm standing right next to it??? 
            }


            // increase the experienced xp
            int ticksSinceLastShot = GenTicks.TicksAbs - lastTick;
            lastTick = GenTicks.TicksAbs;
            if (ticksSinceLastShot > 2000)
                ticksSinceLastShot = 0;

            if (fighter?.CurJob?.def?.joySkill != null &&
                    fighter.skills.GetSkill(fighter.CurJob.def.joySkill).GetLevel() <= Utility_MaxAllowedTrainingLevel.GetMaxAllowedTrainingLevel(pawn))
            {
                fighter.skills.GetSkill(fighter.CurJob.def.joySkill).Learn(joyCanEndJob ? fighter.CurJob.def.joyXpPerTick * ticksSinceLastShot * 1.2f : fighter.CurJob.def.joyXpPerTick * ticksSinceLastShot);
            }
        }
        private int lastTick;


    }
}
