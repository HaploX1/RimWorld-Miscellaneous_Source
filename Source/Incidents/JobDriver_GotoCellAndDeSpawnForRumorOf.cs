using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
//using VerseBase;
using Verse;
using Verse.AI;
//using Verse.Sound;
using RimWorld;
//using RimWorld.Planet;
//using RimWorld.SquadAI;


namespace Incidents
{
    /// <summary>
    /// This JobDriver calls a pawn to a square. Arriving there he will leave the map
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>Please check the provided license info for granted permissions.</permission>
    public class JobDriver_GotoCellAndDeSpawnForRumorOf : JobDriver
    {

        public JobDriver_GotoCellAndDeSpawnForRumorOf() { }

        protected override IEnumerable<Toil> MakeNewToils()
        {

            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);

            // Add pawn to MapComponent
            Toil arrival = new Toil();
            arrival.initAction = () =>
            {
                MapComponent_ColonistsOutsideMap_RumorOf mc;

                if (!MapComponent_ColonistsOutsideMap_RumorOf.IsMapComponentAvailable(out mc))
                    return;

                mc.ExitMapCell = TargetA.Cell;
                mc.PawnsOutOfMap.Add(pawn);
                pawn.DeSpawn();

            };
            arrival.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return arrival;

        }
    }
}
