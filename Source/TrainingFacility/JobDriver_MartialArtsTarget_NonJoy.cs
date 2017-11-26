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
    public class JobDriver_MartialArtsTarget_NonJoy : JobDriver_MartialArtsTarget
    {

        public JobDriver_MartialArtsTarget_NonJoy()
        {
            base.joyCanEndJob = false;
        }

    }
}
