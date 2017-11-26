using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace AIRobot
{
    public class X2_ThingDef_AIRobot : ThingDef
    {
        public ThingDef destroyedDef = null;

        public bool allowLearning = false;

        public class RobotSkills
        {
            public SkillDef skillDef;
            public int level = 0;
            public Passion passion = Passion.None;
        }
        public class RobotWorkTypes
        {
            public WorkTypeDef workTypeDef;
            public int priority = 1;
        }

        public List<RobotSkills> robotSkills = new List<RobotSkills>();
        public List<RobotWorkTypes> robotWorkTypes = new List<RobotWorkTypes>();

        WorkTags robotWorkTagsInt = WorkTags.None;
        public WorkTags robotWorkTags
        {
            get
            {
                if (robotWorkTagsInt == WorkTags.None && robotWorkTypes.Count > 0)
                    InitWorkTagsFromWorkTypes();

                return robotWorkTagsInt;
            }
            set
            {
                robotWorkTagsInt = value;
            }
        }




        #region functions

        private WorkTags InitWorkTagsFromWorkTypes()
        {
            WorkTags workTags = WorkTags.None;
            foreach (RobotWorkTypes workTypes in this.robotWorkTypes)
                workTags = workTags | workTypes.workTypeDef.workTags;

            return workTags;
        }

        #endregion
    }
}
