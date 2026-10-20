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
        // RimWorld 1.6: body and head graphics are def driven. The head is applied through
        // story.headType, the body through the render node of the MAI_Humanlike render tree.
        // See AIPawn.UpdateGraphics().
        public string normalHeadDefName = "MAI_Head_Normal";
        public string draftedHeadDefName = "MAI_Head_Drafted";
        // The body graphic is supplied by PawnRenderNode_MAIBody (see the MAI_Humanlike render
        // tree); the pawn itself keeps a vanilla body type, because RimWorld resolves apparel
        // graphics as wornGraphicPath + "_" + bodyType.defName and has no fallback for mod
        // defined body types. See AIPawn.UpdateGraphics().
        public string normalBodyGraphicPath = "Things/Pawns/Naked/Naked_Mia";
        public string draftedBodyGraphicPath = "Things/Pawns/Drafted/Drafted_Mia";
        public int refreshBaseInfosMax = 0;
        public int refreshQuickMax = 0;
        public int incapToExplosionCounter = 0;

        public int passionLevel = 0;
        public int startingSkillLevel = 6;
        public bool enhancedAI = false;
    }
}
