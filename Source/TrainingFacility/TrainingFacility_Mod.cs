using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;

namespace TrainingFacility
{
    public class TrainingFacility_Mod : Mod
    {
        public static string Text_Category = "TrainingFacility_ModOptions_Category";

        public static string Text_Option_Shooting_LearningXpMultiplier = "Text_Option_Shooting_LearningXpMultiplier";
        public static string Tooltip_Option_Shooting_LearningXpMultiplier = "Text_Option_Shooting_LearningXpMultiplier_hint";

        public static string Text_Option_Melee_LearningXpMultiplier = "Text_Option_Melee_LearningXpMultiplier";
        public static string Tooltip_Option_Melee_LearningXpMultiplier = "Text_Option_Melee_LearningXpMultiplier_hint";


        public TrainingFacility_Mod(ModContentPack mcp) : base(mcp) {
            LongEventHandler.ExecuteWhenFinished(SetTexts);
            LongEventHandler.ExecuteWhenFinished(GetSettings);
        }

        public void SetTexts()
        {
            Text_Category = Text_Category.Translate();

            Text_Option_Shooting_LearningXpMultiplier = Text_Option_Shooting_LearningXpMultiplier.Translate();
            Tooltip_Option_Shooting_LearningXpMultiplier = Tooltip_Option_Shooting_LearningXpMultiplier.Translate();

            Text_Option_Melee_LearningXpMultiplier = Text_Option_Melee_LearningXpMultiplier.Translate();
            Tooltip_Option_Melee_LearningXpMultiplier = Tooltip_Option_Melee_LearningXpMultiplier.Translate();

        }
        public void GetSettings()
        {
            GetSettings<TrainingFacility_ModSettings>();
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

            optionsLH.Gap(24);
            optionsLH.Label(Text_Option_Shooting_LearningXpMultiplier, -1, tooltip: Tooltip_Option_Shooting_LearningXpMultiplier);
            optionsLH.Gap(8);
            TrainingFacility_ModSettings.learningXpMultiplierShooting = optionsLH.SliderLabeled(TrainingFacility_ModSettings.learningXpMultiplierShooting.ToString(), 
                                                                                            TrainingFacility_ModSettings.learningXpMultiplierShooting, 0f, 10f);

            optionsLH.Gap(24);
            optionsLH.Label(Text_Option_Melee_LearningXpMultiplier, -1, tooltip: Tooltip_Option_Melee_LearningXpMultiplier);
            optionsLH.Gap(8);
            TrainingFacility_ModSettings.learningXpMultiplierMelee = optionsLH.SliderLabeled(TrainingFacility_ModSettings.learningXpMultiplierMelee.ToString(), 
                                                                                            TrainingFacility_ModSettings.learningXpMultiplierMelee, 0f, 10f);

            optionsLH.End();


            optionsRH.Begin(rectRH);

            optionsRH.End();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
        }
        
    }
}
