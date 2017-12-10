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


namespace TacticalComputer
{
    /// <summary>
    /// This driver calls a pawn to a square, where he then will find a random standable square and will be drafted
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>Please check the provided license info for granted permissions.</permission>
    public class JobDriver_GotoCellAndDraft : JobDriver
    {

        public PathEndMode PathEndMode = PathEndMode.ClosestTouch;
        public int spreadOut = 5;

        public JobDriver_GotoCellAndDraft() { }

        public override bool TryMakePreToilReservations()
        {
            this.pawn.Map.pawnDestinationReservationManager.Reserve(pawn, job, job.targetA.Cell);
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Goto Target
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);

            // Spread out
            Toil setPosition = new Toil()
            {
                initAction = () =>
                {
                    IntVec3 freeCell = CellFinder.RandomClosewalkCellNear(pawn.Position, pawn.Map, spreadOut);
                    LocalTargetInfo ti = new LocalTargetInfo(freeCell);
                    pawn.pather.StartPath(ti, PathEndMode.OnCell);
                },
                defaultCompleteMode = ToilCompleteMode.PatherArrival
            };
            yield return setPosition;

            // Set playerController to drafted
            Toil arrivalDraft = new Toil();
            arrivalDraft.initAction = () =>
            {
                pawn.drafter.Drafted = true;
                Find.Selector.Select(pawn);
            };
            arrivalDraft.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return arrivalDraft;
        }
    }
}
