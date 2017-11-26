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
    [StaticConstructorOnStartup()]
    public class Building_AIPawnRechargeStation : Building_Bed
    {
        #region Variables


        public ThingDef_Building_RechargeStation def2 = null;
        public string SecondaryGraphicPath = "";
        public string MedicalGraphicPath = "";
        public string MedicalSecondaryGraphicPath = "";

        private string txtNoOwner = "AIPawn_NoOwner";
        private string txtSendOwnerToSleep = "AIPawn_SendOwnerToSleep";
        
        public Graphic SecondaryGraphic;
        public Graphic PrimaryGraphic;
        public Graphic MedicalGraphic;
        public Graphic MedicalSecondaryGraphic;

        public string UI_ForceSleepButtonPath = "";
        private static Texture2D UI_ForceSleepButton;

        private string HopperDefName = "AIPawn_NaniteAssembler";

        #endregion

        #region Initialization

        private void ReadXMLData()
        {
            def2 = (ThingDef_Building_RechargeStation)def;
            if (!def2.secondaryGraphicPath.NullOrEmpty())
            {
                SecondaryGraphicPath = def2.secondaryGraphicPath;
                MedicalGraphicPath = def2.medicalGraphicPath;
                MedicalSecondaryGraphicPath = def2.medicalSecondaryGraphicPath;
                UI_ForceSleepButtonPath = def2.uiButtonForceSleepPath;
            }

        }

        private void GetGraphics()
        {
            if (SecondaryGraphicPath.NullOrEmpty())
                ReadXMLData();

            if (def2 == null)
                def2 = (ThingDef_Building_RechargeStation)def;

            if (PrimaryGraphic == null && !def.graphicData.texPath.NullOrEmpty())
            {
                PrimaryGraphic = GraphicDatabase.Get<Graphic_Multi>(def.graphicData.texPath, def.graphic.Shader, def.graphic.drawSize, def.graphic.Color, def.graphic.ColorTwo);
            }

            if (SecondaryGraphic == null && !SecondaryGraphicPath.NullOrEmpty())
            {
                SecondaryGraphic = GraphicDatabase.Get<Graphic_Multi>(SecondaryGraphicPath, def.graphic.Shader, def.graphic.drawSize, def.graphic.Color, def.graphic.ColorTwo);
            }

            if (MedicalGraphic == null && !MedicalGraphicPath.NullOrEmpty())
            {
                MedicalGraphic = GraphicDatabase.Get<Graphic_Multi>(MedicalGraphicPath, def.graphic.Shader, def.graphic.drawSize, def.graphic.Color, def.graphic.ColorTwo);
            }

            if (MedicalSecondaryGraphic == null)
            {
                if (!MedicalSecondaryGraphicPath.NullOrEmpty())
                {
                    MedicalSecondaryGraphic = GraphicDatabase.Get<Graphic_Multi>(MedicalSecondaryGraphicPath, def.graphic.Shader, def.graphic.drawSize, def.graphic.Color, def.graphic.ColorTwo);
                }
                else if (!MedicalGraphicPath.NullOrEmpty())
                {
                    MedicalSecondaryGraphic = GraphicDatabase.Get<Graphic_Multi>(MedicalGraphicPath, def.graphic.Shader, def.graphic.drawSize, def.graphic.Color, def.graphic.ColorTwo);
                }
            }
        }


        public override void PostMake()
        {

            base.PostMake();
        }

        public override void SpawnSetup(Map map, bool respawnAfterLoad)
        {
            base.SpawnSetup(map, respawnAfterLoad);

            //ReadXMLData();

            LongEventHandler.ExecuteWhenFinished(Setup_Part2);

        }

        /// <summary>
        /// This is called seperately when the Mod-Thread is done.
        /// It is needed to be seperately from SpawnSetup, so that the graphics can be found
        /// </summary>
        private void Setup_Part2()
        {

            GetGraphics();

            if (!UI_ForceSleepButtonPath.NullOrEmpty())
                UI_ForceSleepButton = ContentFinder<Texture2D>.Get(UI_ForceSleepButtonPath, true);

        }

        public override void ExposeData()
        {
            base.ExposeData();

            ReadXMLData();

            //LongEventHandler.ExecuteWhenFinished(Setup_Part2);
        }

        #endregion

        #region Graphics

        public override Graphic Graphic
        {
            get
            {
                if (!ForPrisoners)
                {
                    if (Medical)
                    {
                        //return SecondaryMaterial;
                        if (MedicalGraphic == null)
                        {
                            GetGraphics();
                            if (MedicalGraphic == null)
                                return base.Graphic;
                        }

                        return MedicalGraphic;
                    }
                    else
                    {
                        if (PrimaryGraphic == null)
                        {
                            GetGraphics();
                            if (PrimaryGraphic == null)
                                return base.Graphic;
                        }

                        return PrimaryGraphic; //base.DrawMat(rot);
                    }
                }
                else
                {
                    if (Medical)
                    {
                        //return SecondaryMaterial;
                        if (MedicalSecondaryGraphic == null)
                        {
                            GetGraphics();
                            if (MedicalSecondaryGraphic == null)
                                return base.Graphic;
                        }

                        return MedicalSecondaryGraphic;
                    }
                    else
                    {
                        //return SecondaryMaterial;
                        if (SecondaryGraphic == null)
                        {
                            GetGraphics();
                            if (SecondaryGraphic == null)
                                return base.Graphic;
                        }

                        return SecondaryGraphic;
                    }
                }
            }
        }

        #endregion

        #region Ticker

        public override void Tick()
        {
            base.Tick();
        }

        #endregion

        #region Inspections

        public override string GetInspectString()
        {
            return TrimStartNewlines( base.GetInspectString().TrimEndNewlines() );
        }
        public string TrimStartNewlines(string s)
        {
            return s.TrimStart(new char[]
            {
        '\r',
        '\n'
            });
        }

        /// <summary>
        /// This creates new selection buttons with a new graphic
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (!UI_ForceSleepButtonPath.NullOrEmpty())
                UI_ForceSleepButton = ContentFinder<Texture2D>.Get(UI_ForceSleepButtonPath, true);

            // Add base.GetCommands()
            List<Gizmo> baseList = base.GetGizmos().ToList();
            for (int i = 0; i < baseList.Count; i++)
                yield return baseList[i];


            int groupBaseKey = 31367670;

            // Key-Binding B - Call owner To rest
            Command_Action opt1;
            opt1 = new Command_Action();
            opt1.disabled = this.owners == null;
            opt1.disabledReason = txtNoOwner.Translate();
            opt1.defaultDesc = txtSendOwnerToSleep.Translate();
            opt1.icon = UI_ForceSleepButton;
            opt1.hotKey = KeyBindingDefOf.Misc1;
            opt1.activateSound = SoundDef.Named("Click");
            opt1.action = Button_CallOwnerToRecharge;
            opt1.groupKey = groupBaseKey + 1;
            yield return opt1;

        }


        public void Button_CallOwnerToRecharge()
        {
            if (owners == null)
                return;

            DoWork_ForceOwnerToSleep();
        }

        #endregion

        #region Functions

        private void DoWork_ForceOwnerToSleep()
        {

            for (int i = 0; i < owners.Count; i++)
            {

                // preparation: stop all other jobs
                if (owners[i].jobs != null)
                    owners[i].jobs.StopAll();

                // Do Job with JobGiver  -> Force owner to sleep
                JobGiver_GetRest getRest = new JobGiver_GetRest();
                JobIssueParams jobParms = new JobIssueParams();
                ThinkResult thinkResult = getRest.TryIssueJobPackage(owners[i], jobParms);
                owners[i].jobs.StartJob(thinkResult.Job);



                // Alternate: If you want to send the pawn somewhere...

                //// Do Job with JobDriver  -> Call owner to bed
                //JobDriver_GotoTarget jobDriver = new JobDriver_GotoTarget(owner); 
                //// Alternate: (JobDriver_GotoTarget)MakeDriver(owner, typeof(JobDriver_GotoTarget));
                //jobDriver.TargetCell = Position;
                //jobDriver.StartJob();
            }
        }

        

        // Find Hopper next to this
        public IEnumerable<Building> AllAdjacentHopper
        {
            get
            {
                ThingDef thingDef = DefDatabase<ThingDef>.GetNamed(HopperDefName, false);
                if (thingDef == null)
                    yield break;

                List<IntVec3> intVec3s = AdjCellsCardinal;
                for (int i = 0; i < intVec3s.Count; i++)
                {
                    IntVec3 cell = intVec3s[i];
                    List<Thing> things = Map.thingGrid.ThingsListAt(cell);

                    for (int j = 0; j < things.Count; j++)
                    {
                        Thing thing = things[j];

                        if (thing == this)
                            continue;

                        if (thing.def != thingDef)
                            continue;

                        Building building = thing as Building;
                        if (building != null)
                            yield return building;

                    }
                }
            }
        }
        // Find Item in all Hoppers
        public IEnumerable<Thing> AllItemsInHopper
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
                        if (item.def.category == ThingCategory.Item)
                        {
                            thing = item;
                        }
                        if (item.def == ThingDef.Named(HopperDefName))
                        {
                            thing1 = item;
                        }
                    }

                    if (thing != null && thing1 != null)
                    {
                        yield return thing;
                    }
                }
            }
        }
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

        #endregion

    }
}
