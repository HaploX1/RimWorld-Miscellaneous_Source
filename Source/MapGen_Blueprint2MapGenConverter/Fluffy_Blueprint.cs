using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blueprint2MapGenConverter
{
    public class Fluffy_Blueprint : IExposable
    {

        public List<Fluffy_BlueprintElement> contents;
        public string name;
        public IntVec2 size;

        public Fluffy_Blueprint()
        {

        }

        public void ExposeData()
        {
            Scribe_Collections.LookList<Fluffy_BlueprintElement>(ref this.contents, "BuildableThings", LookMode.Deep, new object[]
            {
                //this
            });
            Scribe_Values.LookValue<string>(ref this.name, "Name", null, false);
            Scribe_Values.LookValue<IntVec2>(ref this.size, "Size", default(IntVec2), false);
        }


    }
}
