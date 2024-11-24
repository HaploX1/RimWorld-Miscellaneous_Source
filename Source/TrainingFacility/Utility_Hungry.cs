using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TrainingFacility
{
    public static class Utility_Hungry
    {
        public static bool IsTooHungry(Pawn actor)
        {
            if (actor?.needs?.food != null && actor.needs.food.CurCategory >= HungerCategory.UrgentlyHungry)
            {
                return true;
            }
            return false;
        }
    }
}
