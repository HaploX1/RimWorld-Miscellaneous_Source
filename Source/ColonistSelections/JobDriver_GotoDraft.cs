using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed
using Verse.AI; // Needed when you do something with the AI
using Verse.Sound; // Needed when you do something with the Sound

namespace ColonistSelections
{
    public class JobDriver_GotoDraft : JobDriver
    {
        public JobDriver_GotoDraft() { }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            this.pawn.Map.pawnDestinationReservationManager.Reserve(this.pawn, this.job, this.job.targetA.Cell);
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Map map = this.Map;

            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);

            Toil arrive = new Toil();
            arrive.initAction = () =>
            {
                if (this.job.exitMapOnArrival && pawn.Position.OnEdge(map))
                {
                    pawn.ExitMap(false, Rot4.Random);
                }
            };
            arrive.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return arrive;

            Toil scatter = new Toil();
            scatter.initAction = () =>
            {
                List<Thing> thingsHere = map.thingGrid.ThingsListAt(pawn.Position);
                bool foundOtherPawnHere = false;
                for (int i = 0; i < thingsHere.Count; i++)
                {
                    Pawn p = thingsHere[i] as Pawn;
                    if (p != null && p != pawn)
                    {
                        foundOtherPawnHere= true;
                        break;
                    }
                }

                LocalTargetInfo tp;
                if (foundOtherPawnHere)
                {
                    IntVec3 freeCell = CellFinder.RandomClosewalkCellNear(pawn.Position, map, 2);
                    tp = new LocalTargetInfo(freeCell);
                }
                else
                    tp = new LocalTargetInfo(pawn.Position);

                pawn.pather.StartPath(tp, PathEndMode.OnCell);
            };
            scatter.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            yield return scatter;

            // Set playerController to drafted
            Toil arrivalDraft = new Toil();
            arrivalDraft.initAction = () =>
            {
                pawn.drafter.Drafted = true;
            };
            arrivalDraft.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return arrivalDraft;

        }

    }
}
