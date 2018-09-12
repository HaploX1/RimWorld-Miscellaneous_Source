using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace AIPawn
{
    public class ThingDef_AIPawn : ThingDef
    {
        public string normalHeadGraphicPathMulti = null;
        public string draftedHeadGraphicPathMulti = null;
        public string normalBodyGraphicPathMulti = null;
        public string draftedBodyGraphicPathMulti = null;
        public int refreshBaseInfosMax = 0;
        public int refreshQuickMax = 0;
        public int incapToExplosionCounter = 0;

        public int passionLevel = 0;
        public int startingSkillLevel = 6;
        public bool enhancedAI = false;
    }
}
