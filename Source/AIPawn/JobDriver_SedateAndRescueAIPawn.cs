using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace AIPawn
{
    public class JobDriver_SedateAndRescueAIPawn : JobDriver
    {
        //Shortcut properties
        public AIPawn Takee { get { return (AIPawn)this.job.targetA.Thing; } }
        public Building_AIPawnRechargeStation DropBed { get { return this.job.targetB.Thing as Building_AIPawnRechargeStation; } }

        public JobDriver_SedateAndRescueAIPawn() { }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Reserve the takee
            yield return Toils_Reserve.Reserve(TargetIndex.A, 1);

            //Reserve the bed
            yield return Toils_Reserve.Reserve(TargetIndex.B, 1);

            //Claim the bed for the takee
            Toil claimBed = new Toil();
            claimBed.initAction = () =>
                {
                    if (Takee.ownership != null && Takee.ownership.OwnedBed != DropBed && !DropBed.Medical)
                        Takee.ownership.ClaimBedIfNonMedical((Building_Bed)DropBed);
                };
            yield return claimBed;

            Func<bool> ownershipFail = () =>
                {
                    if (DropBed.Medical)
                    {
                        if (DropBed.AnyUnownedSleepingSlot &&
                           DropBed.CurOccupants != Takee) return true;
                    }
                    else if (DropBed.OwnersForReading != null && !DropBed.OwnersForReading.Contains(Takee)) return true;
                    return false;
                };

            //Goto takee
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                                       .FailOnDestroyedNullOrForbidden(TargetIndex.A)
                                       .FailOnDestroyedNullOrForbidden(TargetIndex.B)
                                       .FailOn(ownershipFail) //Abandon if takee loses bed ownership
                                       .FailOn(() => this.job.def == JobDefOf.Arrest && !Takee.CanBeArrestedBy(this.pawn))//Abandon arrest if takee is not of a team who is willing to be arrested
                                       .FailOn(() => !pawn.CanReach(DropBed, PathEndMode.OnCell, Danger.Deadly));
            //.FailOn(()=>!pawn.CanReach( DropBed, PathEndMode.OnCell, Danger.Deadly ) ); // From Alpha 8

            //Make unconscious if needed
            Toil makeUnconscious = new Toil();
            makeUnconscious.initAction = () =>
            {
                //Log.Error("Applying Anesthetic");
                //Takee.healthTracker.ApplyAnesthetic(); Does not work with AIPawn

                Takee.health.forceIncap = true;
                Takee.health.AddHediff(HediffDefOf.Anesthetic, null, null);
                Takee.health.forceIncap = false;
            };
            yield return makeUnconscious;

            //Start carrying the takee
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);

            //Change takee to prisoner if necessary
            Toil makePrisoner = new Toil();
            makePrisoner.initAction = () =>
            {
                if (this.job.def == JobDefOf.Arrest || this.job.def == JobDefOf.Capture || this.job.def == JobDefOf.TakeWoundedPrisonerToBed)
                {
                    if (Takee.HostFaction != Faction.OfPlayer)
                    {
                        Takee.guest.SetGuestStatus(Faction.OfPlayer, true);
                    }
                }
            };
            yield return makePrisoner;

            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch)
                                    .FailOnDestroyedNullOrForbidden(TargetIndex.A);
            //Note no failure conditions here
            //Because otherwise it's easy to get wardens to drop prisoners in arbitrary places.
            //I'd rather they just go to wherever they were going.
            // if( DropBed.owner != Takee )
            //	return true;
            //return !Toils.CanInteractStandard(DropBed);


            //Unreserve bed so takee can use it
            yield return Toils_Reserve.Release(TargetIndex.B);


            //Drop in or near bed
            Toil tuckIntoBed = new Toil();
            tuckIntoBed.initAction = () =>
            {
                //Note: We don't stop the task if the bed is destroyed or changes ownership
                //because then the wardens drop prisoners at random points and they escape
                //So we have to handle some ugly cases here

                //note this may use the position of a destroyed bed
                IntVec3 dropPos = DropBed.Position;

                Thing unused;
                pawn.carryTracker.TryDropCarriedThing(dropPos, ThingPlaceMode.Direct, out unused);

                //Should we tuck them into bed?
                if ((Takee.Downed || Takee.health.HasHediffsNeedingTend(false) || 
                        ((HealthAIUtility.ShouldSeekMedicalRest(Takee) || HealthAIUtility.ShouldBeTendedNowByPlayer(Takee) || HealthAIUtility.ShouldBeTendedNowByPlayerUrgent(Takee)) && 
                        DropBed.Medical))
                    && !DropBed.Destroyed
                    && (DropBed.OwnersForReading.Contains( Takee) || (DropBed.Medical && DropBed.AnyUnoccupiedSleepingSlot))  //They could have lost ownership and the last toil would continue
                  )
                {
                    Takee.jobs.Notify_TuckedIntoBed(DropBed);
                }

                if (Takee.IsPrisonerOfColony)
                    LessonAutoActivator.TeachOpportunity(ConceptDefOf.PrisonerTab, Takee, OpportunityType.GoodToKnow);

            };
            tuckIntoBed.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return tuckIntoBed;
        }

    }
}
