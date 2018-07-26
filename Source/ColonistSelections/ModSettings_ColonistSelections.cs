using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;

namespace ColonistSelections
{
    public class ModSettings_ColonistSelections : ModSettings
    {
        public static bool showModIcons = true;
        public static bool showGroupIcons = false;

        public static float sizeGroupIconsPercent = 0.016f;

        public static float startPosGroupIconsPercentX = 0.8246f;
        public static float startPosGroupIconsPercentY = 0.9300f;

        public static int buttonSpacing = 5;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref showModIcons, "showModIcons", true);
            Scribe_Values.Look<bool>(ref showGroupIcons, "showGroupIcons", false);
            Scribe_Values.Look<float>(ref sizeGroupIconsPercent, "sizeGroupIconsPercent", 0.015f);
            Scribe_Values.Look<float>(ref startPosGroupIconsPercentX, "startPosGroupIconsPercentX", 0.8177f);
            Scribe_Values.Look<float>(ref startPosGroupIconsPercentY, "startPosGroupIconsPercentY", 0.9346f);
            Scribe_Values.Look<int>(ref buttonSpacing, "buttonSpacing", 5);
        }
        
    }
}
