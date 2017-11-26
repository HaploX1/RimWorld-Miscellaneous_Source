using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed
using Verse.AI; // Needed when you do something with the AI
//using Verse.Sound; // Needed when you do something with the Sound

namespace NanoPrinter
{
    class ThingDef_NanoPrinter : ThingDef
    {
        public bool XmlExtended = false;

        public float CostPriceToSteel = 0f;
        public float CostHealthToSteel = 0f;

        public int ProductionCountDownStartValue = 0;

        public string ScannerDefName = "";
        public string ResourceDefName = "";

    }
}
