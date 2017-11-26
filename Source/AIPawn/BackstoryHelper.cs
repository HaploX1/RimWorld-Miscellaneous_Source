using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace AIPawn
{
    /// <summary>
    /// This is the backstory helper class.
    /// This is needed to create the backstories to the database, so that the pawn can use them.
    /// Afterwards they are again removed from the database to prevent other pawns from using them.
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>Usage of this code is free. All I ask is that you mention my name somewhere.</permission>
    [StaticConstructorOnStartup]
    public class BackstoryHelper
    {
        public static string BackstoryDefNameIdentifier = "MAI";
        public static string BackstoryDefNameIdentifierDisabled = "_off_";

        public static List<WorkTags> workTagCollection = null;
        public static List<List<WorkTags>> allCombos = null;
        public static List<Backstory> backstories = null;
        /// <summary>
        /// The AI can never learn these items
        /// </summary>
        /// <returns></returns>
        public static WorkTags GetBasicWorkDisables()
        {
            return WorkTags.Artistic;
        }

        public static void AddNewBackstoriesToDatabase()
        {
            //string log = "ADDED";

            IEnumerable<Backstory> backstoriesToAdd = NewBackstories();
            foreach (Backstory b in backstoriesToAdd)
            {
                bool added = AddBackstoryToDatabase(b);
                //log = log + Environment.NewLine + added.ToString() + " - " + b.defName;
            }

            //Log.Error(log);
        }
        public static void RemoveNewBackstoriesFromDatabase()
        {
            //string log = "REMOVED";

            IEnumerable<Backstory> backstoriesToRemove = NewBackstories();
            foreach (Backstory b in backstoriesToRemove)
            {
                bool removed = RemoveBackstoryFromDatabase(b);
                //log = log + Environment.NewLine + removed.ToString() + " - " + b.defName;
            }
            //Log.Error(log);
        }

        public static string GetBackstoryUniqueKey(WorkTags workDisables)
        {
            //string log = "UNIQUE KEY";

            string baseBackstoryDefName = BackstoryDefNameIdentifier + BackstoryDefNameIdentifierDisabled;
            string ukey = baseBackstoryDefName + workDisables.ToString().Replace(", ", "_");

            //log = log + Environment.NewLine + ukey;

            //Log.Error(log);
            return ukey;
        }
        private static IEnumerable<Backstory> NewBackstories()
        {
            if (backstories == null)
            {
            Backstory backstory;

                backstories = new List<Backstory>();
            string txtBaseDesc = "----------";
            string txtTitle = "---";
            string txtTitleShort = "AI";
            BodyType bodyType = BodyType.Female;

                if (workTagCollection == null)
                {
                    // Set a list with the possible disabled items
                    workTagCollection = new List<WorkTags>();
                    workTagCollection.Add(WorkTags.Animals);
                    //workTagCollection.Add(WorkTags.Artistic); //- In BasicWorkDisables
                    workTagCollection.Add(WorkTags.Caring);
                    workTagCollection.Add(WorkTags.Cleaning);
                    workTagCollection.Add(WorkTags.Cooking);
                    workTagCollection.Add(WorkTags.Crafting);
                    workTagCollection.Add(WorkTags.Firefighting);
                    workTagCollection.Add(WorkTags.Hauling);
                    workTagCollection.Add(WorkTags.Intellectual); 
                    workTagCollection.Add(WorkTags.ManualDumb);
                    workTagCollection.Add(WorkTags.ManualSkilled);
                    workTagCollection.Add(WorkTags.Mining);
                    workTagCollection.Add(WorkTags.PlantWork);
                    workTagCollection.Add(WorkTags.Social); 
                    workTagCollection.Add(WorkTags.Violent);
                    //workTagCollection.Add();
                }

            // Get the base disables (The AI can never do these)
            WorkTags workDisables1 = GetBasicWorkDisables();
            WorkTags workDisables2;

                // Create minimal backstory
                backstory = new Backstory();
                backstory.baseDesc = txtBaseDesc;
                backstory.SetTitle(txtTitle);
                backstory.SetTitleShort(txtTitleShort);
                backstory.bodyTypeGlobal = bodyType;
                backstory.bodyTypeFemale = BodyType.Female;
                backstory.bodyTypeMale = BodyType.Male;
                backstory.workDisables = workDisables1;
                backstory.identifier = GetBackstoryUniqueKey(backstory.workDisables);
                backstories.Add(backstory);

            //string log = workDisables1.ToString().Replace(", ", "_"); //Logging

            // add other backstories
                if (allCombos == null)
                    allCombos = HelperAIPawn.GetAllCombos<WorkTags>(workTagCollection);

            foreach (List<WorkTags> activeCombo in allCombos)
            {
                workDisables2 = GetBasicWorkDisables();
                foreach (WorkTags entry in activeCombo)
                {
                    workDisables2 = workDisables2 | entry;
                }

                    backstory = new Backstory();
                    backstory.baseDesc = txtBaseDesc;
                    backstory.SetTitle(txtTitle);
                    backstory.SetTitleShort(txtTitleShort);
                    backstory.bodyTypeGlobal = bodyType;
                    backstory.bodyTypeFemale = BodyType.Female;
                    backstory.bodyTypeMale = BodyType.Male;
                    backstory.workDisables = workDisables2;
                    backstory.identifier = GetBackstoryUniqueKey(backstory.workDisables);
                    backstories.Add(backstory);

                    //log = log + Environment.NewLine + backstory.defName; // workDisables2.ToString().Replace(", ", "_"); //Logging
                }
            }

            //Log.Error(log); //Logging

            return backstories.AsEnumerable<Backstory>();

        }
        private static bool AddBackstoryToDatabase(Backstory backstory)
        {
            bool keyFound = BackstoryDatabase.allBackstories.ContainsKey(backstory.identifier);

            //Log.Error("Adding Key:" + backstory.identifier + "// keyFound:" + keyFound.ToString());

            if (!keyFound)
                BackstoryDatabase.AddBackstory(backstory);

            return !keyFound;
        }
        private static bool RemoveBackstoryFromDatabase(Backstory backstory)
        {
            bool keyFound = BackstoryDatabase.allBackstories.ContainsKey(backstory.identifier);

            //Log.Error("Remove Key:" + backstory.defName + "// keyFound:" + keyFound.ToString());

            if (keyFound)
                BackstoryDatabase.allBackstories.Remove(backstory.identifier);

            return keyFound;
        }

    }
}
