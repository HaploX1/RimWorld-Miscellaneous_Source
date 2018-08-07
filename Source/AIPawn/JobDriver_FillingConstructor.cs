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
    public class JobDriver_FillConstructor : JobDriver
    {

        public JobDriver_FillConstructor() { }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            pawn.Map.pawnDestinationReservationManager.Reserve(pawn, job, job.targetB.Cell);

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Go next to target
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);

            yield return Toils_Haul.StartCarryThing(TargetIndex.B, true, true, false);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
        }

        private Toil Toils_FillThingIntoConstructor(Pawn actor, TargetIndex thingInd, TargetIndex constructorInd)
        {

            IntVec3 targetThing = pawn.jobs.curJob.GetTarget(constructorInd).Thing;
            Thing carryThing = pawn.jobs.curJob.GetTarget(thingInd).Thing;

            IEnumerable<Building_TurretWeaponBase> foundBuildings = Map.listerBuildings.AllBuildingsColonistOfClass<Building_TurretWeaponBase>();

            if (foundBuildings == null)
                return null;

            Building_TurretWeaponBase weaponBase = foundBuildings.Where(b => b.Position == targetCell).FirstOrDefault();

            if (weapon == null || weaponBase == null)
                return null;

            Toil toil = new Toil();
            toil.initAction = () =>
            {
                weaponBase.TryToInstallWeapon(weapon);

                // TEST Enabled -> Look at target, install weapon
                //IntVec3 lookAtCell = PawnRotator.RotFromAngleBiased((actor.Position - targetCell).AngleFlat).FacingCell;
                if (actor.carryTracker.TryDropCarriedThing(targetCell, ThingPlaceMode.Direct, out weapon))
                {
                    weapon.DeSpawn();
                }
            };

            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            toil.defaultDuration = 0;

            return toil;
        }



    }
}
