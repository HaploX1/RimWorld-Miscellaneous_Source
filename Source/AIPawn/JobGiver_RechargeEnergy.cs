using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace AIPawn
{
    /// <summary>
    /// This is the JobGiver 'Recharge your Energy' for mai.
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>Usage of this code is free. All I ask is that you mention my name somewhere.</permission>
    public class JobGiver_RechargeEnergy : JobGiver_GetRest
    {

        public override float GetPriority(Pawn pawn)
        {
            Need_Rest needRest = pawn.needs.rest;
            if (needRest == null)
            {
                return 0f;
            }
            float curLevel = needRest.CurLevel;
            TimeAssignmentDef timeAssignmentDef = (pawn.timetable != null ? pawn.timetable.CurrentAssignment : TimeAssignmentDefOf.Anything);
            if (timeAssignmentDef == TimeAssignmentDefOf.Anything)
            {
                if (curLevel < 0.3f)
                {
                    return 8f;
                }
                return 0f;
            }
            if (timeAssignmentDef == TimeAssignmentDefOf.Work)
            {
                return 0f;
            }
            if (timeAssignmentDef == TimeAssignmentDefOf.Joy)
            {
                if (curLevel < 0.3f)
                {
                    return 8f;
                }
                return 0f;
            }
            if (timeAssignmentDef != TimeAssignmentDefOf.Sleep)
            {
                throw new NotImplementedException();
            }
            if (curLevel < 0.75f)
            {
                return 8f;
            }
            return 0f;
        }


        protected override Job TryGiveJob(Pawn pawn)
        {
            if (Find.TickManager.TicksGame - pawn.mindState.lastDisturbanceTick < 400)
                return null;

            AIPawn aiPawn = pawn as AIPawn;
            Building_AIPawnRechargeStation rechargeStation = HelperAIPawn.FindRechargeStationFor(aiPawn);

            if (rechargeStation == null)
                return null;

            if (rechargeStation.owners != null && !rechargeStation.owners.Contains(aiPawn) && !rechargeStation.Medical)
                aiPawn.ownership.ClaimBedIfNonMedical(rechargeStation);

            if (aiPawn.ownership.OwnedBed == null || aiPawn.ownership.OwnedBed != rechargeStation)
                return null;

            Job job = new Job(JobDefOf.LayDown, rechargeStation);
            return job;
        }

    }
}
