using System;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;

namespace TrainingFacility
{
    public class JobDriver_Archery : JobDriver_WatchBuilding
    {
        private const int ArrowShootInterval = 350;

        public JobDriver_Archery() {}

        protected override void WatchTickAction()
        {
            if (this.pawn.IsHashIntervalTick(ArrowShootInterval))
            {
                ShootArrow(this.pawn, base.TargetA.Cell);
            }
            base.WatchTickAction();
        }

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

            yield return Toils_Reserve.Reserve(TargetIndex.A, this.job.def.joyMaxParticipants, 0);
            yield return Toils_Reserve.Reserve(TargetIndex.B);
            
            if (TargetC.HasThing)
                yield return Toils_Reserve.Reserve(TargetIndex.C);

            Toil getWeapon = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return getWeapon;

            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);

            Toil play = new Toil();
            play.tickAction = () => WatchTickAction();
            play.defaultCompleteMode = ToilCompleteMode.Delay;
            play.defaultDuration = this.job.def.joyDuration;
            play.AddFinishAction(() => JoyUtility.TryGainRecRoomThought(pawn)); 
            yield return play;

            Toil returnWeapon = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return returnWeapon;
            
        }


        /// <summary>
        /// The for arrows ballanced mote thrower
        /// Original: MoteThrower.ThrowHorseshoe
        /// </summary>
        public static void ShootArrow(Pawn thrower, IntVec3 targetCell)
        {
            if (!thrower.Position.ShouldSpawnMotesAt(thrower.Map) || thrower.Map.moteCounter.Saturated)
                return;

            float speed = Rand.Range(4.0f, 6.5f) * 1.8f;
            Vector3 vector = targetCell.ToVector3Shifted() + Vector3Utility.RandomHorizontalOffset((1f - (float)thrower.skills.GetSkill(SkillDefOf.Shooting).Level / 20f) * 1.8f);
            vector.y = thrower.DrawPos.y;
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Mote_ArcheryArrow"), null);
            moteThrown.Scale = 1f;
            moteThrown.rotationRate = 0f;
            moteThrown.exactPosition = thrower.DrawPos;
            moteThrown.exactRotation =(vector - moteThrown.exactPosition).AngleFlat(); // this corrects the angle of the arrow 
            moteThrown.SetVelocity((vector - moteThrown.exactPosition).AngleFlat(), speed);
            moteThrown.airTimeLeft = ((moteThrown.exactPosition - vector).MagnitudeHorizontal() / speed);
            if (moteThrown.airTimeLeft > 1f) // reduce the airtime randomly to let the arrows be too short from time to time
                moteThrown.airTimeLeft -= Rand.Value;
            GenSpawn.Spawn(moteThrown, thrower.Position, thrower.Map);
        }

    }
}
