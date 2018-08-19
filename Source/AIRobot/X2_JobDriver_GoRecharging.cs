using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace AIRobot
{
    public class X2_JobDriver_GoRecharging : X2_JobDriver_GoDespawning
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            despawn = false;
            foreach (Toil toil in base.MakeNewToils())
                yield return toil;
        }
    }
}
