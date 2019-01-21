using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;

namespace MapGenerator
{
    public class MapGenerator_Mod : Mod
    {
        public static string Text_Category = "MapGenerator_ModOptions_Category";

        public static string Text_Option_SpawnWithHoles = "MapGenerator_ModOptions_TryAlwaysSpawnWithHoles";
        public static string ToolTip_Option_SpawnWithHoles = "MapGenerator_ModOptions_TryAlwaysSpawnWithHoles_hint";

        public static string Text_Option_HoleChance = "MapGenerator_ModOptions_HoleChancePercent";
        public static string Text_Option_HoleChanceOnWater = "MapGenerator_ModOptions_HoleChanceOnWaterPercent";

        public static string Text_Option_ForUrbanCityOnly = "MapGenerator_ModOptions_UrbanCity_OnlyForUrbanCityExtension";
        public static string Tooltip_Option_ForUrbanCityOnly = "MapGenerator_ModOptions_UrbanCity_OnlyForUrbanCityExtension_hint";
        public static string Text_Option_UrbanCityChanceMultiplier = "MapGenerator_ModOptions_UrbanCity_SpawnChanceMultiplier";
        public static string Tooltip_Option_UrbanCityChanceMultiplier = "MapGenerator_ModOptions_UrbanCity_SpawnChanceMultiplier_hint";

        public MapGenerator_Mod(ModContentPack mcp) : base(mcp) {
            LongEventHandler.ExecuteWhenFinished(SetTexts);
            LongEventHandler.ExecuteWhenFinished(GetSettings);
        }

        public void SetTexts()
        {
            Text_Category = Text_Category.Translate();

            Text_Option_SpawnWithHoles = Text_Option_SpawnWithHoles.Translate();
            ToolTip_Option_SpawnWithHoles = ToolTip_Option_SpawnWithHoles.Translate();

            Text_Option_HoleChance = Text_Option_HoleChance.Translate();
            Text_Option_HoleChanceOnWater = Text_Option_HoleChanceOnWater.Translate();

            Text_Option_ForUrbanCityOnly = Text_Option_ForUrbanCityOnly.Translate();
            Tooltip_Option_ForUrbanCityOnly = Tooltip_Option_ForUrbanCityOnly.Translate();
            Text_Option_UrbanCityChanceMultiplier = Text_Option_UrbanCityChanceMultiplier.Translate();
            Tooltip_Option_UrbanCityChanceMultiplier = Tooltip_Option_UrbanCityChanceMultiplier.Translate();

        }
        public void GetSettings()
        {
            GetSettings<MapGenerator_ModSettings>();
        }
        public override string SettingsCategory()
        {
            return Text_Category;
        }
        
        public override void DoSettingsWindowContents(Rect rect)
        {

            Rect rectLH = rect.LeftHalf().Rounded(); 
            Rect rectRH = rect.RightHalf().Rounded();

            //Rect rectLH =  new Rect(rect.x, rect.y, rect.width / 2 - 5, rect.height);
            //Rect rectRH = new Rect(rect.x + rect.width / 2 + 5, rect.y, rect.width / 2 - 5, rect.height);

            //Rect rectLH = new Rect(rect.x, rect.y, rect.width / 2, rect.height).Rounded();
            //Rect rectRH = new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, rect.height).Rounded();

            Listing_Standard optionsLH = new Listing_Standard();
            Listing_Standard optionsRH = new Listing_Standard();
            optionsLH.Begin(rectLH);

            //options.CheckboxLabeled(Text_Option1, ref MapGenerator_ModSettings.chanceForHoles, ToolTip_Option1);

            optionsLH.CheckboxLabeled(Text_Option_SpawnWithHoles, ref MapGenerator_ModSettings.createAllNonPawnBPsWithHoles, ToolTip_Option_SpawnWithHoles);
            optionsLH.Gap();
            optionsLH.Label(Text_Option_HoleChance + "  " + (MapGenerator_ModSettings.chanceForHoles).ToStringPercent());
            optionsLH.Label(Text_Option_HoleChanceOnWater + "  " + (MapGenerator_ModSettings.chanceForHolesOnWater).ToStringPercent());
            optionsLH.GapLine();
            optionsLH.Gap();
            optionsLH.Label(Text_Option_ForUrbanCityOnly, -1, Tooltip_Option_ForUrbanCityOnly);
            optionsLH.Label(Text_Option_UrbanCityChanceMultiplier + "  " + (MapGenerator_ModSettings.chance4UrbanCitiesMultiplier).ToString(), -1, Tooltip_Option_UrbanCityChanceMultiplier);


            optionsLH.End();
            //mcp.GetDefPackagesInFolder("ThingDefs").First().RemoveDef();

            optionsRH.Begin(rectRH);
            optionsRH.Gap();
            optionsRH.Gap();
            optionsRH.Gap();
            MapGenerator_ModSettings.chanceForHoles = optionsRH.Slider(MapGenerator_ModSettings.chanceForHoles, 0.05f, 0.90f);
            MapGenerator_ModSettings.chanceForHolesOnWater = optionsRH.Slider(MapGenerator_ModSettings.chanceForHolesOnWater, 0.05f, 0.95f);
            optionsRH.GapLine();
            optionsRH.Gap();
            optionsRH.Gap();
            optionsRH.Gap();
            optionsRH.Gap(6);
            MapGenerator_ModSettings.chance4UrbanCitiesMultiplier = optionsRH.Slider(MapGenerator_ModSettings.chance4UrbanCitiesMultiplier, 0.0f, 20.0f);

            optionsRH.End();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
        }
        
    }
}
