using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using RimWorld;


namespace PawnCreator
{
    public class PawnCreator : Thing
    {
        private bool destroy = false;

        public override void Tick()
        {
            if (destroy)
            {
                Destroy(DestroyMode.Vanish);
                return;
            }

            try
            {
                ThingDef_PawnCreator def2 = def as ThingDef_PawnCreator;
                Pawn pawn = SpawnPawn(def2.pawnKindDef, def2.factionDef, this.Position, this.Map);

                if (!def2.letterLabel.NullOrEmpty() && !def2.letterText.NullOrEmpty())
                    DoLetter(def2.letterLabel, def2.letterText, def2.letterDef, pawn);
            }
            catch (Exception ex)
            {
                Log.Error("Error while creating the pawn.\n" + ex.Message + "\nSTACK:\n" + ex.StackTrace);
            }
            destroy = true;
        }


        private Pawn SpawnPawn(PawnKindDef kindDef, FactionDef factionDef, IntVec3 cell, Map map)
        {
            // Generate the pawn
            Pawn pawn = null;
            if (factionDef == null)
                pawn = PawnGenerator.GeneratePawn(kindDef, this.Faction);
            else
            {
                Faction faction;
                if (!Find.FactionManager.AllFactions.Where(f => f.def == factionDef).TryRandomElement( out faction ) )
                {
                    Log.Error("Error spawning pawn. Could not find faction with factionDef:" + factionDef.defName);
                    return null;
                }
                
                pawn = PawnGenerator.GeneratePawn(kindDef, faction);
            }

            // Spawn the pawn
            //GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(Position, Map, 1, null), Map);

            // Spawn the pawn
            GenPlace.TryPlaceThing(pawn, base.Position, base.Map, ThingPlaceMode.Near, null);

            return pawn;
        }

        public static void DoLetter(string letterLabel, string letterText, LetterDef letterDef, Pawn pawn)
        {
            Letter letter = LetterMaker.MakeLetter(letterLabel, letterText.AdjustedFor(pawn), letterDef, pawn);
            Find.LetterStack.ReceiveLetter(letter);
        }
    }
}
