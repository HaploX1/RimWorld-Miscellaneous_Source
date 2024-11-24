using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;

namespace TrainingFacility
{
    public static class Utility_Tired
    {
        public static IJobEndable EndOnTired(IJobEndable f, JobCondition endCondition = JobCondition.InterruptForced)
        {
            Pawn actor = f.GetActor();
            bool isTired = IsTooTired(actor);

            f.AddEndCondition(() => !isTired ? JobCondition.Ongoing : endCondition);
            return f;
        }

        public static bool IsTooTired(Pawn actor)
        {
            if (actor?.needs?.rest != null && actor.needs.rest.CurLevel < (Need_Rest.ThreshTired - (Need_Rest.ThreshVeryTired / 2)) + 0.03)
            {
                return true;
            }
            return false;
        }

    }
}
