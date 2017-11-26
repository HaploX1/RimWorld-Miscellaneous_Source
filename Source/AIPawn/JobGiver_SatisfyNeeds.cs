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
    /// This is the ThinkNode 'Satisfy your Needs' for mai.
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>Usage of this code is free. All I ask is that you mention my name somewhere.</permission>
    public class JobGiver_SatisfyNeeds : ThinkNode
    {
        private JobGiver_RechargeEnergy giverRechargeEnergy = new JobGiver_RechargeEnergy();
        private List<Need> statuses = new List<Need>();
        public float maxDistToSquadFlag = -1f;


        //public override void PostLoad()
        //{
        //    subNodes.Add(giverRechargeEnergy);
        //}F
        
       
        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobIssueParams)
        {
            statuses.Clear();
            if (pawn.needs != null && pawn.needs.rest != null && pawn.needs.rest.CurLevel <= 0.25)
            {
                statuses.Add(pawn.needs.rest);
            }

            for (int i = 0; i < this.statuses.Count; i++)
            {
                ThinkResult jobPackage = new ThinkResult();
                if (this.statuses[i] is Need_Rest)
                {
                    jobPackage = this.giverRechargeEnergy.TryIssueJobPackage(pawn, jobIssueParams);
                }
                if (jobPackage.IsValid)
                {
                    if (this.maxDistToSquadFlag > 0f)
                    {
                        IntVec3 cell = jobPackage.Job.targetA.Cell;
                        if ((pawn.Position - cell).LengthHorizontalSquared > this.maxDistToSquadFlag * this.maxDistToSquadFlag)
                            return ThinkResult.NoJob;
                    }
                    return jobPackage;
                }
            }
            return ThinkResult.NoJob;
        }

    }
}
