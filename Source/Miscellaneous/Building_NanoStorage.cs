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


namespace NanoStorage
{
    /// <summary>
    /// This is the main class for the emergency storage.
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>Please check the provided license info for granted permissions.</permission>
    [StaticConstructorOnStartup]
    public class Building_NanoStorage : Building_Storage
    {

        // Internal variables
        private List<Thing> items = new List<Thing>();
        private IntVec3 storagePos;

        // Needed to stop working further ticks when this is destroyed
        private bool destroyedFlag = false;

        // Set the maximum internal storage places
        private const int maxStorage = 9;
        private bool outputActive;
        private bool storageInOutDisabled;
        private Thing lastItemInStorage;


        // Filled in SpawnSetup()
        private static Texture2D texUI_StorageSwitchModeOff;
        private static Texture2D texUI_StorageSwitchModeReceive;
        private static Texture2D texUI_StorageSwitchModeDispense;
        private static Texture2D texUI_StorageSwitchItem;


        public string txtMode = "Mode:";
        public string txtNone = "---";
        public string txtReceive = "Receive";
        public string txtDispense = "Dispense";
        public string txtSwitchTopItem = "Switch the top item.";
        public string txtSwitchToOff = "Switch mode to off.";
        public string txtSwitchToDispense = "Switch mode to dispense.";
        public string txtSwitchToReceive = "Switch mode to receive.";
        public string txtEmpty = "Empty";



        /// <summary>
        /// Do something after the object is initialized, but before it is spawned
        /// </summary>
        public override void PostMake()
        {
            base.PostMake();

            slotGroup = base.GetSlotGroup();

            // Translate texts
            txtMode = "Miscellaneous_Mode".Translate(); // "Mode:";
            txtNone = "Miscellaneous_None".Translate(); // "---";
            txtReceive = "Miscellaneous_Receive".Translate(); // "Receive";
            txtDispense = "Miscellaneous_Dispense".Translate(); // "Dispense";
            txtSwitchTopItem = "Miscellaneous_SwitchTopItem".Translate(); // "Switch the top item.";
            txtSwitchToOff = "Miscellaneous_SwitchModeToOff".Translate(); // "Switch mode to off.";
            txtSwitchToDispense = "Miscellaneous_SwitchModeToDispense".Translate(); // "Switch mode to dispense.";
            txtSwitchToReceive = "Miscellaneous_SwitchModeToReceive".Translate(); // "Switch mode to receive.";
            txtEmpty = "Miscellaneous_StateEmpty".Translate(); // "Empty";
        }


        /// <summary>
        /// Do something after the object is spawned
        /// </summary>
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            storagePos = Position; // new IntVec3(Position.x, Position.y, Position.z);

            base.SpawnSetup(map, respawningAfterLoad);

            LongEventHandler.ExecuteWhenFinished(SpawnSetup_Part2);

        }

        /// <summary>
        /// This is called seperately when the Mod-Thread is done.
        /// It is needed to be seperately from SpawnSetup, so that the graphics can be found
        /// </summary>
        private void SpawnSetup_Part2()
        {

            // Get grafics
            texUI_StorageSwitchModeOff = ContentFinder<Texture2D>.Get("UI/Commands/UI_StorageSwitchModeNone", true);
            texUI_StorageSwitchModeReceive = ContentFinder<Texture2D>.Get("UI/Commands/UI_StorageSwitchModeReceive", true);
            texUI_StorageSwitchModeDispense = ContentFinder<Texture2D>.Get("UI/Commands/UI_StorageSwitchModeDispense", true);
            texUI_StorageSwitchItem = ContentFinder<Texture2D>.Get("UI/Commands/UI_StorageSwitchItem", true);
        }


        /// <summary>
        /// Clean up when it is destroyed
        /// </summary>
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            destroyedFlag = true;

            for (int i = 0; i < items.Count; i++)
            {
                // create the integrated items at this position
                if ((items[i] != null) && (items[i].stackCount > 0))
                {
                    SpawnItem(storagePos, items[i]);
                }
            }

            base.Destroy(mode);
        }


        /// <summary>
        /// To write and read data (savegame)
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Deep.Look<StorageSettings>(ref this.settings, "settings", this);

            Scribe_Values.Look<bool>(ref outputActive, "outputActive");
            Scribe_Values.Look<bool>(ref storageInOutDisabled, "storageInOutDisabled");

            Scribe_Collections.Look(ref items, "items", LookMode.Deep, null);

            if (items == null)
                items = new List<Thing>();
        }


        /// <summary>
        /// Fill items position == my position
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<IntVec3> AllSlotCells()
        {
            //Where to bring the items to?
            yield return storagePos;
        }


        /// <summary>
        /// I lost something => create first list item
        /// </summary>
        /// <param name="newItem"></param>
        public override void Notify_LostThing(Thing lostItem)
        {
            if (items.Count == 0 || !outputActive || storageInOutDisabled || lastItemInStorage == lostItem)
                return;

            // create new item
            CreateItemFromStorage();
        }


        /// <summary>
        /// I received something => add stackCount to items and destroy it
        /// </summary>
        /// <param name="newItem"></param>
        public override void Notify_ReceivedThing(Thing newItem)
        {
            // count of items >= max => switch to output mode
            if (items.Count >= maxStorage)
            {
                outputActive = true;
                storageInOutDisabled = true;
            }

            // do nothing further, if output mode is active
            if (outputActive || storageInOutDisabled)
                return;

            // received a valid item, save the ThingDef
            AddItemToStorage(newItem);
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

            DoTickWork(1);

        }

        private int tickCounter;
        private void DoTickWork(int ticks)
        {
            tickCounter = tickCounter - ticks;
            if (tickCounter <= 0)
            {
                // Random counter to prevent time overlappings with other nano storages
                tickCounter = Rand.RangeInclusive(45, 90);

                // Enable / disable slotGroup depending on storage state
                if (storageInOutDisabled && slotGroup != null)
                {
                    foreach (IntVec3 c in slotGroup.CellsList)
                        slotGroup.Notify_LostCell(c);
                    slotGroup = null;
                    return;
                }
                if (!storageInOutDisabled && base.slotGroup == null)
                {
                    slotGroup = new SlotGroup(this);
                    return;
                }


                // Check if output field is empty => create item
                if (outputActive && !storageInOutDisabled)
                    CreateItemFromStorage();
                else if (!outputActive && !storageInOutDisabled)
                {
                    Thing thing = FindItemAtPosition(storagePos);
                    if (thing != null)
                        Notify_ReceivedThing(thing);
                }

            }
        }



        /// <summary>
        /// This string will be shown when the object is selected (focus)
        /// </summary>
        /// <returns></returns>
        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string baseString = base.GetInspectString();
            if (!baseString.NullOrEmpty()) {
                stringBuilder.Append(base.GetInspectString());
                stringBuilder.AppendLine();
            }

            stringBuilder.Append(txtMode + " ");
            if (!outputActive && !storageInOutDisabled)
                stringBuilder.Append(txtReceive);
            else if (outputActive && !storageInOutDisabled)
                stringBuilder.Append(txtDispense);
            else
                stringBuilder.Append(txtNone);
            stringBuilder.AppendLine();

            if (items.Count > 0)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (i != 0 && Math.IEEERemainder(i, 3) == 0.0f) 
                        stringBuilder.AppendLine();

                    stringBuilder.Append(CreateItemLabel(items[i].def, items[i].stackCount));
                    stringBuilder.Append("    ");
                }
            }
            else
            {
                stringBuilder.Append(txtEmpty);
            }

            return stringBuilder.ToString().TrimEndNewlines();
        }


        /// <summary>
        /// This creates new selection buttons with a new graphic
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<Gizmo> GetGizmos()
        {
            // Key-Binding F - Switch top item
            Command_Action optF;
            optF = new Command_Action();
            optF.icon = texUI_StorageSwitchItem;
            optF.defaultDesc = txtSwitchTopItem;
            optF.hotKey = KeyBindingDefOf.Misc1; //B
            optF.activateSound = SoundDef.Named("Click");
            optF.action = SwitchTopItem;
            optF.Disabled = false;
            optF.groupKey = 31325911;
            yield return optF;

            // Key-Binding X - Switch top item
            Command_Action optX;
            optX = new Command_Action();
            if (!outputActive && !storageInOutDisabled) // Receive
            {
                optX.icon = texUI_StorageSwitchModeReceive;
                optX.defaultDesc = txtSwitchToOff;
            }
            else if (outputActive && !storageInOutDisabled) // Dispense
            {
                optX.icon = texUI_StorageSwitchModeDispense;
                optX.defaultDesc = txtSwitchToReceive;
            }
            else // Off
            {
                optX.icon = texUI_StorageSwitchModeOff;
                optX.defaultDesc = txtSwitchToDispense;
            }
            optX.hotKey = KeyBindingDefOf.Misc3; //Y
            optX.activateSound = SoundDef.Named("Click");
            optX.action = SwitchInputOutput;
            optX.Disabled = false;
            optX.groupKey = 31325912;
            yield return optX;


            foreach (Gizmo g in base.GetGizmos())
                yield return g;

        }


        /// <summary>
        /// Switch add item to the item list and create the first item from the item list new
        /// </summary>
        private void SwitchTopItem()
        {
            // First: set to output to prevent an new adding to the storage
            outputActive = true;
            storageInOutDisabled = false;

            if (items.Count == 0)
                return;

            // Find item at position and add it to storage
            Thing thing = FindItemAtPosition(storagePos);

            if (thing != null)
                AddItemToStorage(thing, false);

            // Now create the top item from storage
            CreateItemFromStorage();
        }



        /// <summary>
        /// Switch mode Receive = Dispense
        /// </summary>
        private void SwitchInputOutput()
        {
            //outputActive = !outputActive;

            if (storageInOutDisabled)
            {
                storageInOutDisabled = false;
                return;
            }

            if (outputActive)
            {
                outputActive = false;
                storageInOutDisabled = false;
            } 
            else if (!outputActive)
            {
                outputActive = true;
                storageInOutDisabled = true;
            }
        }


        /// <summary>
        /// Create an item
        /// </summary>
        /// <param name="pos">The position, where it should be created</param>
        /// <param name="thingDef">The definition of the thing</param>
        /// <param name="count">The count of items to be created</param>
        private void SpawnItem(IntVec3 pos, ThingDef thingDef, ThingDef stuffDef, int count)
        {
            Thing thing = ThingMaker.MakeThing(thingDef, stuffDef);
            GenSpawn.Spawn(thing, storagePos, Map).stackCount = count;
        }
        private void SpawnItem(IntVec3 pos, Thing thing, int count)
        {
            Thing t = GenSpawn.Spawn(thing, pos, Map);
            t.stackCount = count;
        }
        private void SpawnItem(IntVec3 pos, Thing thing)
        {
            Thing t = GenSpawn.Spawn(thing, pos, Map);
        }


        /// <summary>
        /// Add an item to the item storage
        /// </summary>
        /// <param name="item"></param>
        private void AddItemToStorage(Thing item, bool tryAbsorb = true)
        {
            int workStack = item.stackCount;

            if (tryAbsorb)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].def == item.def && items[i].stackCount < items[i].def.stackLimit)
                    {
                        if (items[i].stackCount + item.stackCount <= items[i].def.stackLimit)
                        {

                            items[i].TryAbsorbStack(item, true);
                            //items[i].stackCount = items[i].stackCount + workStack;
                            workStack = 0;
                            break;
                        }
                        else
                        {
                            workStack = workStack - (items[i].def.stackLimit - items[i].stackCount);

                            items[i].TryAbsorbStack(item, true); // In this case TryAbsorbStack doesn't work, so I have to do it the simple way..
                            //items[i].stackCount = items[i].def.stackLimit;

                        }
                    }

                }
            }

            lastItemInStorage = item;

            if (workStack > 0)
            {
                item.stackCount = workStack;
                items.Add(item);
                item.DeSpawn();
            }
            else
            {
                //item.Destroy(DestroyMode.Vanish);
            }
        }


        private string CreateItemLabel(ThingDef def, int count)
        {
            return def.LabelCap + " x" + count.ToString();
        }


        /// <summary>
        /// Create an item from the item storage
        /// </summary>
        private void CreateItemFromStorage()
        {
            if (items.Count == 0)
                return;

            // Find item at position
            Thing thing = FindItemAtPosition(storagePos);
            if (thing != null)
                return;

            // Create top item from list
            SpawnItem(storagePos, items[0]);
            
            // Remove top item from list
            items.Remove(items[0]);
        }


        /// <summary>
        /// Find first item at position
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private Thing FindItemAtPosition(IntVec3 pos)
        {
            IEnumerable<Thing> things = Map.listerThings.AllThings.Where(t => t.Position == pos);

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


    }
}
