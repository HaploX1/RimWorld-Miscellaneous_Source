using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blueprint2MapGenConverter
{
    public class Fluffy_BlueprintElement : IExposable
    {
        Fluffy_Blueprint blueprint;

        public string TerrainDef;
        public string ThingDef;
        public string Stuff;
        public IntVec3 Position;
        public Rot4 Rotation;


        public Fluffy_BlueprintElement() {  }
        public Fluffy_BlueprintElement(Fluffy_Blueprint blueprint)
        {
            this.blueprint = blueprint;
        }

        public void ExposeData()
        {
            Scribe_Values.LookValue<string>(ref this.ThingDef, "ThingDef");
            Scribe_Values.LookValue<string>(ref this.Stuff, "Stuff");
            Scribe_Values.LookValue<string>(ref this.TerrainDef, "TerrainDef");
            Scribe_Values.LookValue<IntVec3>(ref this.Position, "Position", default(IntVec3), false);
            Scribe_Values.LookValue<Rot4>(ref this.Rotation, "Rotation", Rot4.North, false);
        }

    }
    
}
