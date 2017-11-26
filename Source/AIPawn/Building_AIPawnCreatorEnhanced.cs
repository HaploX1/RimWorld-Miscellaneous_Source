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
    /// This is the mai creator.
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>For usage of this code, please look at the license information.</permission>
    public class Building_AIPawnCreatorEnhanced : Building_AIPawnCreator
    {

        public override void Tick()
        {
            pawnDefName = "AIPawnE";
            base.Tick();
        }

        public override Pawn Create()
        {
            pawnDefName = "AIPawnE";
            return base.Create();
        }

    }
}
