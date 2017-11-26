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
    public class Building_AIPawnCreator_male : Building_AIPawnCreator
    {

        public override void Tick()
        {
            base.gender = Gender.Male;

            //Log.Error("MAI gender (tick male): " + gender.ToString());
            
            base.Tick();
        }

        public override Pawn Create()
        {
            base.gender = Gender.Male;
            //Log.Error("MAI gender (create male): " + gender.ToString());
            return base.Create();
        }

    }
}
