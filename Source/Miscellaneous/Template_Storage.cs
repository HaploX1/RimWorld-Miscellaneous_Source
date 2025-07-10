using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed
using Verse.AI; // Needed when you do something with the AI
//using Verse.Sound; // Needed when you do something with the Sound

namespace Storage
{
    /// <summary>
    /// This is a template of an building, that accepts resources
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>Usage of this code is free. All I ask is that you mention my name somewhere.</permission>
    class Template_Storage : Building_Storage //Building, SlotGroupParent
    {

        // ===================== Variables =====================
        #region Variables

        // This is the storage input/output position
        private IntVec3         collectorPos;

        // These are the item data lists that hold the collected item infos
        private List<ThingDef>  storageDefs = new List<ThingDef>();
        private List<int>       storageCounts = new List<int>();
        private List<string>    storageLabels = new List<string>();

        // Helper flag for when this will be destroyed
        private bool            destroyedFlag = false;

        #endregion




        // ===================== Setup Work =====================
        #region Setup Work
        /// <summary>
        /// Do something after the object is initialized, but before it is spawned
        /// </summary>
        public override void PostMake()
        {
            base.PostMake();
        }

        /// <summary>
        /// This is called when the building is spawned into the world
        /// </summary>
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            // Prepare storage info data
            collectorPos = InteractionCell;

            base.SpawnSetup(map, respawningAfterLoad);

        }


        /// <summary>
        /// To write and read data (savegame)
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();

            // Save and load the items in storage
            Scribe_Collections.Look(ref storageDefs, "storageDefs", LookMode.Def, this);
            Scribe_Collections.Look(ref storageCounts, "storageCounts", LookMode.Value, this);
            Scribe_Collections.Look(ref storageLabels, "storageLabels", LookMode.Value, this);

        }

        #endregion



        // ===================== Storage Notifications =====================
        #region Storage Notifications

        /// <summary>
        /// Notification: The storage lost something => do some work?
        /// </summary>
        /// <param name="item">The item that's lost from storage square.</param>
        public override void Notify_LostThing(Thing item)
        {
            if (storageDefs.Count == 0)
                return;

            // create a new item
            //CreateItemFromStorage();
        }

        /// <summary>
        /// Notification: The storage received something => do some work?
        /// </summary>
        /// <param name="item"></param>
        public override void Notify_ReceivedThing(Thing item)
        {
            // save received items to collections
            //AddItemToStorage(item);
        }

        #endregion



        // ===================== Storage WorkFunctions =====================
        #region Storage Functions

        /// <summary>
        /// Spawn an item
        /// </summary>
        /// <param name="pos">The position, where it should be created</param>
        /// <param name="thingDef">The definition of the thing</param>
        /// <param name="count">The count of items to be created</param>
        private Thing SpawnItem(IntVec3 pos, Map map, ThingDef thingDef, int count)
        {
            Thing thing = GenSpawn.Spawn(thingDef, pos, map);
            thing.stackCount = count;
            return thing;
        }

        /// <summary>
        /// Add an item to the item storage and destroy the original
        /// </summary>
        /// <param name="item"></param>
        private void AddItemToStorage(Thing item)
        {
            // Add item info to storage list
            storageDefs.Add(item.def);
            storageCounts.Add(item.stackCount);
            storageLabels.Add(item.Label);

            // We've taken all the infos, so now destroy the item
            item.Destroy(DestroyMode.Vanish);
        }

        /// <summary>
        /// Spawn an item from the item storage
        /// </summary>
        private void CreateItemFromStorage()
        {
            if (storageDefs.Count == 0)
                return;

            // Find item at position
            Thing thing = FindItemAtPosition(collectorPos);
            if (thing != null)
                return;

            // Create top item from storage list
            SpawnItem(collectorPos, Map, storageDefs[0], storageCounts[0]);

            // Remove the created item from storage list
            storageDefs.Remove(storageDefs[0]);
            storageCounts.Remove(storageCounts[0]);
            storageLabels.Remove(storageLabels[0]);
        }

        /// <summary>
        /// Find the first item at position
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private Thing FindItemAtPosition(IntVec3 pos)
        {
            // Get all things at the position
            IEnumerable<Thing> things = Map.listerThings.AllThings.Where(t => t.Position == pos);

            // Find the first item
            Thing thing = null;
            foreach (Thing t in things)
            {
                if (t.def.category == ThingCategory.Item && t != this)
                {
                    thing = t;
                    break;
                }
            }

            return thing;
        }

        #endregion



        // ===================== Ticker =====================
        #region Ticker

        /// <summary>
        /// This is used, when the Ticker is changed from Normal to Rare
        /// This is a tick thats done once every 250 Ticks
        /// </summary>
        public override void TickRare()
        {
            if (destroyedFlag)
                return;

            //base.TickRare();

            TickerWork(250);
        }

        /// <summary>
        /// This is used, when the Ticker is set to Normal
        /// This Tick is done often (60 times per second)
        /// </summary>
        protected override void Tick()
        {
            if (destroyedFlag)
                return;

            base.Tick();

            TickerWork(1);

        }

        /// <summary>
        /// Do your ticker work here...
        /// </summary>
        /// <param name="tickAmount">The amount of tickes passed since the last call</param>
        private void TickerWork(int tickAmount)
        {

        }

        #endregion



        // ===================== Inspections =====================
        #region Inspections

        /// <summary>
        /// This string will be shown when the object is selected (focus)
        /// </summary>
        /// <returns></returns>
        public override string GetInspectString()
        {
            return base.GetInspectString();
        }

        /// <summary>
        /// This creates selection buttons
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<Gizmo> GetGizmos()
        {
            // Do base gizmos
            foreach (var c in base.GetGizmos())
                yield return c;

        }

        #endregion



        // ===================== Destroy =====================
        #region Destroy

        /// <summary>
        /// Clean up when it is destroyed
        /// </summary>
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            // block further ticker work
            destroyedFlag = true;

            ////Check if the slotGroup is active >> deregister it
            //if (slotGroup != null)
            //{
            //    slotGroup.Notify_ParentDestroying();
            //    slotGroup = null;
            //}
            if (slotGroup == null)
            {
                // Needed because of base destroy!!
                slotGroup = new SlotGroup(this);
            }

            // create the stored resources as new resources at the refill position
            if (storageDefs.Count > 0)
            {
                for (int i = 0; i < storageDefs.Count; i++)
                {
                    if ((storageCounts[i] > 0) && (storageDefs[i] != null))
                    {
                        // create the item at this position
                        GenSpawn.Spawn(storageDefs[i], collectorPos, Map).stackCount = storageCounts[i];
                    }
                }
            }

            base.Destroy(mode);
        }

        #endregion



        // ===================== Storage Setup =====================
        #region Storage Setup

        /// <summary>
        /// Fill items position == my position
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<IntVec3> AllSlotCells()
        {
            //Where to bring the items to?
            yield return collectorPos;
        }

        #endregion

    }
}
