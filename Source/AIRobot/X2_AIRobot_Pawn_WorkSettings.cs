using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;

namespace AIRobot
{

    // This is done to simulate a working worksettings part for the Robots. 
    // It has no usage other than prevent some errors..
    public class X2_AIRobot_Pawn_WorkSettings : Pawn_WorkSettings
    {
        Pawn pawn2;

        // For Reflection needed.
        private static readonly System.Reflection.BindingFlags BindFlags = System.Reflection.BindingFlags.Instance
                               | System.Reflection.BindingFlags.Public
                               | System.Reflection.BindingFlags.NonPublic
                               | System.Reflection.BindingFlags.Static;

        private DefMap<WorkTypeDef, int> prioritiesReflected
        {
            set
            {
                // With this Reflection you can access a private variable! Here: "priorities" is set 
                System.Reflection.FieldInfo fi = typeof(Pawn_WorkSettings).GetField("priorities", BindFlags);
                fi.SetValue(this, value);

                // Also set the local variable..
                this.priorities = value;
            }
            get
            {
                // With this Reflection you can access a private variable! Here: "priorities" is set 
                System.Reflection.FieldInfo fi = typeof(Pawn_WorkSettings).GetField("priorities", BindFlags);
                return fi == null ? null : fi.GetValue(this) as DefMap<WorkTypeDef, int>;
            }
        }
        private DefMap<WorkTypeDef, int> priorities;

        public X2_AIRobot_Pawn_WorkSettings()
        {
        }
        public X2_AIRobot_Pawn_WorkSettings(Pawn pawn) : base(pawn) 
        {
            this.pawn2 = pawn;
            EnableAndInitialize();
        }

        public new void EnableAndInitialize()
        {
            if (this.prioritiesReflected == null)
            {
                this.prioritiesReflected = new DefMap<WorkTypeDef, int>();
            }
            this.prioritiesReflected.SetAll(0);

            

            //int num = 0;
            foreach (WorkTypeDef current in from w in DefDatabase<WorkTypeDef>.AllDefs
                                            //where !w.alwaysStartActive && !this.pawn.story.WorkTypeIsDisabled(w)
                                            //orderby this.pawn2.skills.AverageOfRelevantSkillsFor(w) descending
                                            select w)
            {
                //bool found = false;
                //foreach (X2_ThingDef_AIRobot.RobotWorkTypes rwtdef in (this.pawn2.def as X2_ThingDef_AIRobot).robotWorkTypes)
                //{
                //    if (rwtdef.workTypeDef == current) 
                //    {
                //        found = true;
                //        break;
                //    }
                //}
                //if (found)
                //    this.SetPriority(current, 3);
                //else
                //    this.SetPriority(current, 0);


                this.SetPriority(current, 3);

                //num++;
                //if (num >= 6)
                //{
                //    break;
                //}
            }
            //foreach (WorkTypeDef current3 in this.pawn.story.DisabledWorkTypes)
            //{
            //    this.Disable(current3);
            //}
        }

        public new void SetPriority(WorkTypeDef w, int priority)
        {
            //this.ConfirmInitializedDebug();
            //if (priority != 0 && this.pawn.story.WorkTypeIsDisabled(w))
            //{
            //    Log.Error(string.Concat(new object[]
            //    {
            //        "Tried to change priority on disabled worktype ",
            //        w,
            //        " for pawn ",
            //        this.pawn2
            //    }));
            //    return;
            //}
            //if (priority < 0 || priority > 4)
            //{
            //    Log.Message("Trying to set work to invalid priority " + priority);
            //}

            //Log.Message("PRE - priority:" + priority.ToString() + ", reflected:" + this.prioritiesReflected[w] + ", ");

            this.prioritiesReflected[w] = priority;

            //Log.Message("POST - priority:" + priority.ToString() + ", reflected:" + this.prioritiesReflected[w] + ", ");


            Log.Message( w.defName + ":" + this.WorkIsActive(w).ToString() );
            //if (priority == 0)
            //{
            //    this.pawn2.mindState.Notify_WorkPriorityDisabled(w);
            //}
            //this.workGiversDirty = true;
        }

    }
}
