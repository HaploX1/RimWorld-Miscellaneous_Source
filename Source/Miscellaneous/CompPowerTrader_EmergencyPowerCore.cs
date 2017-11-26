using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using Verse.Sound;
using RimWorld;


namespace EmergencyPowerCore
{
    public class CompPowerTrader_EmergencyPowerCore : CompPower
    {
        public int generatingTime = 10000;
        public int cooldownTime = 5000;
        public int explosionChance = 5;
        public int explosionRadius = 5;
        public DamageDef explosionDamageDef = DamageDefOf.Bomb;

        public int showEmergencyTimeInSeconds = 60;

        private int tickStageStarted;

        private stage activeStage = stage.off;
        private bool showEmergencyTimer = false;

        private enum stage
        {
            off,
            generating,
            cooling
        }





        public override void CompTick()
        {
            base.CompTick();
            
            switch (activeStage)
            {
                case stage.off:

                    if (PowerOn)
                    {
                        activeStage = stage.generating;
                        SetStageStartTick();
                        return;
                    }

                    if (this.flickableComp != null && this.flickableComp.SwitchIsOn)
                        this.flickableComp.SwitchIsOn = false;

                    break;

                case stage.generating:

                    // Generation time is nearly done, show emergency message
                    if (!showEmergencyTimer && 
                        RemainingTicksInStage(generatingTime) - showEmergencyTimeInSeconds * GenTicks.TicksPerRealSecond < 0)
                    {
                        showEmergencyTimer = true;
                    }

                    // Generation time is done, no switch off occured -> Explosion chance!
                    bool generatingDone = false;
                    if (RemainingTicksInStage( generatingTime ) < 0)
                    {
                        if (Rand.Value < explosionChance / 100)
                        {
                            DoExplosion();
                        }

                        generatingDone = true;
                        PowerOn = false;
                    }


                    // Generation time is done, switch to next stage
                    if (generatingDone || !PowerOn)
                    {
                        activeStage = stage.cooling;
                        SetStageStartTick();
                        showEmergencyTimer = false;
                    }

                    break;

                case stage.cooling:

                    if (PowerOn)
                        PowerOn = false;

                    // Generation time is done
                    if (RemainingTicksInStage( cooldownTime ) < 0)
                    {
                        activeStage = stage.off;
                        SetStageStartTick();
                    }

                    break;
            }
        }

        public void DoExplosion()
        {
            GenExplosion.DoExplosion(parent.Position, parent.Map, explosionRadius, explosionDamageDef, parent);
        }

        public void SetStageStartTick()
        {
            this.tickStageStarted = GenTicks.TicksAbs;
        }

        public int RemainingTicksInStage(int maxTicksInStage)
        {
            int workTime1 = GenTicks.TicksAbs - tickStageStarted;
            return maxTicksInStage - workTime1;
            
        }






        // ==================================================
        // From original CompPowerTrader. Changes are marked!
        // ==================================================

        public const string PowerTurnedOnSignal = "PowerTurnedOn";
        public const string PowerTurnedOffSignal = "PowerTurnedOff";

        public Action powerStartedAction;
        public Action powerStoppedAction;

        private bool powerOnInt;
        public float powerOutputInt;

        private bool powerLastOutputted;
        private Sustainer sustainerPowered;
        private CompFlickable flickableComp;

        public float PowerOutput
        {
            get
            {
                return this.powerOutputInt;
            }
            set
            {
                this.powerOutputInt = value;
                if (this.powerOutputInt > 0f)
                {
                    this.powerLastOutputted = true;
                }
                if (this.powerOutputInt < 0f)
                {
                    this.powerLastOutputted = false;
                }
            }
        }

        public float EnergyOutputPerTick
        {
            get
            {
                return this.PowerOutput * CompPower.WattsToWattDaysPerTick;
            }
        }

        public bool DesirePowerOn
        {
            get
            {
                return this.flickableComp == null || this.flickableComp.SwitchIsOn;
            }
        }

        public bool PowerOn
        {
            get
            {
                return this.powerOnInt;
            }
            set
            {
                if (this.powerOnInt == value)
                    return;


                // === NEW ADDITION ===  
                // Allow power on only if the correct stage is active!
                // Work with power off signal

                if (activeStage == stage.cooling && value == true)
                    return;
                if (activeStage == stage.generating && value == false)
                {
                    activeStage = stage.cooling;
                    this.tickStageStarted = GenTicks.TicksAbs;
                    showEmergencyTimer = false;
                }

                // === END ===


                this.powerOnInt = value;
                if (this.powerOnInt)
                {
                    if (!this.DesirePowerOn)
                    {
                        Log.Warning("Tried to power on " + this.parent + " which did not desire it.");
                        return;
                    }
                    if (this.parent.IsBrokenDown())
                    {
                        Log.Warning("Tried to power on " + this.parent + " which is broken down.");
                        return;
                    }
                    if (this.powerStartedAction != null)
                    {
                        this.powerStartedAction();
                    }
                    this.parent.BroadcastCompSignal(PowerTurnedOnSignal);
                    SoundDef soundDef = ((CompProperties_Power_EmergencyPowerCore)this.parent.def.CompDefFor<CompPowerTrader_EmergencyPowerCore>()).soundPowerOn;
                    if (soundDef.NullOrUndefined())
                    {
                        soundDef = SoundDefOf.PowerOnSmall;
                    }
                    
                    soundDef.PlayOneShot(new TargetInfo(this.parent.Position, parent.Map, false));
                    this.StartSustainerPoweredIfInactive();
                }
                else
                {
                    if (this.powerStoppedAction != null)
                    {
                        this.powerStoppedAction();
                    }
                    this.parent.BroadcastCompSignal(PowerTurnedOffSignal);
                    SoundDef soundDef2 = ((CompProperties_Power_EmergencyPowerCore)this.parent.def.CompDefFor<CompPowerTrader_EmergencyPowerCore>()).soundPowerOff;
                    if (soundDef2.NullOrUndefined())
                    {
                        soundDef2 = SoundDefOf.PowerOffSmall;
                    }
                    soundDef2.PlayOneShot(new TargetInfo(this.parent.Position, parent.Map, false));
                    this.EndSustainerPoweredIfActive();
                }
            }
        }

        public string DebugString
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(this.parent.LabelCap + " CompPower:");
                stringBuilder.AppendLine("   PowerOn: " + this.PowerOn);
                stringBuilder.AppendLine("   energyProduction: " + this.PowerOutput);
                return stringBuilder.ToString();
            }
        }

        public override void ReceiveCompSignal(string signal)
        {
            if (signal == "FlickedOff" || signal == "Breakdown")
            {
                this.PowerOn = false;
            }
            if (signal == "RanOutOfFuel" && this.powerLastOutputted)
            {
                this.PowerOn = false;
            }
            if (signal == "FlickedOn")
            {
                this.PowerOn = true;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            this.flickableComp = this.parent.GetComp<CompFlickable>();
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            this.EndSustainerPoweredIfActive();
            this.powerOutputInt = 0f;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.powerOnInt, "powerOn", true, false);

            // === NEW ADDITION ===

            Scribe_Values.Look<stage>(ref this.activeStage, "activeStage", stage.off, false);
            Scribe_Values.Look<int>(ref this.tickStageStarted, "tickStateStarted", 0, false);

            // === END NEW ADDITION ===
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (!this.parent.IsBrokenDown())
            {
                if (!this.DesirePowerOn)
                {
                    parent.Map.overlayDrawer.DrawOverlay(this.parent, OverlayTypes.PowerOff);
                }
                else if (!this.PowerOn)
                {
                    parent.Map.overlayDrawer.DrawOverlay(this.parent, OverlayTypes.NeedsPower);
                }
            }
        }

        public override void SetUpPowerVars()
        {
            base.SetUpPowerVars();
            CompProperties_Power props = base.Props;
            this.PowerOutput = -1f * props.basePowerConsumption;
            this.powerLastOutputted = (props.basePowerConsumption <= 0f);

            // === NEW ADDITION ===
            CompProperties_Power_EmergencyPowerCore props2 = base.Props as CompProperties_Power_EmergencyPowerCore;

            if (props == null)
                return;

            this.generatingTime = props2.baseGeneratingTime;
            this.cooldownTime = props2.baseCoolingTime;
            this.showEmergencyTimeInSeconds = props2.showEmergencyTimeInSeconds;
            this.explosionChance = props2.ExplosionChance;
            this.explosionRadius = props2.ExplosionRadius;
            this.explosionDamageDef = props2.ExplosionDamageDef;

            // === END NEW ADDITION ===
        }

        public override void ResetPowerVars()
        {
            base.ResetPowerVars();
            this.powerOnInt = false;
            this.powerOutputInt = 0f;
            this.powerLastOutputted = false;
            this.sustainerPowered = null;
            if (this.flickableComp != null)
            {
                this.flickableComp.ResetToOn();
                // === CHANGE ===
                this.flickableComp.SwitchIsOn = false;
                // === END CHANGE ===
            }
        }

        public override void LostConnectParent()
        {
            base.LostConnectParent();
            this.PowerOn = false;
        }

        public override string CompInspectStringExtra()
        {
            string str;
            if (this.powerLastOutputted)
            {
                str = "PowerOutput".Translate() + ": " + this.PowerOutput.ToString("#####0") + " W";
            }
            else
            {
                str = "PowerNeeded".Translate() + ": " + (-this.PowerOutput).ToString("#####0") + " W";
            }


            // === NEW ADDITION ===

            switch (activeStage)
            {
                case stage.off:
                    str = str + "\n" + "EmergencyPowerCore_Off".Translate();
                    break;

                case stage.cooling:

                    int remainingTime1 = RemainingTicksInStage(cooldownTime);

                    str = str + "\n" + "EmergencyPowerCore_Cooling".Translate(GetRemainingStageTimeInSeconds());
                    break;

                case stage.generating:

                    str = str + "\n" + "EmergencyPowerCore_Generating".Translate() + "\n";

                    int remainingTime2 = RemainingTicksInStage(generatingTime);

                    if (!showEmergencyTimer)
                    {
                        str = str + "\n" + "EmergencyPowerCore_GeneratingRemainingTime".Translate(GetRemainingStageTimeInSeconds());
                    }
                    else
                    {
                        str = str + "\n" + "EmergencyPowerCore_GeneratingCoreCritical".Translate(GetRemainingStageTimeInSeconds());
                    }
                    break;
            }
            
            // === END NEW ADDITION ===

            return str + "\n" + base.CompInspectStringExtra();
        }


        private void StartSustainerPoweredIfInactive()
        {
            CompProperties_Power props = base.Props;
            if (!props.soundAmbientPowered.NullOrUndefined() && this.sustainerPowered == null)
            {
                SoundInfo info = SoundInfo.InMap(this.parent, MaintenanceType.None);
                this.sustainerPowered = props.soundAmbientPowered.TrySpawnSustainer(info);
            }
        }

        private void EndSustainerPoweredIfActive()
        {
            if (this.sustainerPowered != null)
            {
                this.sustainerPowered.End();
                this.sustainerPowered = null;
            }
        }


        // === NEW ADDITION ===
        private string GetRemainingStageTimeInSeconds()
        {
            int remainingTime2 = RemainingTicksInStage(generatingTime);
            return (remainingTime2 / GenTicks.TicksPerRealSecond).ToString() + "s";
        }
        // === END NEW ADDITION ===


    }

    // ============
    // End Original
    // ============

}

