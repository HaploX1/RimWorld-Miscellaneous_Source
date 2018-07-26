using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;

namespace ColonistSelections
{
    public class Mod_ColonistSelections : Mod
    {
        public static string Text_Category = "ColonistSelections_ModOptions_Category";
        public static string Text_Option_ShowGroupIcons = "ColonistSelections_ModOptions_ShowGroupIcons";
        public static string ToolTip_Option_ShowGroupIcons = "ColonistSelections_ModOptions_ShowGroupIcons_ToolTip";
        public static string Text_Option_SizeGroupIconsPercent = "ColonistSelections_ModOptions_SizeGroupIconsPercent";
        public static string Text_Option_StartPosGroupIconsX = "ColonistSelections_ModOptions_StartPosGroupIconsX";
        public static string Text_Option_StartPosGroupIconsY = "ColonistSelections_ModOptions_StartPosGroupIconsY";

        public Mod_ColonistSelections(ModContentPack mcp) : base(mcp) {
            LongEventHandler.ExecuteWhenFinished(SetTexts);
            LongEventHandler.ExecuteWhenFinished(GetSettings);
        }

        public void SetTexts()
        {
            Text_Category = Text_Category.Translate();

            Text_Option_ShowGroupIcons = Text_Option_ShowGroupIcons.Translate();
            ToolTip_Option_ShowGroupIcons = ToolTip_Option_ShowGroupIcons.Translate();
            Text_Option_SizeGroupIconsPercent = Text_Option_SizeGroupIconsPercent.Translate();
            Text_Option_StartPosGroupIconsX = Text_Option_StartPosGroupIconsX.Translate();
            Text_Option_StartPosGroupIconsY = Text_Option_StartPosGroupIconsY.Translate();

        }
        public void GetSettings()
        {
            GetSettings<ModSettings_ColonistSelections>();
        }
        public override string SettingsCategory()
        {
            return Text_Category;
        }
        
        public override void DoSettingsWindowContents(Rect rect)
        {

            Rect rectLH = rect.LeftHalf().Rounded(); 
            Rect rectRH = rect.RightHalf().Rounded();

            Listing_Standard optionsLH = new Listing_Standard();
            Listing_Standard optionsRH = new Listing_Standard();

            optionsLH.Begin(rectLH);
      

            optionsLH.CheckboxLabeled(Text_Option_ShowGroupIcons, ref ModSettings_ColonistSelections.showModIcons, ToolTip_Option_ShowGroupIcons);
            optionsLH.GapLine();
            optionsLH.Gap(12);
            optionsLH.Label(Text_Option_SizeGroupIconsPercent + "  " + (ModSettings_ColonistSelections.sizeGroupIconsPercent).ToStringPercent());
            optionsLH.Label(Text_Option_StartPosGroupIconsX + "  " + (ModSettings_ColonistSelections.startPosGroupIconsPercentX).ToString());
            optionsLH.Label(Text_Option_StartPosGroupIconsY + "  " + (ModSettings_ColonistSelections.startPosGroupIconsPercentY).ToString());
            //optionsLH.GapLine();


            optionsLH.End();
            //mcp.GetDefPackagesInFolder("ThingDefs").First().RemoveDef();

            optionsRH.Begin(rectRH);
            optionsRH.Gap();
            optionsRH.Gap();
            optionsRH.GapLine();
            optionsRH.Gap(18);
            ModSettings_ColonistSelections.sizeGroupIconsPercent = optionsRH.Slider(ModSettings_ColonistSelections.sizeGroupIconsPercent, 0.001f, 0.1f);
            ModSettings_ColonistSelections.startPosGroupIconsPercentX = optionsRH.Slider(ModSettings_ColonistSelections.startPosGroupIconsPercentX, 0.003f, 0.97f);
            ModSettings_ColonistSelections.startPosGroupIconsPercentY = optionsRH.Slider(ModSettings_ColonistSelections.startPosGroupIconsPercentY, 0.003f, 0.97f);
            //optionsRH.GapLine();

            optionsRH.End();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
        }
        
    }
}
