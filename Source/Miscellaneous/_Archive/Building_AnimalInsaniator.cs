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

using ModCommon; // needed for Radar

namespace AnimalInsaniator
{
    /// <summary>
    /// This is the main class for the animal insaniator.
    /// An Object that makes all animals in reach insane every x ticks.
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>Usage of this code is free. All I ask is that you mention my name somewhere.</permission>
    class Building_AnimalInsaniator : Building
    {
        private int countdownStartValue = 75000;
        private int searchDistance = 25; // maybe increase to 30?

        private int countdown;

        private int countdownSDValue = 75000 * 5 + 1;

        private IEnumerable<Pawn> animals;

        private ThingDef_AnimalInsaniator def2;

        // Get the data from the extended def
        private void ReadXmlData()
        {
            def2 = (ThingDef_AnimalInsaniator)def;

            // Use the xml data if available
            if (def2.countdownValue != -1)
            {
                countdownStartValue = def2.countdownValue;
                searchDistance = def2.searchDistance;
                countdownSDValue = def2.countdownSDValue;
            }
        }


        /// <summary>
        /// Do work after the object is spawned
        /// </summary>
        public override void SpawnSetup()
        {
            base.SpawnSetup();

            // Get xml data
            ReadXmlData();

            // Set countdown => first activation in half the normal time
            countdown = countdownStartValue / 2;

        }


        /// <summary>
        /// To write and read data (savegame)
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.LookValue<int>(ref countdown, "countdown");
            Scribe_Values.LookValue<int>(ref countdownSDValue, "countdownSD");
        }


        /// <summary>
        /// Returns the inspection string
        /// </summary>
        /// <returns></returns>
        public override string GetInspectString()
        {
            StringBuilder str = new StringBuilder();
            str.Append(base.GetInspectString());
            str.AppendLine();
            //str.Append("" + countdown.ToString()).AppendLine();
            str.Append("" + countdownSDValue.ToString());

            return str.ToString();
        }


        /// <summary>
        /// This Tick is executed every 250 Ticks
        /// </summary>
        public override void TickRare()
        {
            base.TickRare();

            // Update selfdestruct countdown
            countdownSDValue -= 250;
            if (countdownSDValue <= 0)
            {
                DoSelfDestruct();
                return;
            }

            // Update countdown to next animal insanity
            countdown -= 250;

            // Do animal insanity check
            DoAnimalInsanity();
        }


        /// <summary>
        /// This Tick is executed 60 times per second
        /// </summary>
        public override void Tick()
        {
            base.Tick();

            // Update selfdestruct countdown
            countdownSDValue -= 1;
            if (countdownSDValue <= 0)
            {
                DoSelfDestruct();
                return;
            }

            // Update countdown to next animal insanity
            countdown -= 1;

            // Do animal insanity check
            DoAnimalInsanity();
        }



        private void DoAnimalInsanity()
        {
            if (countdown > 0)
                return;

            // Reset countdown
            countdown = countdownStartValue;

            // Find the animals to work with
            animals = Radar.FindAllAnimals(Position, searchDistance);

            // Make all animals normal, dazed, psychotic (10:30:60 chance) 
            foreach (Pawn animal in animals)
            {
                int value = UnityEngine.Random.Range(0, 100);

                if (value < 10)
                    PsychologyUtility.TryDoMentalBreak(animal, SanityState.Normal);
                else if (value < 40)
                    PsychologyUtility.TryDoMentalBreak(animal, SanityState.DazedWander);
                else
                    PsychologyUtility.TryDoMentalBreak(animal, SanityState.Psychotic);
                    
            }
        }


        private void DoSelfDestruct()
        {
            // Make an explosion
            Explosion.DoExplosion(Position, 25.0f, DamageTypeDefOf.Flame, null);

            // Leave nothing
            Destroy(DestroyMode.Vanish);
        }

    }
}
