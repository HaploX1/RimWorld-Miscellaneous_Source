using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;

namespace EmergencyPowerCore
{
    public class Building_PowerPlant_EmergencyPowerCore : Building
    {

        protected CompPowerTrader_EmergencyPowerCore powerComp;

        protected CompRefuelable refuelableComp;

        protected CompBreakdownable breakdownableComp;

        protected CompFlickable flickableComp;

        protected virtual float DesiredPowerOutput
        {
            get
            {
                return -this.powerComp.Props.PowerConsumption;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            LongEventHandler.ExecuteWhenFinished(SpawnSetup_Part2);

        }

        /// <summary>
        /// This is called seperately when the Mod-Thread is done.
        /// It is needed to be seperately from SpawnSetup, so that the graphics can be found
        /// </summary>
        private void SpawnSetup_Part2()
        {

            this.powerComp = base.GetComp<CompPowerTrader_EmergencyPowerCore>();
            this.refuelableComp = base.GetComp<CompRefuelable>();
            this.breakdownableComp = base.GetComp<CompBreakdownable>();
            this.flickableComp = base.GetComp<CompFlickable>();
            if (!this.IsBrokenDown())
            {
                this.powerComp.PowerOn = true;
            }
        }

        protected override void Tick()
        {
            base.Tick();
            if ((this.breakdownableComp != null && this.breakdownableComp.BrokenDown) || (this.refuelableComp != null && !this.refuelableComp.HasFuel) || (this.flickableComp != null && !this.flickableComp.SwitchIsOn) || (this.powerComp != null && !this.powerComp.PowerOn))
            {
                this.powerComp.PowerOutput = 0f;
            }
            else
            {
                this.powerComp.PowerOutput = this.DesiredPowerOutput;
            }
        }

    }
}
