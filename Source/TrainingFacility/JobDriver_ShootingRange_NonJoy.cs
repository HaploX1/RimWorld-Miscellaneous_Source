using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;


namespace TrainingFacility
{
    public class JobDriver_ShootingRange_NonJoy : JobDriver_ShootingRange
    {

        public JobDriver_ShootingRange_NonJoy()
        {
            base.joyCanEndJob = false;
        }

    }
}
