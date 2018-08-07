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
    /// This is the main class for the mai construction station.
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>For usage of this code, please look at the license information.</permission>
    [StaticConstructorOnStartup]
    public class Building_AIPawnConstructionStation : Building, ISlotGroupParent, IStoreSettingsParent, IHaulDestination
    {

        #region Variables

        // These variables are needed to setup the storage field
        public SlotGroup slotGroup;
        public StorageSettings settingsStorage;
        private List<IntVec3> cachedOccupiedCells;

        // The count of resources
        public int countSteel = 0;
        public int countSilver = 0;
        public bool gatheringSuppliesActive = false;
        public bool productionActive = false;

        // internal count variable
        private int counterUsingSteelSilver = 0;

        // Use up 10 every x Ticks (Info: 60 Ticks == 1s)
        private int counterUsingSteelSilverMax = 1200;

        // The max. amount of steel and silver to be filled with
        private int maxSteelCount = 6;
        private int maxSilverCount = 6;

        // Internal variables
        private string steelDefName = "Steel";
        private string silverDefName = "Silver";
        private ThingDef steelDef = null;
        private ThingDef silverDef = null;
        private IntVec3 refillPos;
        private Thing receivedThing = null;

        // Needed to stop working further ticks when this is destroyed
        private bool destroyedFlag = false;

        //private string pawnDefName = "AIPawn";
        private string buildingDefName = "AIPawn_Inactive";

        public CompPowerTrader power;


        // Save the grafics
        private string UI_StartProduction_Path = "";
        private string UI_StopProduction_Path = "";
        private static Texture2D UI_StartProduction;
        private static Texture2D UI_StopProduction;


        // various texts
        //private string txtSteel = "Steel:";
        //private string txtSilver = "Silver:";
        private string txtStartProduction = "Start Production.";
        private string txtStopProduction = "Stop Production.";
        private string txtProductionRunningSign = "->";
        private string txtProductionRunning = "Production running...";
        private string txtOutputBlocked = "Output blocked.";

        public bool IgnoreStoredThingsBeauty
        {
            get
            {
                return this.def.building.ignoreStoredThingsBeauty;
            }
        }

        public int SteelAmountRequired
        {
            get
            {
                if (!gatheringSuppliesActive)
                    return 0;

                return maxSteelCount - countSteel;
            }
        }
        public int SilverAmountRequired
        {
            get
            {
                if (!gatheringSuppliesActive)
                    return 0;

                return maxSilverCount - countSilver;
            }
        }

        #endregion


        // ================== Create / Destroy ==================
        #region Create / Destroy

        /// <summary>
        /// Do something after the object is initialized, but before it is spawned
        /// </summary>
        public override void PostMake()
        {
            base.PostMake();

            this.settingsStorage = new StorageSettings(this);

            if (def.building.defaultStorageSettings != null)
                settingsStorage.CopyFrom(def.building.defaultStorageSettings);

        }


        /// <summary>
        /// Do something after the object is spawned
        /// </summary>
        public override void SpawnSetup(Map map, bool respawnAfterLoad)
        {

            base.SpawnSetup(map, respawnAfterLoad);

            LongEventHandler.ExecuteWhenFinished(SpawnSetup_Part2);

        }

        /// <summary>
        /// This is called seperately when the Mod-Thread is done.
        /// It is needed to be seperately from SpawnSetup, so that the graphics can be found
        /// </summary>
        private void SpawnSetup_Part2()
        {

            ReadXmlData();
            DoTranslations();

            if (!UI_StartProduction_Path.NullOrEmpty())
                UI_StartProduction = ContentFinder<Texture2D>.Get(UI_StartProduction_Path, true);
            if (!UI_StopProduction_Path.NullOrEmpty())
                UI_StopProduction = ContentFinder<Texture2D>.Get(UI_StopProduction_Path, true);

            //refillPos = InteractionCell;
            refillPos = Position;

            cachedOccupiedCells = this.AllSlotCells().ToList<IntVec3>();

            //slotGroup = new SlotGroup(this);

            power = base.GetComp<CompPowerTrader>();
            //glower.def.glowColor = new ColorInt(255,0,0,255);
        }


        /// <summary>
        /// Clean up when it is destroyed
        /// </summary>
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            destroyedFlag = true;

            //Check if the slotGroup is active >> deregister it
            if (slotGroup != null)
            {
                slotGroup.Notify_LostCell(this.PositionHeld);
                //slotGroup = null;
            }

            base.Destroy(mode);

            if ((countSteel > 0) && (steelDef != null))
            {
                // create the remaining resources as new resources at the refill position
                GenSpawn.Spawn(steelDef, refillPos, Map).stackCount = countSteel;
                countSteel = 0;
            }
            if ((countSilver > 0) && (silverDef != null))
            {
                // create the remaining resources as new resources at the refill position
                GenSpawn.Spawn(silverDef, refillPos, Map).stackCount = countSilver;
                countSilver = 0;
            }
        }

        #endregion


        // ================== Load/Save ==================
        #region Load / Save

        // Get the data from the extended def
        private void ReadXmlData()
        {
            ThingDef_Building_AIPawnConstructionStation def2 = (ThingDef_Building_AIPawnConstructionStation)def;

            // update values, if xml data is valid
            if (def2 != null && def2.maxSteelCount != -1 && def2.maxSilverCount != -1)
            {
                maxSteelCount = def2.maxSteelCount;
                maxSilverCount = def2.maxSilverCount;
                counterUsingSteelSilverMax = def2.counterUsingResources;
                UI_StartProduction_Path = def2.UI_StartProduction_Path;
                UI_StopProduction_Path = def2.UI_StopProduction_Path;
            }
        }

        private void DoTranslations()
        {

            // Translate texts
            txtStartProduction = "AIPawn_StartProduction".Translate(); //"Start Production.";
            txtStopProduction = "AIPawn_StopProduction".Translate(); //"Stop Production.";
            txtProductionRunning = "AIPawn_ProductionRunning".Translate(); //"Production running...";
            txtProductionRunningSign = "AIPawn_ProductionRunningSign".Translate(); //"->";
            txtOutputBlocked = "AIPawn_OutputBlocked".Translate(); //
        }
        
        public bool Accepts(Thing t)
        {
            return settingsStorage.AllowedToAccept(t);
        }

        /// <summary>
        /// To write and read data (savegame)
        /// </summary>
        public override void ExposeData()
        {
            ReadXmlData();
            
            base.ExposeData();
            Scribe_Deep.Look<StorageSettings>(ref settingsStorage, "settingsStorage", this);
            Scribe_Values.Look(ref countSteel, "countSteel");
            Scribe_Values.Look(ref countSilver, "countSilver");
            Scribe_Values.Look(ref productionActive, "productionActive");
            Scribe_Values.Look(ref gatheringSuppliesActive, "gatheringSuppliesActive");

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                gatheringSuppliesActive = false;

                if (slotGroup != null)
                {
                    slotGroup.Notify_LostCell(this.PositionHeld);
                    slotGroup = null;
                }
            }

        }

        #endregion


        // ================== Storage Settings ==================
        #region Storage Settings

        public bool StorageTabVisible
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Don't know what this does...
        /// </summary>
        /// <returns></returns>
        public string SlotYielderLabel()
        {
            return this.Label;
        }

        /// <summary>
        /// Base storage settings (from xml)
        /// </summary>
        /// <returns></returns>
        public StorageSettings GetParentStoreSettings()
        {
            return def.building.fixedStorageSettings;
        }

        /// <summary>
        /// Active storage settings (from xml or base)
        /// </summary>
        /// <returns></returns>
        public StorageSettings GetStoreSettings()
        {
            return settingsStorage;
        }

        /// <summary>
        /// Returns the occupied slot list
        /// </summary>
        /// <returns></returns>
        public List<IntVec3> AllSlotCellsList()
        {
            return cachedOccupiedCells;
        }

        /// <summary>
        /// Fill resources position == my position
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IntVec3> AllSlotCells()
        {
            //Where to bring the wood to refill to?
            yield return refillPos;
        }

        /// <summary>
        /// Returns the slotgroup
        /// </summary>
        /// <returns></returns>
        public SlotGroup GetSlotGroup()
        {
            return this.slotGroup;
        }

        /// <summary>
        /// Not used
        /// </summary>
        /// <param name="newItem"></param>
        public void Notify_LostThing(Thing newItem)
        {
        }

        /// <summary>
        /// I received something => add stackCount to wood and destroy it
        /// </summary>
        /// <param name="newItem"></param>
        public void Notify_ReceivedThing(Thing newItem)
        {
            if (productionActive || !gatheringSuppliesActive)
                return;

            receivedThing = newItem;
            DoWork_ReceivedThing();
        }

        private void DoWork_ReceivedThing()
        {
            Thing newItem = receivedThing;
            receivedThing = null;
            
            if (productionActive || !gatheringSuppliesActive)
                return;

            if (newItem == null || newItem.Position != refillPos)
                return;

            if (newItem.def.defName == steelDefName)
            {
                // received a valid wood item, save the ThingDef
                steelDef = newItem.def;

                if (countSteel + newItem.stackCount <= maxSteelCount)
                {
                    // stack doesn't overfill
                    countSteel += newItem.stackCount;
                    newItem.Destroy();
                    return;
                }
                else
                {
                    // stack does overfill 
                    int tmpSteel = maxSteelCount - countSteel;
                    newItem.stackCount = newItem.stackCount - tmpSteel;
                    countSteel += tmpSteel;
                    // remove itemdef from allowed items list
                    slotGroup.Settings.filter.SetAllow(newItem.def, false);
                    return;
                }
            }
            if (newItem.def.defName == silverDefName)
            {
                // received a valid wood item, save the ThingDef
                silverDef = newItem.def;

                if (countSilver + newItem.stackCount <= maxSilverCount)
                {
                    // stack doesn't overfill
                    countSilver += newItem.stackCount;
                    newItem.Destroy();
                    return;
                }
                else
                {
                    // stack does overfill 
                    int tmpSilver = maxSilverCount - countSilver;
                    newItem.stackCount = newItem.stackCount - tmpSilver;
                    countSilver += tmpSilver;
                    // remove itemdef from filter list
                    slotGroup.Settings.filter.SetAllow(newItem.def, false);
                    return;
                }
            }

        }

        #endregion


        // ================== Ticks ==================
        #region Ticks

        /// <summary>
        /// This is used, when the Ticker is changed from Normal to Rare
        /// This is a tick thats done once every 5s = 3000Ticks
        /// </summary>
        public override void TickRare()
        {
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


            // Fill until count > x, then start production
            if (countSteel >= maxSteelCount && countSilver >= maxSilverCount && slotGroup != null)
            {
                slotGroup.Notify_LostCell(this.PositionHeld);//.Notify_ParentDestroying();
                slotGroup = null;

                gatheringSuppliesActive = false;
                productionActive = true;
            }


            // No power, do nothing further
            if (power != null && !power.PowerOn)
                return;


            // Use 10 items every x Ticks (Info: 60 Ticks == 1s)
            if (productionActive && (countSteel > 0 || countSilver > 0))
            {
                counterUsingSteelSilver += 1;
                if (counterUsingSteelSilver >= counterUsingSteelSilverMax)
                {
                    countSteel -= 10;
                    if (countSteel < 0)
                        countSteel = 0;

                    countSilver -= 10;
                    if (countSilver < 0)
                        countSilver = 0;

                    counterUsingSteelSilver = 0;
                }
            }


            // Production done
            if (productionActive && countSteel <= 0 && countSilver <= 0)
            {
                productionActive = false;
                Create_Building_AIPawn_Inactive();
            }


        }

        #endregion


        // ================== GUI ==================
        #region GUI

        /// <summary>
        /// This string will be shown when the object is selected (focus)
        /// </summary>
        /// <returns></returns>
        public override string GetInspectString()
        {
            if (steelDef == null)
                steelDef = DefDatabase<ThingDef>.GetNamed(steelDefName);

            if (silverDef == null)
                silverDef = DefDatabase<ThingDef>.GetNamed(silverDefName);

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());

            stringBuilder.AppendLine();
            stringBuilder.Append(steelDef.LabelCap + ": " + countSteel.ToString() + " / " + maxSteelCount.ToString());
            stringBuilder.Append("  ");
            stringBuilder.Append(silverDef.LabelCap + ": " + countSilver.ToString() + " / " + maxSilverCount.ToString());

            if (productionActive)
                stringBuilder.Append(" " + txtProductionRunningSign + " " + counterUsingSteelSilver.ToString());

            return stringBuilder.ToString();
        }


        /// <summary>
        /// This creates new selection buttons with a new graphic
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (!productionActive && !gatheringSuppliesActive)
            {
                foreach (Gizmo cbase in base.GetGizmos())
                    yield return cbase;
            }

            if (!power.PowerOn && !productionActive)
                yield break;

            // Key-Binding B - Start Production
            Command_Action opt1;
            opt1 = new Command_Action();
            opt1.disabled = productionActive;
            opt1.disabledReason = "Production running...";
            if (!productionActive && !gatheringSuppliesActive)
            {
                opt1.defaultDesc = txtStartProduction;
                opt1.icon = UI_StartProduction;
            }
            else
            {
                opt1.defaultDesc = txtStopProduction;
                opt1.icon = UI_StopProduction;
            }
            opt1.hotKey = KeyBindingDefOf.Misc1;
            opt1.activateSound = SoundDef.Named("Click");
            opt1.action = Button_StartStopProduction;
            opt1.groupKey = 313676141;
            
            yield return opt1;

        }

        #endregion


        // ================== Functions ==================
        #region Functions

        /// <summary>
        /// This is the action, thats done when clicking on the production button
        /// </summary>
        private void Button_StartStopProduction()
        {
            if (!CheckProductionOutletFree())
            {
                Messages.Message(txtOutputBlocked, MessageTypeDefOf.RejectInput);
                return;
            }

            // Gathering supplies: Stop gathering
            if (gatheringSuppliesActive)
            {
                gatheringSuppliesActive = false;
                slotGroup.Notify_LostCell(this.PositionHeld);
                slotGroup = null;
                return;
            }

            // Production active: Can not be stopped
            if (productionActive)
            {
                return;
            }

            // start gathering supplies
            gatheringSuppliesActive = true;

            // Restart storage
            if (slotGroup == null)
                slotGroup = new SlotGroup(this);

            // Reset filter
            slotGroup.Settings.filter.SetAllow(ThingDef.Named(steelDefName), true);
            slotGroup.Settings.filter.SetAllow(ThingDef.Named(silverDefName), true);

        }


        /// <summary>
        /// Create the produced AI Pawn
        /// </summary>
        private void Create_Building_AIPawn_Inactive()
        {
            ThingDef thingDef = ThingDef.Named(buildingDefName);
            Thing thing = ThingMaker.MakeThing(thingDef);

            thing.SetFaction(this.Faction);

            IntVec3 spawnPos = InteractionCell;

            if (thing.def.Minifiable)
            {
                Thing minified = MinifyUtility.MakeMinified(thing);
                thing = minified;
            }

                if (Rotation == Rot4.North)
                {
                    // Create the inactive mai R/H of the interaction square
                    spawnPos = spawnPos + new IntVec3(1, 0, 0); // RH 1cell from the interaction cell
                    GenSpawn.Spawn(thing, spawnPos, Map, Rot4.East);
                }
                if (Rotation == Rot4.East)
                {
                    spawnPos = spawnPos + new IntVec3(0, 0, -1); // bottom 1cell from the interaction cell
                    GenSpawn.Spawn(thing, spawnPos, Map, Rot4.South);
                }
                if (Rotation == Rot4.South)
                {
                    spawnPos = spawnPos + new IntVec3(-1, 0, 0); // LH 1cell from the interaction cell
                    GenSpawn.Spawn(thing, spawnPos, Map, Rot4.West);
                }
                if (Rotation == Rot4.West)
                {
                    spawnPos = spawnPos + new IntVec3(0, 0, 1); // top 1cell from the interaction cell
                    GenSpawn.Spawn(thing, spawnPos, Map, Rot4.North);
                }

        }

        private bool CheckProductionOutletFree()
        {
            IntVec3 spawnPos = IntVec3.Invalid;
            Thing foundThing = null;

            if (Rotation == Rot4.North)
            {
                // Create the inactive mai R/H of the interaction square
                spawnPos = InteractionCell + new IntVec3(1, 0, 0); // RH 1cell from the interaction cell
            }
            if (Rotation == Rot4.East)
            {
                spawnPos = InteractionCell + new IntVec3(0, 0, -1); // bottom 1cell from the interaction cell
            }
            if (Rotation == Rot4.South)
            {
                spawnPos = InteractionCell + new IntVec3(-1, 0, 0); // LH 1cell from the interaction cell
            }
            if (Rotation == Rot4.West)
            {
                spawnPos = InteractionCell + new IntVec3(0, 0, 1); // top 1cell from the interaction cell
            }


            if (spawnPos != IntVec3.Invalid)
            {
                foundThing = Map.thingGrid.ThingAt(spawnPos, ThingCategory.Building);
            }

            if (foundThing == null)
            {
                return true;
            }

            return false;

        }



        #endregion

    }
}
