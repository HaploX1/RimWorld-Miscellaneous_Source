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
    public class JobDriver_ShootingRange : JobDriver_WatchBuilding
    {
        private const int UpdateInterval = 350;
        protected bool joyCanEndJob = true;

        public JobDriver_ShootingRange() {}

        public override bool TryMakePreToilReservations()
        {
            return base.TryMakePreToilReservations();
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //TargetA is the building
            //TargetB is the place to stand to watch
            //TargetC is the chair to sit in (can be null)

            this.EndOnDespawnedOrNull(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.A);

            yield return Toils_Reserve.Reserve(TargetIndex.A, this.job.def.joyMaxParticipants, 0);
            yield return Toils_Reserve.Reserve(TargetIndex.B);
            
            if (TargetC.HasThing)
                yield return Toils_Reserve.Reserve(TargetIndex.C);

            // go to shooting cell
            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
            // shoot at target 
            yield return GetToil_ShootAtTarget(this.pawn, TargetA);

        }

        public Toil GetToil_ShootAtTarget(Pawn shooter, LocalTargetInfo targetInfo)
        {

            Toil toil = new Toil();
            toil.tickAction = () => WatchTickAction();
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = this.job.def.joyDuration;
            toil.AddFinishAction(() => JoyUtility.TryGainRecRoomThought(shooter));
            toil.socialMode = RandomSocialMode.SuperActive;
            return toil;

        }

        private int weaponCheckResult = 0; 
        protected override void WatchTickAction()
        {
            // Check for dangerous weapons (One-Shot, grenades, destroy on drop, ...)
            if (weaponCheckResult == 0)
            {
                Verb attackVerb = pawn.TryGetAttackVerb(false);
                if (pawn != null && pawn.equipment != null && pawn.equipment.Primary != null && attackVerb != null)
                {
                    ThingDef primaryDef = pawn.equipment.Primary.def;

                    if (attackVerb.IsEMP() || attackVerb.IsIncendiary() || attackVerb.verbProps.ai_IsBuildingDestroyer ||
                        primaryDef.destroyOnDrop || attackVerb is Verb_ShootOneUse || primaryDef.thingCategories.Any(d => d == DefDatabase<ThingCategoryDef>.GetNamed("Grenades")))
                    {
                        weaponCheckResult = -1;

                        if (pawn.Faction != null && pawn.Faction == Faction.OfPlayer)
                            Messages.Message("TrainingFacility_DangerousWeaponFound_ThrowingStones".Translate(pawn.NameStringShort), pawn, MessageTypeDefOf.NeutralEvent);
                        else
                            Messages.Message("TrainingFacility_DangerousWeaponFound_ThrowingStones".Translate(pawn.NameStringShort), pawn, MessageTypeDefOf.SilentInput);
                        //Log.Error("WeaponCheck ==> Illegal weapon found. No shooting allowed!");
                    }
                    else
                        weaponCheckResult = 1;
                }
                else
                    weaponCheckResult = 1;
            }


            if (this.pawn.IsHashIntervalTick(UpdateInterval))
            {
                if (weaponCheckResult > 0)
                    AttackTarget(this.pawn, TargetA.Cell);
                else
                {
                    // dangerous weapons --> Throw some stones instead
                    MoteMaker.ThrowStone(pawn, TargetA.Cell);
                    pawn.skills.GetSkill(pawn.CurJob.def.joySkill).Learn(5);

                    // Alternate: Shoot some arrows?
                    //JobDriver_Archery.ShootArrow(pawn, TargetA.Cell);
                }
            }


            //base.StandTickAction();

            this.pawn.rotationTracker.FaceCell(base.TargetA.Cell);
            this.pawn.GainComfortFromCellIfPossible();

            //JoyUtility.JoyTickCheckEnd(this.pawn, false, 1f); // changed; => needs to be disabled when not joy activity or it will end the job!

            Job curJob = pawn.CurJob;
            if (pawn.needs.joy.CurLevel <= 0.9999f) // changed, else it would throw an error: joyKind NullRef ???
            {
                pawn.needs.joy.GainJoy(1f * curJob.def.joyGainRate * 0.000144f, curJob.def.joyKind);
            }
            if (curJob.def.joySkill != null)
            {
                pawn.skills.GetSkill(curJob.def.joySkill).Learn(curJob.def.joyXpPerTick);
            }
            if (joyCanEndJob)
            {
                if (!pawn.GetTimeAssignment().allowJoy) // changed => disable TimeAssignment
                    pawn.jobs.curDriver.EndJobWith(JobCondition.InterruptForced);

                if (pawn.needs.joy.CurLevel > 0.9999f) // changed => disable Max Joy
                    pawn.jobs.curDriver.EndJobWith(JobCondition.Succeeded);

            }
        }

        private void AttackTarget(Pawn shooter, LocalTargetInfo targetInfo)
        {
            Verb attackVerb = null;
            if (shooter != null)
                attackVerb = shooter.TryGetAttackVerb(false);
            
            if (attackVerb != null)
                attackVerb.TryStartCastOn(targetInfo);



            // increase the experienced xp
            int ticksSinceLastShot = GenTicks.TicksAbs - lastTick;
            lastTick = GenTicks.TicksAbs;
            if (ticksSinceLastShot > 2000)
                ticksSinceLastShot = 0;
            if (shooter.CurJob.def.joySkill != null)
                shooter.skills.GetSkill(shooter.CurJob.def.joySkill).Learn(shooter.CurJob.def.joyXpPerTick * ticksSinceLastShot);
        }
        private int lastTick;
    }
}
