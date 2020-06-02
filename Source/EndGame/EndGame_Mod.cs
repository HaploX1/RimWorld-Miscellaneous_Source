using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;

namespace EndGame
{
    public class EndGame_Mod : Mod
    {
        public static string Text_Category = "EndGame_ModOptions_Category";

        public static string Text_Option_DaysActive =    "EndGame_ModOptions_DaysActive"; 
        public static string ToolTip_Option_DaysActive = "EndGame_ModOptions_DaysActive_hint";

        public static string Text_Option_TicksBetweenIncidents =    "EndGame_ModOptions_TicksBetweenIncidents";
        public static string ToolTip_Option_TicksBetweenIncidents = "EndGame_ModOptions_TicksBetweenIncidents_hint";

        public static string Text_Option_RaidPointsFactorRangePercent =    "EndGame_ModOptions_RaidPointsFactorRangePercent";
        public static string ToolTip_Option_RaidPointsFactorRangePercent = "EndGame_ModOptions_RaidPointsFactorRangePercent_hint";

        public static string Text_Option_DangerLowUntilDay =   "EndGame_ModOptions_DangerLowUntilDay";
        public static string Text_Option_DangerHighFromDay =   "EndGame_ModOptions_DangerHighFromDay";
        public static string ToolTip_Option_DangerLowHighDay = "EndGame_ModOptions_DangerLowHighDay_hint";

        public static string Text_Option_DangerLowTimeFactor =  "EndGame_ModOptions_DangerLowTimeFactor";
        public static string Text_Option_DangerMidTimeFactor =  "EndGame_ModOptions_DangerMidTimeFactor";
        public static string Text_Option_DangerHighTimeFactor = "EndGame_ModOptions_DangerHighTimeFactor";
        public static string ToolTip_Option_DangerTimeFactor =  "EndGame_ModOptions_DangerTimeFactor_hint";

        public static string Text_Option_DangerLowRaidPointFactor = "EndGame_ModOptions_DangerLowRaidPointFactor";
        public static string Text_Option_DangerMidRaidPointFactor = "EndGame_ModOptions_DangerMidRaidPointFactor";
        public static string Text_Option_DangerHighRaidPointFactor = "EndGame_ModOptions_DangerHighRaidPointFactor";
        public static string ToolTip_Option_DangerRaidPointFactor = "EndGame_ModOptions_DangerRaidPointFactor_hint";

        public EndGame_Mod(ModContentPack mcp) : base(mcp) {
            LongEventHandler.ExecuteWhenFinished(SetTexts);
            LongEventHandler.ExecuteWhenFinished(GetSettings);
        }

        public void SetTexts()
        {
            Text_Category = Text_Category.Translate();

            Text_Option_DaysActive = Text_Option_DaysActive.Translate();
            ToolTip_Option_DaysActive = ToolTip_Option_DaysActive.Translate();

            Text_Option_TicksBetweenIncidents = Text_Option_TicksBetweenIncidents.Translate();
            ToolTip_Option_TicksBetweenIncidents = ToolTip_Option_TicksBetweenIncidents.Translate(GenDate.TicksPerHour, GenDate.TicksPerDay);

            Text_Option_RaidPointsFactorRangePercent = Text_Option_RaidPointsFactorRangePercent.Translate();
            ToolTip_Option_RaidPointsFactorRangePercent = ToolTip_Option_RaidPointsFactorRangePercent.Translate(GenDate.TicksPerHour, GenDate.TicksPerDay);

            Text_Option_DangerLowUntilDay = Text_Option_DangerLowUntilDay.Translate();
            Text_Option_DangerHighFromDay = Text_Option_DangerHighFromDay.Translate();
            ToolTip_Option_DangerLowHighDay = ToolTip_Option_DangerLowHighDay.Translate();

            Text_Option_DangerLowTimeFactor = Text_Option_DangerLowTimeFactor.Translate();
            Text_Option_DangerMidTimeFactor = Text_Option_DangerMidTimeFactor.Translate();
            Text_Option_DangerHighTimeFactor = Text_Option_DangerHighTimeFactor.Translate();
            ToolTip_Option_DangerTimeFactor = ToolTip_Option_DangerTimeFactor.Translate();

            Text_Option_DangerLowRaidPointFactor = Text_Option_DangerLowRaidPointFactor.Translate();
            Text_Option_DangerMidRaidPointFactor = Text_Option_DangerMidRaidPointFactor.Translate();
            Text_Option_DangerHighRaidPointFactor = Text_Option_DangerHighRaidPointFactor.Translate();
            ToolTip_Option_DangerRaidPointFactor = ToolTip_Option_DangerRaidPointFactor.Translate();

        }
        public void GetSettings()
        {
            GetSettings<EndGame_ModSettings>();
        }
        public override string SettingsCategory()
        {
            return Text_Category;
        }

        private string bufferDaysActive = null;
        private string bufferDangerLowUntilDay = null;
        private string bufferDangerHighFromDay = null;
        private string bufferDangerLowTimeFactor = null;
        private string bufferDangerMidTimeFactor = null;
        private string bufferDangerHighTimeFactor = null;
        private string bufferDangerLowRaidPointFactor = null;
        private string bufferDangerMidRaidPointFactor = null;
        private string bufferDangerHighRaidPointFactor = null;
        public override void DoSettingsWindowContents(Rect rect)
        {

            Rect rectLH = rect.LeftHalf().Rounded(); 
            Rect rectRH = rect.RightHalf().Rounded();

            Listing_Standard optionsLH = new Listing_Standard();
            Listing_Standard optionsRH = new Listing_Standard();

            optionsLH.Begin(rectLH);

            optionsLH.Gap(24);
            optionsLH.Label(Text_Option_DaysActive, -1, ToolTip_Option_DaysActive);
            optionsLH.Gap(12);
            optionsLH.GapLine(12);
            optionsLH.Gap(24);
            optionsLH.Label(Text_Option_TicksBetweenIncidents, -1, ToolTip_Option_TicksBetweenIncidents);
            optionsLH.Gap(12);
            optionsLH.Label(Text_Option_RaidPointsFactorRangePercent, -1, ToolTip_Option_RaidPointsFactorRangePercent);
            optionsLH.Gap(24);
            optionsLH.Label(Text_Option_DangerLowUntilDay, -1, ToolTip_Option_DangerLowHighDay);
            optionsLH.Gap(12);
            optionsLH.Label(Text_Option_DangerHighFromDay, -1, ToolTip_Option_DangerLowHighDay);
            optionsLH.Gap(24);
            optionsLH.Label(Text_Option_DangerLowTimeFactor, -1, ToolTip_Option_DangerTimeFactor);
            optionsLH.Gap(12);
            optionsLH.Label(Text_Option_DangerMidTimeFactor, -1, ToolTip_Option_DangerTimeFactor);
            optionsLH.Gap(12);
            optionsLH.Label(Text_Option_DangerHighTimeFactor, -1, ToolTip_Option_DangerTimeFactor);
            optionsLH.Gap(24);
            optionsLH.Label(Text_Option_DangerLowRaidPointFactor, -1, ToolTip_Option_DangerRaidPointFactor);
            optionsLH.Gap(12);
            optionsLH.Label(Text_Option_DangerMidRaidPointFactor, -1, ToolTip_Option_DangerRaidPointFactor);
            optionsLH.Gap(12);
            optionsLH.Label(Text_Option_DangerHighRaidPointFactor, -1, ToolTip_Option_DangerRaidPointFactor);
            optionsLH.Gap(24);


            //optionsLH.Label(Text_Option_HoleChanceOnWater + "  " + (MapGenerator_ModSettings.chanceForHolesOnWater).ToStringPercent());
            //optionsLH.GapLine();
            //optionsLH.Gap();
            //optionsLH.Label(Text_Option_ForUrbanCityOnly, -1, Tooltip_Option_ForUrbanCityOnly);
            //optionsLH.Label(Text_Option_UrbanCityChanceMultiplier + "  " + (MapGenerator_ModSettings.chance4UrbanCitiesMultiplier).ToString(), -1, Tooltip_Option_UrbanCityChanceMultiplier);


            optionsLH.End();


            optionsRH.Begin(rectRH);

            optionsRH.Gap(24);
            optionsRH.TextFieldNumeric(ref EndGame_ModSettings.maxDaysActive, ref bufferDaysActive, 10f, 240f);
            optionsRH.Gap(12);
            optionsRH.GapLine(12);
            optionsRH.Gap(12);
            optionsRH.IntRange(ref EndGame_ModSettings.ticksBetweenIncidents, 20000, 300000);
            optionsRH.Gap(6);
            optionsRH.IntRange(ref EndGame_ModSettings.raidPointsFactorRangePercent, 10, 1000);
            optionsRH.Gap(30);
            optionsRH.TextFieldNumeric<int>(ref EndGame_ModSettings.dangerLowUntilDay, ref bufferDangerLowUntilDay, 0, 15);
            optionsRH.Gap(12);
            optionsRH.TextFieldNumeric<int>(ref EndGame_ModSettings.dangerHighFromDay, ref bufferDangerHighFromDay, EndGame_ModSettings.dangerLowUntilDay, EndGame_ModSettings.maxDaysActive);
            optionsRH.Gap(24);
            optionsRH.TextFieldNumeric<float>(ref EndGame_ModSettings.dangerLowTimeFactor, ref bufferDangerLowTimeFactor, 0.25f, 5.0f);
            optionsRH.Gap(12);
            optionsRH.TextFieldNumeric<float>(ref EndGame_ModSettings.dangerMidTimeFactor, ref bufferDangerMidTimeFactor, 0.25f, 5.0f);
            optionsRH.Gap(12);
            optionsRH.TextFieldNumeric<float>(ref EndGame_ModSettings.dangerHighTimeFactor, ref bufferDangerHighTimeFactor, 0.25f, 5.0f);
            optionsRH.Gap(24);
            optionsRH.TextFieldNumeric<float>(ref EndGame_ModSettings.dangerLowRaidPointFactor, ref bufferDangerLowRaidPointFactor, 0.25f, 5.0f);
            optionsRH.Gap(12);
            optionsRH.TextFieldNumeric<float>(ref EndGame_ModSettings.dangerMidRaidPointFactor, ref bufferDangerMidRaidPointFactor, 0.25f, 5.0f);
            optionsRH.Gap(12);
            optionsRH.TextFieldNumeric<float>(ref EndGame_ModSettings.dangerHighRaidPointFactor, ref bufferDangerHighRaidPointFactor, 0.25f, 5.0f);
            optionsRH.Gap(24);


            //MapGenerator_ModSettings.chanceForHolesOnWater = optionsRH.Slider(MapGenerator_ModSettings.chanceForHolesOnWater, 0.05f, 0.95f);
            //optionsRH.GapLine();
            //optionsRH.Gap();
            //optionsRH.Gap();
            //optionsRH.Gap();
            //optionsRH.Gap(6);
            //MapGenerator_ModSettings.chance4UrbanCitiesMultiplier = optionsRH.Slider(MapGenerator_ModSettings.chance4UrbanCitiesMultiplier, 0.0f, 20.0f);

            optionsRH.End();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
        }
        
    }
}
