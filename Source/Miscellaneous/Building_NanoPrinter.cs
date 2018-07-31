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
    /// <summary>
    /// This is a template of an building, that accepts resources
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>Please check the provided license info for granted permissions.</permission>
    class Building_NanoPrinter : Building_Storage
    {

        // ===================== Variables =====================
        #region Variables

        // Power trader
        private CompPowerTrader powerComp;

        //// These variables are needed to setup the storage field
        //public SlotGroup slotGroup;
        //public StorageSettings settingsStorage;
        //private List<IntVec3> cachedOccupiedCells;

        // This is the storage input/output position
        private IntVec3 collectorPos;

        // These are the item data lists that hold the collected item infos
        private List<ThingDef> storageDefs = new List<ThingDef>();
        private List<int> storageCounts = new List<int>();
        private List<string> storageLabels = new List<string>();

        // The list of how many material is needed
        private Dictionary<ThingDef, int> neededMaterial;
        // The item to create
        private string scannedThingName = "";
        private ThingDef scannedBlueprint;
        private ThingDef scannedBlueprintStuff;
        private int productionCountDown;
        public int productionCountDownStartValue = 300;
        private int gatheringItemCheckCounter;

        // Price calc values
        public float costPriceToSteel = 1.2f;
        public float costHealthToSteel = 1.5f;


        private ThingDef thingDefOfSteel;

        // Helper flag for when this will be destroyed
        private bool destroyedFlag = false;

        private bool loaded = false;

        private NanoPrinterStatus status = NanoPrinterStatus.Idle;
        private enum NanoPrinterStatus
        {
            Idle = 0,
            Paused,
            Scanning,
            Gathering,
            Printing,
            Error
        }

        private string ScannerDefName = "NanoScanner";//"Hopper";
        private string ResourceDefName = "Steel";

        // UI graphics
        public string UI_NanoPrinterButtonError_path = "UI/Commands/NanoPrinter/UI_ButtonError";
        public string UI_NanoPrinterButtonStart_path = "UI/Commands/NanoPrinter/UI_ButtonStart";
        public string UI_NanoPrinterButtonStop_path = "UI/Commands/NanoPrinter/UI_ButtonStop";
        public Texture2D UI_NanoPrinterButtonError;
        public Texture2D UI_NanoPrinterButtonStart;
        public Texture2D UI_NanoPrinterButtonStop;

        // text variables
        private string txtError = "NanoPrinter_Error"; // Error
        private string txtErrorDescr = "NanoPrinter_ErrorDescr"; // An error occured. Click to reset.
        private string txtStart = "NanoPrinter_Start"; // Start
        private string txtStop = "NanoPrinter_Stop"; // Stop
        private string txtScanningNoHoppersFound = "NanoPrinter_ScanningNoHoppersFound"; // No adjanced hoppers found to scan from
        private string txtScanningNoItemsFound = "NanoPrinter_ScanningNoItemsFound"; // No items to scan found
        private string txtBlueprint = "NanoPrinter_ActiveBlueprint"; // Blueprint:
        private string txtStatus = "NanoPrinter_ActiveStatus"; // Status:

        private string txtProductionRunningSign = "= >";

        private List<IntVec3> cachedAdjCellsCardinal;
        private List<IntVec3> AdjCellsCardinal
        {
            get
            {
                if (this.cachedAdjCellsCardinal == null)
                {
                    this.cachedAdjCellsCardinal = GenAdj.CellsAdjacentCardinal(this).ToList<IntVec3>();
                }
                return this.cachedAdjCellsCardinal;
            }
        }

        // Find Hopper next to this
        public IEnumerable<Building> AllAdjacentScanner
        {
            get
            {
                ThingDef thingDef = ThingDef.Named(ScannerDefName);

                for (int i = 0; i < this.AdjCellsCardinal.Count; i++)
                {
                    Building edifice = this.AdjCellsCardinal[i].GetEdifice(Map);
                    if (edifice != null && edifice.def == thingDef)
                    {
                        yield return (Building)edifice;
                    }
                }
            }
        }
        // Find Items in all Hoppers
        public Thing AllItemsInScanner
        {
            get
            {
                for (int i = 0; i < this.AdjCellsCardinal.Count; i++)
                {
                    Thing thing = null;
                    Thing thing1 = null;
                    List<Thing> thingList = this.AdjCellsCardinal[i].GetThingList(Map);
                    for (int j = 0; j < thingList.Count; j++)
                    {
                        Thing item = thingList[j];
                        if (item.def.IsApparel || item.def.IsMeleeWeapon || item.def.IsRangedWeapon)
                        {
                            thing = item;
                        }
                        if (item.def == ThingDef.Named(ScannerDefName))
                        {
                            thing1 = item;
                        }
                    }
                    if (thing != null && thing1 != null)
                    {
                        return thing;
                    }
                }
                return null;
            }
        }

        // Is this powered?
        public bool PowerOk
        {
            get
            {
                if (powerComp != null)
                    return powerComp.PowerOn;
                else
                    return true;
            }
        }

        #endregion


        // ===================== Setup / Load/Save =====================
        #region Setup Work / Load/Save

        private void ReadXmlData()
        {
            ThingDef_NanoPrinter def2 = (ThingDef_NanoPrinter)def;

            if (!def2.XmlExtended)
                return;

            costPriceToSteel = def2.CostPriceToSteel;
            costHealthToSteel = def2.CostHealthToSteel;

            productionCountDown = def2.ProductionCountDownStartValue;

            ScannerDefName = def2.ScannerDefName;
            ResourceDefName = def2.ResourceDefName;
        }


        /// <summary>
        /// Do something after the object is initialized, but before it is spawned
        /// </summary>
        public override void PostMake()
        {
            base.PostMake();

            //settingsStorage = new StorageSettings(this);
            //if (def.building.defaultStorageSettings != null)
            //{
            //    settingsStorage.CopyFrom(def.building.defaultStorageSettings);
            //}

        }

        /// <summary>
        /// This is called when the building is spawned into the world
        /// </summary>
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            ReadXmlData();

            // Prepare storage info data
            collectorPos = InteractionCell;

            base.SpawnSetup(map, respawningAfterLoad);

            LongEventHandler.ExecuteWhenFinished(SpawnSetup_Part2);

        }

        /// <summary>
        /// This is called seperately when the Mod-Thread is done.
        /// It is needed to be seperately from SpawnSetup, so that the graphics can be found
        /// </summary>
        private void SpawnSetup_Part2()
        {

            powerComp = GetComp<CompPowerTrader>();

            thingDefOfSteel = ThingDef.Named(ResourceDefName);

            //cachedOccupiedCells = new List<IntVec3>();
            //cachedOccupiedCells.Add(collectorPos);

            // Load graphics for the UI elements
            UI_NanoPrinterButtonError = ContentFinder<Texture2D>.Get(UI_NanoPrinterButtonError_path, true);
            UI_NanoPrinterButtonStart = ContentFinder<Texture2D>.Get(UI_NanoPrinterButtonStart_path, true);
            UI_NanoPrinterButtonStop = ContentFinder<Texture2D>.Get(UI_NanoPrinterButtonStop_path, true);

            if (!loaded)
                ResetNanoPrinter();

        }

        /// <summary>
        /// To write and read data (savegame)
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            // Save and load the storage settings
            //Scribe_Deep.LookDeep<StorageSettings>(ref settingsStorage, "settingsStorage", this);

            // Save and load the items in storage
            Scribe_Collections.Look(ref storageDefs, "storageDefs", LookMode.Def, this);
            Scribe_Collections.Look(ref storageCounts, "storageCounts", LookMode.Value, this);
            Scribe_Collections.Look(ref storageLabels, "storageLabels", LookMode.Value, this);

            // Save and load the work data
            Scribe_Values.Look<int>(ref productionCountDown, "productionCountDown");
            Scribe_Values.Look<NanoPrinterStatus>(ref status, "status");

            if (Scribe.mode == LoadSaveMode.LoadingVars)
                loaded = true;
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
            // Not gathering, but receiving -> destroy slotGroup
            if (status != NanoPrinterStatus.Gathering)
            {
                if (slotGroup != null)
                {
                    foreach (IntVec3 c in slotGroup.CellsList)
                        slotGroup.Notify_LostCell(c);
                    slotGroup = null;
                }
                return;
            }

            // No needed materials found -> error -> go to idle
            if (neededMaterial == null || neededMaterial.Count() == 0)
            {
                SetReceivedError();
                return;
            }

            // ThingDef not in neededMaterial list
            if (!neededMaterial.ContainsKey(item.def))
            {
                SetReceivedError();
                return;
            }

            // Get the max needed material
            int maxCount = neededMaterial[item.def];

            // save received items to collections
            AddItemToStorage(item, maxCount);
        }

        private void SetReceivedError()
        {
            foreach (IntVec3 c in slotGroup.CellsList)
                slotGroup.Notify_LostCell(c);
            slotGroup = null;
            status = NanoPrinterStatus.Error;
        }

        #endregion


        // ===================== Storage WorkFunctions =====================
        #region Storage WorkFunctions

        /// <summary>
        /// Spawn an item
        /// </summary>
        /// <param name="pos">The position, where it should be created</param>
        /// <param name="thingDef">The definition of the thing</param>
        /// <param name="count">The count of items to be created</param>
        private void SpawnItem(IntVec3 pos, Map map, ThingDef thingDef, ThingDef thingDefStuff, int count)
        {
            Thing thing = ThingMaker.MakeThing(thingDef, thingDefStuff);
            thing = GenSpawn.Spawn(thing, pos, map);
            thing.stackCount = count;
        }

        /// <summary>
        /// Add an item to the item storage and destroy the original
        /// </summary>
        /// <param name="item"></param>
        private void AddItemToStorage(Thing item, int maxCount)
        {
            // Try to find entry
            int foundPosition = -1;
            for (int i = 0; i < storageDefs.Count(); i++ )
            {
                if (storageDefs[i] == item.def)
                {
                    foundPosition = i;
                    break;
                }
            }


            // Item found 
            if (foundPosition >= 0)
            {
                if (storageCounts[foundPosition] + item.stackCount > maxCount)
                {
                    item.stackCount = item.stackCount - (maxCount - storageCounts[foundPosition]);
                    storageCounts[foundPosition] = maxCount;
                    if (!CheckIfGatheringDone())
                        slotGroup.Settings.filter.SetAllow(item.def, false);
                }
                else
                {
                    storageCounts[foundPosition] += item.stackCount;
                    // We've taken all the infos, so now destroy the item
                    item.Destroy(DestroyMode.Vanish);
                }
            }
            // Item doesn't exist in list yet
            else 
            {
                // Add item info to storage list
                storageDefs.Add(item.def);
                storageLabels.Add(item.Label);

                if (item.stackCount > maxCount)
                {
                    storageCounts.Add(maxCount);
                    item.stackCount = item.stackCount - maxCount;
                    if (!CheckIfGatheringDone())
                        slotGroup.Settings.filter.SetAllow(item.def, false);
                }
                else
                {
                    storageCounts.Add(item.stackCount);
                    // We've taken all the infos, so now destroy the item
                    item.Destroy(DestroyMode.Vanish);
                }
            }
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
            SpawnItem(collectorPos, Map, storageDefs[0], null, storageCounts[0]);

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

        /// <summary>
        /// Check if all needed items have enough material gathered
        /// </summary>
        /// <returns></returns>
        private bool CheckIfGatheringDone()
        {
            bool gatheringDone = true;
            for (int i = 0; i < storageDefs.Count(); i++ )
            {
                ThingDef thingDef = storageDefs[i];
                int thingCount = storageCounts[i];
                for (int n = 0; n < neededMaterial.Count(); n++)
                {
                    if (!neededMaterial.ContainsKey(thingDef))
                        continue;

                    if (thingCount < neededMaterial[thingDef])
                    {
                        gatheringDone = false;
                        break;
                    }
                }
            }

            if (!gatheringDone)
                return false;

            if (slotGroup != null)
            {
                foreach (IntVec3 c in slotGroup.CellsList)
                    slotGroup.Notify_LostCell(c);
                slotGroup = null;
            }

            SwitchState();

            return true;
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
        public override void Tick()
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
            if (loaded)
            {
                DoWorkAfterLoading();
                loaded = false;
            }

            

            // If gathering check if something is on the receive cell
            if (status == NanoPrinterStatus.Gathering)
            {
                // Work only every x Ticks
                gatheringItemCheckCounter = gatheringItemCheckCounter + tickAmount;
                if (gatheringItemCheckCounter < 60)
                    return;
                gatheringItemCheckCounter = 0;

                CheckIfItemIsAtReceivePoint();
            }


            // No power
            if (!PowerOk)
                return;

            // Wrong status
            if (status != NanoPrinterStatus.Printing)
                return;

            productionCountDown -= tickAmount;
            if (productionCountDown <= 0)
            {
                bool finished = true;
                for (int i = 0; i < storageDefs.Count(); i++)
                {
                    storageCounts[i] = storageCounts[i] - 10;
                    if (storageCounts[i] < 0)
                        storageCounts[i] = 0;

                    if (storageCounts[i] > 0)
                        finished = false;
                }

                productionCountDown = productionCountDownStartValue;

                if (finished)
                {
                    SpawnItem(Position, Map, scannedBlueprint, scannedBlueprintStuff, 1);
                    ResetNanoPrinter();
                }
            }


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
            StringBuilder sb = new StringBuilder();
            
            string txtStatusEnum = "NanoPrinterStatus_" + status.ToString();

            string baseString = base.GetInspectString();

            if (!baseString.NullOrEmpty())
            {
                sb.Append(baseString);
                sb.AppendLine();
            }
            sb.Append(txtStatus.Translate());
            sb.Append(" ");
            sb.Append( txtStatusEnum.Translate());
            sb.AppendLine();

            sb.Append(txtBlueprint.Translate() + " ");
            if (scannedBlueprint == null)
                sb.Append("---");
            else
                //sb.Append(scannedBlueprint.label);
                sb.Append(scannedThingName);

            if (neededMaterial != null)
            {
                sb.AppendLine();
                foreach (KeyValuePair<ThingDef, int> td in neededMaterial)
                {
                    int storedValue = 0;
                    for (int i = 0; i < storageDefs.Count(); i++)
                    {
                        if (storageDefs[i] == td.Key)
                        {
                            storedValue = storageCounts[i];
                            break;
                        }
                    }

                    sb.Append(td.Key.label.CapitalizeFirst() + ": " + storedValue.ToString() + "/" + td.Value.ToString() + " ");
                }
            }
            else
            {
                // do nothing
            }

            if (status == NanoPrinterStatus.Printing)
            {
                sb.Append(" ").Append(txtProductionRunningSign).Append(" ").Append( productionCountDown.ToString());
            }

            return sb.ToString().TrimEndNewlines();

        }

        /// <summary>
        /// This creates selection buttons
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<Gizmo> GetGizmos()
        {
            IList<Gizmo> list = new List<Gizmo>();

            int baseGroupNo = 31366777;


            if ((status == NanoPrinterStatus.Idle || status == NanoPrinterStatus.Paused || status == NanoPrinterStatus.Error) && PowerOk)
            {
                Command_Action ca1 = new Command_Action();
                ca1.disabled = (status == NanoPrinterStatus.Error);
                ca1.disabledReason = txtError.Translate();
                ca1.defaultDesc = txtStart.Translate();
                ca1.icon = UI_NanoPrinterButtonStart;
                ca1.hotKey = KeyBindingDefOf.Misc1; //B
                ca1.activateSound = SoundDef.Named("Click");
                ca1.action = ButtonPressed_Start;
                ca1.groupKey = baseGroupNo + 1;

                list.Add(ca1);
            }

            if (status != NanoPrinterStatus.Idle && status != NanoPrinterStatus.Error)
            {
                Command_Action ca2 = new Command_Action();
                ca2.disabled = false;
                ca2.disabledReason = "";
                ca2.defaultDesc = txtStop.Translate();
                ca2.icon = UI_NanoPrinterButtonStop;
                ca2.hotKey = KeyBindingDefOf.Misc1; //B
                ca2.activateSound = SoundDef.Named("Click");
                ca2.action = ButtonPressed_Stop;
                ca2.groupKey = baseGroupNo + 2;

                list.Add(ca2);
            }

            if (status == NanoPrinterStatus.Error)
            {
                Command_Action ca0 = new Command_Action();
                ca0.disabled = false;
                ca0.disabledReason = "";
                ca0.defaultDesc = txtErrorDescr.Translate();
                ca0.icon = UI_NanoPrinterButtonError;
                ca0.hotKey = KeyBindingDefOf.Misc1; //B
                ca0.activateSound = SoundDef.Named("Click");
                ca0.action = ButtonPressed_Error;
                ca0.groupKey = baseGroupNo + 0;

                list.Add(ca0);
            }

            // Adding the base.GetCommands() when not empty
            IEnumerable<Gizmo> baseList = base.GetGizmos();
            if (baseList != null)
                return list.AsEnumerable<Gizmo>().Concat(baseList);
            else
                return list.AsEnumerable<Gizmo>();
        }

        #endregion


        // ===================== Inspections =====================
        #region Button Handling

        private void ButtonPressed_Error()
        {
            ResetNanoPrinter();
        }

        private void ButtonPressed_Start()
        {
            if (status == NanoPrinterStatus.Error)
                return;

            CheckIfItemIsAtReceivePoint();

            status = NanoPrinterStatus.Scanning;
            SwitchState();

        }

        private void ButtonPressed_Stop()
        {

            if (status != NanoPrinterStatus.Paused && status != NanoPrinterStatus.Idle)
            {
                if (slotGroup != null)
                {
                    // Destroy storage zone
                    foreach (IntVec3 c in slotGroup.CellsList)
                        slotGroup.Notify_LostCell(c);
                    slotGroup = null;
                }

                status = NanoPrinterStatus.Paused;
                return;
            }

            ResetNanoPrinter();
        }

        #endregion


        // ===================== Status switch =====================
        #region Status Switching

        private void SwitchState()
        {
            switch (status)
            {
                case NanoPrinterStatus.Paused:
                    status = NanoPrinterStatus.Paused;
                    break;

                case NanoPrinterStatus.Idle:
                    status = NanoPrinterStatus.Scanning;
                    break;

                case NanoPrinterStatus.Scanning:
                    if (StateScanning())
                    {
                        CreateNewSlotGroup();
                        status = NanoPrinterStatus.Gathering;
                    }
                    break;

                case NanoPrinterStatus.Gathering:
                    status = NanoPrinterStatus.Printing;
                    break;

                case NanoPrinterStatus.Printing:
                    ResetNanoPrinter();
                    status = NanoPrinterStatus.Idle;
                    break;

                default:
                    status = NanoPrinterStatus.Error;
                    break;
            }
        }

        private bool StateScanning(bool onlyScanning = false)
        {
            if (neededMaterial != null && neededMaterial.Count() != 0 && scannedBlueprint != null && scannedThingName != "")
            {
                return true;
            }

            IEnumerable<Building> scanner = AllAdjacentScanner;
            if (scanner == null || scanner.Count() <= 0)
            {
                if (!onlyScanning)
                {
                    Messages.Message(txtScanningNoHoppersFound.Translate(), MessageTypeDefOf.RejectInput);
                    status = NanoPrinterStatus.Idle;
                }
                return false;
            }


            // Set def of scanned object
            Thing foundThing = AllItemsInScanner;
            if (foundThing == null)
            {
                if (!onlyScanning)
                {
                    Messages.Message(txtScanningNoItemsFound.Translate(), MessageTypeDefOf.RejectInput);
                    status = NanoPrinterStatus.Idle;
                }
                return false;
            }

            // calc needed material for the found object
            int price = CalcItemPrice(foundThing);

            scannedThingName = foundThing.Label;
            scannedBlueprint = foundThing.def;
            scannedBlueprintStuff = foundThing.Stuff;

            neededMaterial.Add(thingDefOfSteel, price);

            return true;
        }

        #endregion


        // ===================== Functions =====================
        #region Functions

        private void DoWorkAfterLoading()
        {
            NanoPrinterStatus saveStatus = status;
            int saveProductionCountdown = productionCountDown;

            ResetNanoPrinter();

            StateScanning(true);

            status = saveStatus;
            productionCountDown = saveProductionCountdown;

            if (status == NanoPrinterStatus.Gathering)
                CreateNewSlotGroup();
        }

        private void ResetNanoPrinter()
        {
            // reset storage settings
            settings = new StorageSettings(this);
            if (def.building.defaultStorageSettings != null)
            {
                settings.CopyFrom(def.building.defaultStorageSettings);
            }

            // reset slotGroup
            if (slotGroup != null)
            {
                // Destroy storage zone
                foreach (IntVec3 c in slotGroup.CellsList)
                    slotGroup.Notify_LostCell(c);
                slotGroup = null;
            }

            // reset neededMaterial
            scannedBlueprint = null;
            neededMaterial = new Dictionary<ThingDef, int>();

            productionCountDown = productionCountDownStartValue;

            status = NanoPrinterStatus.Idle;
        }

        private void CreateNewSlotGroup()
        {
            if (slotGroup == null)
            {
                // Create the new collection position (storage zone)
                slotGroup = new SlotGroup(this);
                status = NanoPrinterStatus.Gathering;
            }
        }

        private int CalcItemPrice(Thing thing)
        {
            int price = 0;

            if (thing == null)
                return price;

            // calc needed material for the found object
            if (thing.def.BaseMarketValue > 1 && (thing.def.IsRangedWeapon || thing.def.equipmentType == EquipmentType.Primary))
            {
                // Weapons
                price = (int)Math.Floor(thing.def.BaseMarketValue * costPriceToSteel);

                //Log.Error("Weapon - BaseMarketValue: " + thing.def.BaseMarketValue.ToString());
            }
            else if (thing.def.isTechHediff && thing.def.BaseMarketValue > 1)
            {
                // Bodyparts
                price = (int)Math.Floor(thing.def.BaseMarketValue * costPriceToSteel * 1.3f);

                //Log.Error("Bodypart - BaseMarketValue: " + thing.def.BaseMarketValue.ToString());
            }
            else if (thing.def.IsApparel)
            {
                // Apparel
                if (thing.Stuff == null)
                {
                    price = (int)Math.Floor(thing.def.BaseMarketValue * costPriceToSteel);

                    //Log.Error("Apparel w/o Stuff - BaseMarketValue: " + thing.def.BaseMarketValue.ToString());
                }
                else
                {
                    price = (int)Math.Floor((thing.def.BaseMarketValue + thing.Stuff.BaseMarketValue) * costPriceToSteel * 1.3f);

                    //Log.Error("Apparel w Stuff - BaseMarketValue: " + thing.def.BaseMarketValue.ToString() + "; Stuff Value: " + thing.Stuff.BaseMarketValue.ToString());
                }
            }
            else if (thing.def.BaseMarketValue > 35f)
            {
                // Everything else, that costs more then 35.0
                price = (int)Math.Floor(thing.def.BaseMarketValue * costPriceToSteel * 0.9f);

                //Log.Error("Everything else > 35 Silver - BaseMarketValue: " + thing.def.BaseMarketValue.ToString());
            }
            else
            {
                // Everything else
                price = (int)Math.Floor(thing.def.BaseMaxHitPoints * costHealthToSteel);

                //Log.Error("Everything else <= 35 Silver - BaseMarketValue: " + thing.def.BaseMarketValue.ToString());
            }

            return price;
        }

        private void CheckIfItemIsAtReceivePoint()
        {
            if (slotGroup == null)
                return;

            // Check if there is something at the receive position
            IntVec3 pos = this.AllSlotCellsList()[0];
            IEnumerable<Thing> things = Map.listerThings.AllThings.Where(t => t.Position == pos);
            Thing foundThing = null;
            foreach (Thing thing in things)
            {
                if (thing.def.EverStorable(false))
                {
                    foundThing = thing;
                    break;
                }
            }
            if (foundThing != null)
                Notify_ReceivedThing(foundThing);
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

        ///// <summary>
        ///// Base storage settings (from xml)
        ///// </summary>
        ///// <returns></returns>
        //public StorageSettings GetParentStoreSettings()
        //{
        //    return def.building.fixedStorageSettings;
        //}

        ///// <summary>
        ///// Active storage settings (from xml or base)
        ///// </summary>
        ///// <returns></returns>
        //public StorageSettings GetStoreSettings()
        //{
        //    return settingsStorage;
        //}

        /// <summary>
        /// Fill items position == my position
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<IntVec3> AllSlotCells()
        {
            //Where to bring the items to?
            yield return collectorPos;
        }

        ///// <summary>
        ///// Returns the occupied slot list
        ///// </summary>
        ///// <returns></returns>
        //public List<IntVec3> AllSlotCellsListFast()
        //{
        //    return cachedOccupiedCells;
        //}

        ///// <summary>
        ///// Returns the slotgroup
        ///// </summary>
        ///// <returns></returns>
        //public SlotGroup GetSlotGroup()
        //{
        //    return slotGroup;
        //}

        ///// <summary>
        ///// Don't know what this does...
        ///// </summary>
        ///// <returns></returns>
        //public string SlotYielderLabel()
        //{
        //    return Label;
        //}

        #endregion
    }
}
