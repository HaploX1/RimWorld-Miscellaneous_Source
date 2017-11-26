using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed
//using Verse.AI; // Needed when you do something with the AI
//using Verse.Sound; // Needed when you do something with the Sound

namespace AnimalInsaniator
{
    public class ThingDef_AnimalInsaniator : ThingDef
    {

        public int countdownValue = -1;
        public int searchDistance = -1;
        public int countdownSDValue = -1;

        public string incidentLetter = null;

    }
}
