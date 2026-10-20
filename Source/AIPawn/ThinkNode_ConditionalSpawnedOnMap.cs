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
    /// <summary>
    /// Guard for the MAI think trees (AIPawn, AIPawnConstant).
    /// A despawned pawn is deregistered from the tick list one tick late: Thing.DeSpawn only
    /// queues the deregistration and TickList.Tick removes the pawn with the next pass, while
    /// the tick loop itself checks Destroyed but not Spawned. Pawn.TickInterval therefore runs
    /// the job tracker once more for a pawn that is already off the map (being carried or held
    /// in a container), and the vanilla Downed subtree reaches JobGiver_FleeDanger and
    /// FleeUtility.FleeLargeFireJob, which dereferences pawn.Map without a null check.
    /// While this node is unsatisfied the whole tree is skipped, so nothing is issued for a pawn
    /// that is not on a map. Spawned pawns behave exactly as before.
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>For usage of this code, please look at the license information.</permission>
    public class ThinkNode_ConditionalSpawnedOnMap : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            return pawn.Spawned && pawn.Map != null;
        }
    }
}
