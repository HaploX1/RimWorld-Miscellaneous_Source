using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed
using Verse.AI; // Needed when you do something with the AI
//using Verse.Sound; // Needed when you do something with the Sound

namespace AnimalInsaniator
{
    /// <summary>
    /// This is the incident "Animal Insaniator".
    /// A part of your ships engine crashed down.
    /// The radiation of it will let all the animals around it go berserk.
    /// </summary>
    /// <author>Haplo</author>
    class IncidentWorker_EngineCrashed : IncidentWorker
    {
        private string defNameCrashedObject = "CrashedSpaceshipEngine";

        public override bool TryExecute(IncidentParms parms)
        {
            // Don't execute, when the colonist-count is too low
            if (Find.ListerPawns.ColonistCount <= 4)
                return false;

            // Find a random square thats standable and not hidden by fog
            IntVec3 intVec3 = GenCellFinder.RandomCellWith((IntVec3 sq) => (!sq.Standable() ? false : !sq.IsFogged()));

            // Make the crash explosion
            Explosion.DoExplosion(intVec3, 3f, DamageTypeDefOf.Flame, null);

            string letter = "A part of the engine of your spaceship has crashed nearby./n/n" + 
                            "Caution: It's radiation may cause changes in the behavior of nearby animals!/n" +
                            "I recomend to destroy it as soon as possible.";

            // Get the letter data from xml if possible
            string xmlLetter = ((ThingDef_AnimalInsaniator)ThingDef.Named(defNameCrashedObject)).incidentLetter;
            if (xmlLetter != null)
                letter = xmlLetter;

            // Make Letter
            letter = letter.Replace("/n", Environment.NewLine);
            Find.LetterStack.ReceiveLetter(new Letter(letter, LetterType.BadNonUrgent, intVec3));

            // Spawn the Animal Insaniator
            ThingDef thingDef = ThingDef.Named(defNameCrashedObject);
            Thing thing = GenSpawn.Spawn(thingDef, intVec3);
            thing.SetFactionDirect(Find.FactionManager.AllFactions.Where(f => f.IsHostileToward(Faction.OfColony)).RandomElement<Faction>());

            return true;
        }


    }
}
