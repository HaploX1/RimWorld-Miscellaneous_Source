using RimWorld;
using UnityEngine;
using Verse;

namespace AIPawn
{
    /// <summary>
    /// Body render node of the AI pawns (RimWorld 1.6).
    /// </summary>
    /// <remarks>
    /// RimWorld 1.6 draws a humanlike body from <c>Pawn_StoryTracker.bodyType</c> and resolves apparel
    /// graphics as <c>wornGraphicPath + "_" + bodyType.defName</c>, without any fallback. A body type
    /// defined by this mod would therefore need its own texture set for every apparel item in the game,
    /// so the AI pawns keep a vanilla body type and this node supplies the mod's own body graphic
    /// instead. It is only used by the MAI_Humanlike render tree, so no other pawn is affected.
    ///
    /// Everything except the texture path behaves like <c>Verse.PawnRenderNode_Body</c>: colour, shader
    /// and the vanilla special cases (dessicated corpse, mutant, creep joiner) are unchanged. That logic
    /// is reproduced here because <c>PawnRenderNode_Body</c> is internal and cannot be subclassed.
    /// </remarks>
    public class PawnRenderNode_MAIBody : PawnRenderNode
    {
        public PawnRenderNode_MAIBody(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
        }

        public override Graphic GraphicFor(Pawn pawn)
        {
            Shader shader = ShaderFor(pawn);
            if (shader == null)
            {
                return null;
            }

            // Dessicated corpses keep the graphic of the pawn's body type, exactly like the vanilla node.
            string dessicatedPath = pawn.story == null || pawn.story.bodyType == null
                ? null
                : pawn.story.bodyType.bodyDessicatedGraphicPath;
            if (pawn.Drawer.renderer.CurRotDrawMode == RotDrawMode.Dessicated && !dessicatedPath.NullOrEmpty())
            {
                return GraphicDatabase.Get<Graphic_Multi>(dessicatedPath, shader);
            }

            if (pawn.IsMutant && !pawn.mutant.Def.bodyTypeGraphicPaths.NullOrEmpty())
            {
                string mutantPath = pawn.mutant.Def.GetBodyGraphicPath(pawn);
                if (mutantPath != null)
                {
                    return GraphicDatabase.Get<Graphic_Multi>(mutantPath, shader, Vector2.one, ColorFor(pawn));
                }
            }

            if (ModsConfig.AnomalyActive && pawn.IsCreepJoiner && pawn.story != null && pawn.story.bodyType != null
                && !pawn.creepjoiner.form.bodyTypeGraphicPaths.NullOrEmpty())
            {
                return GraphicDatabase.Get<Graphic_Multi>(pawn.creepjoiner.form.GetBodyGraphicPath(pawn), shader, Vector2.one, ColorFor(pawn));
            }

            string path = BodyGraphicPathFor(pawn);
            if (path.NullOrEmpty())
            {
                return null;
            }
            return GraphicDatabase.Get<Graphic_Multi>(path, shader, Vector2.one, ColorFor(pawn));
        }

        /// <summary>
        /// Body texture of the pawn for its current mode. Only AI pawns take their graphic from the mod's
        /// def - every other pawn keeps the vanilla behaviour (the graphic of its body type).
        /// </summary>
        private static string BodyGraphicPathFor(Pawn pawn)
        {
            ThingDef_AIPawn aiDef = pawn.def as ThingDef_AIPawn;
            if (aiDef != null)
            {
                string aiPath = pawn.Drafted ? aiDef.draftedBodyGraphicPath : aiDef.normalBodyGraphicPath;
                if (!aiPath.NullOrEmpty())
                {
                    return aiPath;
                }
            }
            return pawn.story == null || pawn.story.bodyType == null ? null : pawn.story.bodyType.bodyNakedGraphicPath;
        }
    }
}
