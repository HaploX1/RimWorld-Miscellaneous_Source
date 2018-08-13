using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using RimWorld;

namespace PawnCreator
{
    public class ThingDef_PawnCreator : ThingDef
    {
        public PawnKindDef pawnKindDef = null;
        public FactionDef factionDef = null;

        public LetterDef letterDef = LetterDefOf.PositiveEvent;
        public string letterLabel = null;
        public string letterText = null;
    }
}
