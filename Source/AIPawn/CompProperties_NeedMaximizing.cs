using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed


namespace AIPawn
{
    public class CompProperties_NeedMaximizing : CompProperties
    {
        public List<NeedDef> needDefs;
    }
}
