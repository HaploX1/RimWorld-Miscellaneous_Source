using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed


namespace AIPawn
{
    public class CompNeedMaximizing : ThingComp
    {
        public CompNeedMaximizing() { }



        public CompProperties_NeedMaximizing Props
        {
            get
            {
                return (CompProperties_NeedMaximizing)this.props;
            }
        }

        public override void CompTick()
        {
            if (Props == null || Props.needDefs == null || Props.needDefs.Count == 0)
                return;
            if (parent == null || !parent.Spawned || parent.Destroyed)
                return;
            if ((parent as Pawn) == null)
                return;

            foreach (NeedDef needDef in Props.needDefs)
            {
                foreach (Need need in (parent as Pawn).needs.AllNeeds)
                {
                    if (need.def.defName == needDef.defName)
                    {
                        if (need.CurLevel <= 0.98f)
                            need.CurLevel = 1.0f;
                        break;
                    }
                }
            }
        }
        
        public override string CompInspectStringExtra()
        {
            return base.CompInspectStringExtra();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo c in base.CompGetGizmosExtra())
                yield return c;

            
        }
        
    }
}
