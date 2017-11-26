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
    public class Building_AIPawnCreatorEnhanced_male : Building_AIPawnCreatorEnhanced
    {

        public override void Tick()
        {
            base.gender = Gender.Male;
            base.Tick();
        }

        public override Pawn Create()
        {
            base.gender = Gender.Male;
            return base.Create();
        }

    }
}
