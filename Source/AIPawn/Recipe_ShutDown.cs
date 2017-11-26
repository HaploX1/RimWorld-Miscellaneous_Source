using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace AIPawn
{
    public class Recipe_ShutDownAIPawn : RecipeWorker
    {

        public override void ApplyOnPawn(Pawn pawn, RecipeDef recipe, BodyPartRecord part, Pawn billDoer)
        {
            Disease disease = new Disease()
            {
                Part = part,
                def = recipe.addsHealthDiff
            };
            pawn.healthTracker.AddHealthDiff(disease, null);
        }

        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            foreach(BodyPartRecord bodyPart in pawn.healthTracker.bodyModel.GetNotMissingParts(null, null))
            {
                IEnumerable<Pair<PawnActivityDef, string>> activities = bodyPart.def.Activities;
                foreach (Pair<PawnActivityDef, string> activity in activities)
                {
                    if (activity.First != PawnActivityDefOf.Consciousness)
                        continue;

                    Log.Error("Found Part: " + bodyPart.def.defName);
                    yield return bodyPart;
                    break;
                }
            }
        }

    }
}
