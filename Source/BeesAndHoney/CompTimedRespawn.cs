using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed


namespace BeeAndHoney
{
    // work base: CompRottable
    
    public class CompTimedRespawn : ThingComp
    {
        private enum RespawnReasons
        {
            none,
            temperature,
            time
        }
        private RespawnReasons reason = RespawnReasons.none;

        private bool _active = true;
        public bool Active
        {
            get
            {
                if (parent.Map == null || parent.Position == null)
                    return false;

                return _active;
            }
            set
            {
                _active = value;
            }
        }

        private float activeTimeInt;
        public float ActiveTime
        {
            get
            {
                return this.activeTimeInt;
            }
            set
            {
                this.activeTimeInt = value;
            }
        }

        private CompProperties_TimedRespawn PropsTimedRespawn
        {
            get
            {
                return (CompProperties_TimedRespawn)this.props;
            }
        }
        

        public int TicksUntilRespawnAtCurrentTemp
        {
            get
            {
                if (parent.PositionHeld == null || parent.MapHeld == null)
                    return int.MaxValue;

                float tempCell = GenTemperature.GetTemperatureForCell(parent.PositionHeld, parent.MapHeld);
                tempCell = Mathf.RoundToInt(tempCell);
                bool tempValid = IsTemperatureValid(tempCell);
                if (!tempValid)
                {
                    reason = RespawnReasons.temperature;
                    return 0; // Outside valid temp -> respawn is now.
                }

                int remainingTicks = Mathf.RoundToInt(this.PropsTimedRespawn.TicksToRespawn - this.ActiveTime); ;
                if (remainingTicks <= 0)
                {
                    // Now
                    return 0;
                }
                return remainingTicks;
            }
        }

        private bool IsTemperatureValid(float temperature)
        {
            // Do not use temp -> valid
            if (!this.PropsTimedRespawn.useTempRange)
                return true;

            // Temp inside range -> valid
            if (temperature > this.PropsTimedRespawn.goodTempRange.min && temperature < this.PropsTimedRespawn.goodTempRange.max)
            {
                return true;
            }
            return false;
        }

        public void SpawnDefAndVanishSelf()
        {
            if (this.PropsTimedRespawn.changeDef != null)
            {
                Thing thing = GenSpawn.Spawn(this.PropsTimedRespawn.changeDef, this.parent.PositionHeld, this.parent.MapHeld);
                thing.SetForbiddenIfOutsideHomeArea();
            }

            // Show message if the cell is inside the home area
            if (parent.MapHeld.areaManager.Home.ActiveCells.Contains( this.parent.PositionHeld))
            {
                switch (reason)
                {
                    case RespawnReasons.temperature:
                        Messages.Message("BeeAndHoney_QueenBeeLostBecauseOfTemperature".Translate(), new TargetInfo(this.parent.PositionHeld, parent.MapHeld), MessageTypeDefOf.NegativeEvent);
                        break;

                    case RespawnReasons.time:
                        Messages.Message("BeeAndHoney_QueenBeeLostBecauseOfTime".Translate(), new TargetInfo(this.parent.PositionHeld, parent.MapHeld), MessageTypeDefOf.NeutralEvent);
                        break;
                }
            }

            this.parent.Destroy(DestroyMode.Vanish);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this._active, "active", true, false);
            Scribe_Values.Look<float>(ref this.activeTimeInt, "activeTime", 0f, false);

            //if (!_active && activeTimeInt == 0f)
            //    _active = true;
        }

        public override void CompTickRare()
        {
            //if (parent.Map == null)
            //    Log.Error("Map == null");
            //if (parent.Position == null)
            //    Log.Error("Position == null");

            //Log.Error("Active:" + Active.ToString() + " Time:" + ActiveTime.ToString( "0.##" ));

            if (!this.Active)
                return;

           

            // Update ActiveTime
            this.ActiveTime = this.ActiveTime += 250;

            // Do work if time is finished
            int ticksUntilRespawnAtCurrentTemp = this.TicksUntilRespawnAtCurrentTemp;
            if (ticksUntilRespawnAtCurrentTemp <= 0)
            {
                if (reason == RespawnReasons.none)
                    reason = RespawnReasons.time;

                SpawnDefAndVanishSelf();
            }
            
        }

        public override void PreAbsorbStack(Thing otherStack, int count)
        {
            float t = (float)count / (float)(this.parent.stackCount + count);
            float remainTime = ((ThingWithComps)otherStack).GetComp<CompTimedRespawn>().ActiveTime;
            this.ActiveTime = Mathf.Lerp(this.ActiveTime, remainTime, t);
        }

        public override void PostSplitOff(Thing piece)
        {
            ((ThingWithComps)piece).GetComp<CompTimedRespawn>().ActiveTime = this.ActiveTime;
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder stringBuilder = new StringBuilder();

            float num = (float)this.PropsTimedRespawn.TicksToRespawn - this.ActiveTime;
            if (num > 0f)
            {
                int ticksUntilRespawnAtCurrentTemp = this.TicksUntilRespawnAtCurrentTemp;

                stringBuilder.AppendLine("BeeAndHoney_RemainingTime".Translate(ticksUntilRespawnAtCurrentTemp.ToStringTicksToPeriod()));
                
                //if (Prefs.DevMode && Current.ProgramState == ProgramState.MapPlaying)
                //{
                //    stringBuilder.AppendLine("BeeAndHoney_RemainingTime".Translate(new object[]
                //    {
                //            ticksUntilRespawnAtCurrentTemp.ToString()
                //    }));
                //}


            }
            return stringBuilder.ToString().Trim();
        }







    }
}
