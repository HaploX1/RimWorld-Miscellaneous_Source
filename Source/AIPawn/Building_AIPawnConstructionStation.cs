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
    public class Building_AIPawnConstructionStation : Building
    {

        #region Variables

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
        public IntVec3 RefillPosition
        {
            get
            {
                return refillPos;
            }
        }

        #endregion


        // ================== Create / Destroy ==================
        #region Create / Destroy
        
        public override void SpawnSetup(Map map, bool respawnAfterLoad)
        {

            base.SpawnSetup(map, respawnAfterLoad);

            LongEventHandler.ExecuteWhenFinished(SpawnSetup_Part2);

        }

        private void SpawnSetup_Part2()
        {

            ReadXmlData();
            DoTranslations();

            if (!UI_StartProduction_Path.NullOrEmpty())
                UI_StartProduction = ContentFinder<Texture2D>.Get(UI_StartProduction_Path, true);
            if (!UI_StopProduction_Path.NullOrEmpty())
                UI_StopProduction = ContentFinder<Texture2D>.Get(UI_StopProduction_Path, true);
            
            refillPos = Position;
            

            power = base.GetComp<CompPowerTrader>();
            //glower.def.glowColor = new ColorInt(255,0,0,255);
        }
        
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            destroyedFlag = true;

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

        /// <summary>
        /// To write and read data (savegame)
        /// </summary>
        public override void ExposeData()
        {
            ReadXmlData();
            
            base.ExposeData();
            Scribe_Values.Look(ref countSteel, "countSteel");
            Scribe_Values.Look(ref countSilver, "countSilver");
            Scribe_Values.Look(ref productionActive, "productionActive");
            Scribe_Values.Look(ref gatheringSuppliesActive, "gatheringSuppliesActive");

            if (Scribe.mode == LoadSaveMode.LoadingVars)
                gatheringSuppliesActive = false;

        }

        #endregion


        // ================== Ticks ==================
        #region Ticks

        public override void TickRare()
        {
        }

        public override void Tick()
        {
            if (destroyedFlag)
                return;

            base.Tick();


            // Fill until count > x, then start production
            if (gatheringSuppliesActive && countSteel >= maxSteelCount && countSilver >= maxSilverCount)
            {
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
            
        public bool ReceiveThing(Thing thing)
        {
            if (!gatheringSuppliesActive)
                return false;

            if (thing.def == ThingDefOf.Steel)
            {
                int remaining = (countSteel + thing.stackCount) - maxSteelCount;
                countSteel = countSteel + thing.stackCount > maxSteelCount ? maxSteelCount : countSteel + thing.stackCount;
                if (remaining > 0)
                    thing.stackCount = remaining;
                else
                    thing.Destroy(DestroyMode.Vanish);
            }
            if (thing.def == ThingDefOf.Silver)
            {
                int remaining = (countSilver + thing.stackCount) - maxSilverCount;
                countSilver = countSilver + thing.stackCount > maxSilverCount ? maxSilverCount : countSilver + thing.stackCount;
                if (remaining > 0)
                    thing.stackCount = remaining;
                else
                    thing.Destroy(DestroyMode.Vanish);
            }

            return true;
        }

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
                return;
            }

            // Production active: Can not be stopped
            if (productionActive)
                return;

            // start gathering supplies
            gatheringSuppliesActive = true;
        }
        
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
