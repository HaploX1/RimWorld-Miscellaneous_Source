using System;
using Verse;
using Verse.AI;
using RimWorld;

namespace TacticalComputer
{
    public class JoyGiver_PlayTacticalComputer : JoyGiver_InteractBuilding
    {

        protected override Job TryGivePlayJob(Pawn pawn, Thing t)
        {
            CompPowerTrader compPowerTrader = t.TryGetComp<CompPowerTrader>();
            if (compPowerTrader != null && !compPowerTrader.PowerOn)
                return null;

            return new Job(this.def.jobDef, t);
        }

    }
}
