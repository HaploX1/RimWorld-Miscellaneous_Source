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


namespace Jobs
{
    /// <summary>
    /// This JobDriver calls a pawn to a square. Arriving there he will switch to drafted mode
    /// Note: You will need a Job-XML-File to use this driver. I've descibed it at the end
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>Please check the provided license info for granted permissions.</permission>
    public class TEMPLATE_JobDriver_GotoCellAndDoX : JobDriver
    {

        // Initiation
        // Note: Needs to be the same name as the class!
        public TEMPLATE_JobDriver_GotoCellAndDoX() { }



        // Toils: These are the parts of the job. 
        // This is something like: Go there, reserve that and then haul it over there
        protected override IEnumerable<Toil> MakeNewToils()
        {

            // Toil 1:
            // Goto Target (TargetPack A is selected (It has the info where the target cell is))
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);


            // Toil 2:
            // Set playerController to drafted => The pawn switches to being drafted
            Toil arrivalDraft = new Toil();
            arrivalDraft.initAction = () =>
            {
                // Here you can insert your own code about what should be done
                // At the time when this toil is executed, the pawn is at the goto-cell from the first toil
                pawn.drafter.Drafted = true;
            };
            arrivalDraft.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return arrivalDraft;


            // Toil X:
            // You can add more and more toils, the pawn will do them one after the other. And everything is just one job..
            // End every toil with a "yield return toilName"
        }
    }
}




/*

This is the needed XML file to make a real Job from the JobDriver
     
<?xml version="1.0" encoding="utf-8" ?>
<JobDefs>
<!--========= Job ============-->
<JobDef>
<defName>GoToTargetAndDraft</defName>
<driverClass>Jobs.TEMPLATE_JobDriver_GotoCellAndDoX</driverClass>
<reportString>Moving.</reportString>
</JobDef>
</JobDefs>
     
*/