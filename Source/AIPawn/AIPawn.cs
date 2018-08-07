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
    /// This is the pawn class for mai.
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>For usage of this code, please look at the license information.</permission>
    [StaticConstructorOnStartup]
    public class AIPawn : Pawn
    {
        private int refreshBaseInfosCount = 999999;
        private int refreshQuickCount;

        private int refreshBaseInfosMax = 500; //2000;
        private int refreshQuickMax = 25;

        private int incapToExplosionCounterStartValue = 1200;
        private int incapToExplosionCounter;
        private float incapHealthOld = 99999f;

        private int healDamagedPartsCounterStartValue = 630;
        private int healDamagedPartsCounter;

        private int fixRestWhenInBedCounter = 30;
        private float fixRestWhenInBedFixedValue = 1.0f;

        private bool loaded = false;
        //public Pawn_Ownership ai_ownership;

        // holder for the grafics of the head and body
        public string normalHeadGraphicPathMulti = "Things/Pawns/Female_Average_MiaHead";
        public string draftedHeadGraphicPathMulti = "Things/Pawns/Female_Average_MiaHead";
        public string draftedBodyGraphicPathMulti = "Things/Pawns/Drafted_Mia";

        public static Graphic nakedHeadGraphic;
        public static Graphic nakedBodyGraphic;
        public static Graphic nakedHeadGraphicHue;
        public static Graphic nakedBodyGraphicHue;

        public static Graphic draftedHeadGraphic;
        public static Graphic draftedBodyGraphic;
        public static Graphic draftedHeadGraphicHue;
        public static Graphic draftedBodyGraphicHue;

        public static Graphic hairGraphic;

        public bool graphicHueActive = false;
        private bool draftedActiveOld = false;

        // Init needed to remove the new backstories after they are applied
        private bool init = false;
        private int initTicks = 5;

        private string destroyOne_ThingDefName = "AIPawn_Inactive";
        private bool enhancedAI = false;
        private string thingDefName_KilledLeaving = "Plasteel";
        //private string thingDefName_AICore = "AIPersonaCore";

        public string txtFloatMenuGotoBed = "AIPawn_FloatMenu_GotoBed";
        public string txtFloatMenuSedateAndRescueAIPawn = "AIPawn_FloatMenu_SedateAndRescueAIPawn";
        public string txtFloatMenuSedateAIPawn = "AIPawn_FloatMenu_SedateAIPawn";

        private bool isAnestheticIncap = false;

        private int neededMaterialPerRecreatedBodyPart = 30;

        public bool destroyMeWithoutExplosion = false;

        //private Job curJobOld;

        // ================== Load/Save ==================

        // Get the data from the extended def
        private void ReadXmlData()
        {
            ThingDef_AIPawn def2 = (ThingDef_AIPawn)def;

            if (!def2.normalHeadGraphicPathMulti.NullOrEmpty())
            {
                normalHeadGraphicPathMulti = def2.normalHeadGraphicPathMulti;
                draftedHeadGraphicPathMulti = def2.draftedHeadGraphicPathMulti;
                draftedBodyGraphicPathMulti = def2.draftedBodyGraphicPathMulti;
                refreshBaseInfosMax = def2.refreshBaseInfosMax;
                refreshQuickMax = def2.refreshQuickMax;
                incapToExplosionCounterStartValue = def2.incapToExplosionCounter;
                enhancedAI = def2.enhancedAI;
            }
        }

        /// <summary>
        /// To write and read data (savegame)
        /// </summary>
        public override void ExposeData()
        {
            BackstoryHelper.AddNewBackstoriesToDatabase();

            base.ExposeData();
            Scribe_Values.Look<int>(ref refreshBaseInfosCount, "RefreshBaseInfos");
            Scribe_Values.Look<int>(ref refreshQuickCount, "RefreshQuickInfos");

            draftedActiveOld = this.Drafted;

            loaded = true;
        }



        // ================== Create / Destroy ==================

        /// <summary>
        /// Do something after the object is initialized, but before it is spawned
        /// </summary>
        public override void PostMake()
        {
            base.PostMake();
        }

        /// <summary>
        /// Do something after the object is spawned
        /// </summary>
        public override void SpawnSetup(Map map, bool respawnAfterLoad)
        {
            BackstoryHelper.AddNewBackstoriesToDatabase();

            base.SpawnSetup(map, respawnAfterLoad);

            LongEventHandler.ExecuteWhenFinished(SpawnSetup_Part2);

        }

        /// <summary>
        /// This is called seperately when the Mod-Thread is done.
        /// It is needed to be seperately from SpawnSetup, so that the graphics can be found
        /// </summary>
        private void SpawnSetup_Part2()
        {

            // Get the data from the extended def
            ReadXmlData();

            // Load base graphics
            Color colorNormal = Color.white;
            nakedHeadGraphic = GraphicDatabase.Get<Graphic_Multi>(normalHeadGraphicPathMulti, ShaderDatabase.Cutout, Vector2.one, colorNormal);
            draftedHeadGraphic = GraphicDatabase.Get<Graphic_Multi>(draftedHeadGraphicPathMulti, ShaderDatabase.Cutout, Vector2.one, colorNormal);
            nakedBodyGraphic = GraphicDatabase.Get<Graphic_Multi>(kindDef.lifeStages[0].bodyGraphicData.texPath, ShaderDatabase.Cutout, Vector2.one, colorNormal);
            draftedBodyGraphic = GraphicDatabase.Get<Graphic_Multi>(draftedBodyGraphicPathMulti, ShaderDatabase.Cutout, Vector2.one, colorNormal);

            // Load hue graphics
            Color colorHue = Color.Lerp(Color.white, Color.red, 0.30f);
            nakedHeadGraphicHue = GraphicDatabase.Get<Graphic_Multi>(normalHeadGraphicPathMulti, ShaderDatabase.Cutout, Vector2.one, colorHue);
            draftedHeadGraphicHue = GraphicDatabase.Get<Graphic_Multi>(draftedHeadGraphicPathMulti, ShaderDatabase.Cutout, Vector2.one, colorHue);
            nakedBodyGraphicHue = GraphicDatabase.Get<Graphic_Multi>(kindDef.lifeStages[0].bodyGraphicData.texPath, ShaderDatabase.Cutout, Vector2.one, colorHue);
            draftedBodyGraphicHue = GraphicDatabase.Get<Graphic_Multi>(draftedBodyGraphicPathMulti, ShaderDatabase.Cutout, Vector2.one, colorHue);


            // Load hair graphic (shaved)
            hairGraphic = GraphicDatabase.Get<Graphic_Multi>(this.story.hairDef.texPath, ShaderDatabase.Cutout, Vector2.one, this.story.hairColor);

            //Drawer.renderer.graphics.ResolveAllGraphics(); 
            UpdateGraphics();

            draftedActiveOld = this.Drafted;

            // Set Incap Counter to base value
            incapToExplosionCounter = incapToExplosionCounterStartValue;

            refreshBaseInfosCount = -1000;

            // Delete the creating building
            if (!loaded)
                DeleteCreator();
           
        }

        /// <summary>
        /// Clean up when it is destroyed
        /// </summary>
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (Map == null)
            {
                base.Destroy(DestroyMode.Vanish);
                return;
            }


            // Check for in bed
            bool isInBed = IsInBed(Map);

            bool notConscious = false;
            if (health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) < 0.02f)
                notConscious = true;

            // No explosion while under anesthetic or shutdown
            bool doExplosion = true;
            if (!isAnestheticIncap && !isInBed && !notConscious)
                doExplosion = false;

            Map lastMap = this.Map;
            IntVec3 lastPosition = this.Position;

            if (!Destroyed)
            {
                base.Destroy(DestroyMode.Vanish);
            }
            else
            {
                // Is already destroyed? 
            }

            if (lastMap == null)
                return;

            // If destroyed go with an explosion
            if (!destroyMeWithoutExplosion)
            {

                // No explosion while under anesthetic or shutdown
                if (doExplosion)
                    GenExplosion.DoExplosion(lastPosition, lastMap, 2.4f, DamageDefOf.Bomb, this);

                if (enhancedAI)
                {
                    Thing thing = ThingMaker.MakeThing(ThingDef.Named(thingDefName_KilledLeaving), null);
                    GenPlace.TryPlaceThing(thing, lastPosition, lastMap, ThingPlaceMode.Near, out thing);
                    thing.stackCount = Rand.RangeInclusive(10, 50);
                    thing.SetForbidden(true);
                }
                else
                {
                    Thing thing = ThingMaker.MakeThing(ThingDef.Named(thingDefName_KilledLeaving), null);
                    GenPlace.TryPlaceThing(thing, lastPosition, lastMap, ThingPlaceMode.Near, out thing);
                    thing.stackCount = Rand.RangeInclusive(5, 40);
                    thing.SetForbidden(true);
                }
            }
        }

        /// <summary>
        /// Delete the creator
        /// </summary>
        private void DeleteCreator()
        {
            string destroyThingName = destroyOne_ThingDefName;

            IEnumerable<Thing> buildings = Map.listerThings.ThingsOfDef(ThingDef.Named(destroyThingName));

            if (buildings != null && buildings.Count() > 0)
            {
                Thing building = buildings.ElementAt(0);

                // find nearest
                double distance = 999999d;
                foreach (Thing b in buildings)
                {
                    double workDistance = GetDistance(Position, b.Position);
                    if (workDistance < distance)
                    {
                        distance = workDistance;
                        building = b;
                    }
                }

                building.Destroy(DestroyMode.Vanish);

                // Only destroy one!
                return;
            }
        }

        /// <summary>
        /// Sets the new head and body grafic
        /// </summary>
        private void UpdateGraphics()
        {
            if (nakedHeadGraphic == null)
                return;

            // reset Hair Graphic
            if (Drawer.renderer.graphics.hairGraphic == null)
                Drawer.renderer.graphics.hairGraphic = hairGraphic;

            // Drafted
            if (this.Drafted)
            {
                if (!graphicHueActive)
                {
                    Drawer.renderer.graphics.headGraphic = draftedHeadGraphic;
                    Drawer.renderer.graphics.nakedGraphic = draftedBodyGraphic;
                }
                else
                {
                    Drawer.renderer.graphics.headGraphic = draftedHeadGraphicHue;
                    Drawer.renderer.graphics.nakedGraphic = draftedBodyGraphicHue;
                }
            }
            else // Not Drafted
            {
                if (!graphicHueActive)
                {
                    Drawer.renderer.graphics.headGraphic = nakedHeadGraphic;
                    Drawer.renderer.graphics.nakedGraphic = nakedBodyGraphic;
                }
                else
                {
                    Drawer.renderer.graphics.headGraphic = nakedHeadGraphicHue;
                    Drawer.renderer.graphics.nakedGraphic = nakedBodyGraphicHue;
                }
            }
        }



        // ================== Ticks ==================

        public override void Tick()
        {

            base.Tick();

            // Do init work after spawning
            if (!init)
            {
                HelperAIPawn.ReApplyThingToListerThings(this.Position, this);

                Drawer.renderer.graphics.ResolveAllGraphics(); //Causes errors!
                UpdateGraphics();

                initTicks--;
                if (initTicks <= 0)
                {
                    // Replace invalid Mai with valid Mai
                    if (!story.childhood.identifier.Contains(BackstoryHelper.BackstoryDefNameIdentifier))
                    {
                        string savedDefName = def.defName;
                        IntVec3 savedPosition = Position;
                        Map savedMap = Map;
                        Gender savedGender = this.gender;

                        // Destroy me
                        destroyMeWithoutExplosion = true;
                        Destroy(DestroyMode.Vanish);

                        // Create a new me
                        AIPawn mai = Building_AIPawnCreator.CreateAIPawn(savedDefName, savedPosition, savedMap, savedGender);

                        return;
                    }

                    BackstoryHelper.RemoveNewBackstoriesFromDatabase();
                    init = true;
                }
                return;
            }

            //// To circumvent the strange pathing error!
            //if ((this.CurJob == null || this.CurJob != curJobOld) && Map != null)
            //{
            //    HelperAIPawn.ReApplyThingToListerThings(this.Position, this);
            //    curJobOld = this.CurJob;
            //}

            // To circumvent the destroy apparel error
            if (this.CurJob != null && this.CurJob.def.defName == "Wear" && this.CurJob.targetA.Cell == this.Position)
            {
                AIPawnGenerator.DestroyBaseShielding(this);
            }

            // Update drafted graphics
            if (draftedActiveOld != this.Drafted)
            {
                UpdateGraphics();
                draftedActiveOld = this.Drafted;
            }

            // When AIPawn is in a Container, do nothing
            if ( this.InContainerEnclosed )
                return;

            // When AIPawn is in a bed, fix rest value to prevent explosions in the hospital room
            if (Find.TickManager.TicksGame % fixRestWhenInBedCounter == 0)
            {
                Building_AIPawnRechargeStation rechargeStation;
                if (this.InBed() && !this.IsInAIRechargeStation(Map, out rechargeStation))
                {
                    needs.rest.CurLevel = fixRestWhenInBedFixedValue;
                }
                else
                {
                    fixRestWhenInBedFixedValue = needs.rest.CurLevel;
                }
            }

            //// Disable food reduction
            //if (needs != null && needs.food != null && needs.food.CurLevel <= 0.95f)
            //    needs.food.CurLevel = 1.0f;

            //// Disable joy reduction
            //if (needs != null && needs.joy != null && needs.joy.CurLevel <= 0.95f)
            //    needs.joy.CurLevel = 1.0f;

            //// Disable beauty reduction
            //if (needs != null && needs.beauty != null && needs.beauty.CurLevel <= 0.95f)
            //    needs.beauty.CurLevel = 1.0f;

            //// Disable comfort reduction
            //if (needs != null && needs.comfort != null && needs.comfort.CurLevel <= 0.95f)
            //    needs.comfort.CurLevel = 1.0f;

            //// Disable rest reduction when traveling in a caravan!
            //if (needs != null && needs.rest != null && needs.rest.CurLevel <= 0.95f && !Destroyed && !Downed && Map == null)
            //    needs.rest.CurLevel += 0.01f / 60f;

            // Self healing ability (Nanobots)
            healDamagedPartsCounter -= 1;
            if (healDamagedPartsCounter <= 0)
            {
                DoHealDamagedBodyPart(enhancedAI);
                healDamagedPartsCounter = healDamagedPartsCounterStartValue;
            }

            refreshQuickCount += 1;
            if (refreshQuickCount >= refreshQuickMax)
                refreshQuickCount = 0;

            if (refreshQuickCount == 0)
            {
                // Add thought when health < x
                if (this.health.summaryHealth.SummaryHealthPercent < 0.75)
                    this.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("AIShieldingError"));

                // disable diseases
                DoDiseaseHandling();

                if (Destroyed)
                {
                    destroyMeWithoutExplosion = true;
                    Destroy(DestroyMode.Vanish);
                    return;
                }

                // Do explosion when downed, dead or incapable of moving
                if (Dead || (!isAnestheticIncap && !IsInBed(Map) && (Downed || health.capacities.GetLevel(PawnCapacityDefOf.Moving) < 0.1f)))
                {
                    incapToExplosionCounter -= refreshQuickMax;
                    if (incapToExplosionCounter <= 0)
                    {
                        Destroy(DestroyMode.KillFinalize);
                        return;
                    }
                }

                // Reset counter when health is rising -- Not working right now
                if ((Downed && this.health.summaryHealth.SummaryHealthPercent > incapHealthOld) || !Downed && (incapToExplosionCounter < incapToExplosionCounterStartValue))
                    incapToExplosionCounter = incapToExplosionCounterStartValue;

                incapHealthOld = this.health.summaryHealth.SummaryHealthPercent;

                //// Add thought when rested
                //if (rest.Rest.CurLevel >= 98.9f)
                //    psychology.thoughts.memories.TryGainMemoryThought(ThoughtDef.Named("AIBatteriesRefilled"));

                // Change color when near exhausted
                float levelLowBatterie = 0.25f;
                float levelLowCriticalBatterie = 0.15f;
                if (needs.rest.CurLevel <= levelLowCriticalBatterie && !graphicHueActive)
                {
                    // Switch to alternate graphic
                    graphicHueActive = true;
                    UpdateGraphics();
                }
                else if (needs.rest.CurLevel > levelLowCriticalBatterie && graphicHueActive)
                {
                    // Switch back to normal graphic
                    graphicHueActive = false;
                    UpdateGraphics();
                }

                // Add thought when rest(batteries) < x
                if (needs.rest.CurLevel < levelLowCriticalBatterie)
                    needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("AILowCriticalBattery"));
                // Add thought when rest(batteries) < x
                else if (needs.rest.CurLevel < levelLowBatterie)
                    needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("AILowBattery"));

                // Explosion when exhausted
                if (needs.rest.CurLevel <= 0.011f)
                {
                    if (!Downed)
                        HealthUtility.DamageUntilDowned(this);
                }
                else if (needs.rest.CurLevel < 0.2f)
                    TryGoRecharging();

                // unclaim bed when fully rested // Activate? Don't Activate?
                //if (ownership != null && (rest.Rest.CurLevel >= 94.8f || rest.DoneResting))
                //    ownership.UnclaimBed();
            }

            // Update base AI thought every x
            refreshBaseInfosCount += 1;
            if (refreshBaseInfosCount >= refreshBaseInfosMax || refreshBaseInfosCount < 0)
            {
                refreshBaseInfosCount = 0;
                this.mindState.canFleeIndividual = false;

                DoRelationHandling();

                //Base thought is only available after save/load (why?) so it needs to be set
                this.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("AIBaseThought"));
                
                // Rebuild Base Shielding if needed
                Building_AIPawnRechargeStation rs2;
                AIPawnGenerator.GiveBaseApparelWhileInBed(this, IsInAIRechargeStation(Map, out rs2));
            }
        }

        
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            //// No base float menu options allowed!
            //foreach (FloatMenuOption fmo in base.GetFloatMenuOptionsFor(myPawn))
            //{
            //    yield return fmo;
            //}

            // No self medication!
            if (myPawn == this)
                yield break;

            // Sedate AIPawn
            Action action_OrderSedateMai = delegate
            {
                myPawn.drafter.Drafted = false;

                Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("AIPawn_Sedate"), this)
                {
                    count = 1
                };
                myPawn.jobs.TryTakeOrderedJob(jobNew);
            };
            yield return new FloatMenuOption(txtFloatMenuSedateAIPawn.Translate(new object[] { this.Label }), action_OrderSedateMai);


            // Sedate and Carry AIPawn to recharge station
            Building_AIPawnRechargeStation rechargeStation;
            rechargeStation = HelperAIPawn.FindMedicalRechargeStationFor(this);
            if (rechargeStation == null)
                rechargeStation = HelperAIPawn.FindRechargeStationFor(this);

            Action action_OrderSedateAndCarryMai = delegate
            {
                myPawn.drafter.Drafted = false;

                Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("AIPawn_SedateAndRescue"), this, rechargeStation)
                {
                    count = 1
                };
                myPawn.jobs.TryTakeOrderedJob(jobNew);
            };

            if (rechargeStation != null)
                yield return new FloatMenuOption(txtFloatMenuSedateAndRescueAIPawn.Translate(new object[] {this.Label}), action_OrderSedateAndCarryMai);
            else
                yield return new FloatMenuOption(txtFloatMenuSedateAndRescueAIPawn.Translate(new object[] {this.Label}), null);

            yield break;

        }


        private void DoRelationHandling()
        {
            // Disabled for MAI?
            //this.relations.ClearAllRelations();
       

            //Log.Error("All relations cleared...");
        }


        // Disable the possibility for diseases to be a thread
        private void DoDiseaseHandling()
        {

            IEnumerable<Hediff> diseases = health.hediffSet.GetTendableNonInjuryNonMissingPartHediffs();
            if (diseases != null && diseases.Count() > 0)
            {
                foreach (Hediff disease in diseases)
                {
                    disease.Heal(100.0f);
                }
            }

            bool anesthetic = false;
            IEnumerable<HediffWithComps> hediffs = health.hediffSet.GetHediffs<HediffWithComps>();
            if (hediffs != null && hediffs.Count() > 0)
            {
                foreach (HediffWithComps hediff in hediffs)
                {
                    if (hediff.def == HediffDefOf.Anesthetic)
                    {
                        anesthetic = true;
                        break;
                    }
                }
            }

            isAnestheticIncap = anesthetic;
        }



        // Self healing
        private void DoHealDamagedBodyPart(bool enhancedAI)
        {
            IEnumerable<Hediff> notTendedBodyParts = (from x in this.health.hediffSet.hediffs
                                                              where (x is Hediff_Injury) && x.TendableNow(false) //!(x is Hediff_MissingPart) && x.CanHealFromTending() && !x.IsTended()
                                                      select x);

            IEnumerable<Hediff_Injury> hediff_injuries = (from x in this.health.hediffSet.GetHediffs<Hediff_Injury>()
                                                          where x.CanHealFromTending() || x.CanHealNaturally()
                                                          select x);

            if (notTendedBodyParts.Count() == 0 && hediff_injuries.Count() == 0)
            {
                // Nothing to heal, check for missing body parts next
                DoHealMissingBodyPart(enhancedAI);
                return;
            }


            // Apply Treated, but not healing!
            if (this.health.HasHediffsNeedingTend(false))
            {
                float quality = (enhancedAI ? 1.0f : Rand.Value);
                int batchPosition = 0;
                foreach (Hediff_Injury injury in from x in this.health.hediffSet.GetInjuriesTendable()
                                                 orderby x.Severity descending
                                                 select x)
                {
                    injury.Tended(quality, batchPosition);
                    batchPosition++;
                    if (batchPosition >= (IsInBed(Map) ? 3 : 1))
                        break;
                }
            }

            // No additional healing while in bed (it heals by itself here)
            if (IsInBed(Map))
                return;


            if (hediff_injuries != null && hediff_injuries.Count() > 0)
            {
                Hediff_Injury hediff_Injury2 = hediff_injuries.RandomElement<Hediff_Injury>();

                float tendQuality = hediff_Injury2.TryGetComp<HediffComp_TendDuration>().tendQuality;
                float num2 = GenMath.LerpDouble(0f, 1f, 0.5f, 1.5f, Mathf.Clamp01(tendQuality));
                hediff_Injury2.Heal(22f * num2 * this.HealthScale * 0.01f * (enhancedAI ? 2 : 1));
            }
        }

        // supported self healing of missing limbs
        private void DoHealMissingBodyPart(bool enhancedAI)
        {

            List<Hediff_MissingPart> missingBodyParts = GetMissingBodyparts();
            if (missingBodyParts == null || missingBodyParts.Count == 0)
                return;

            // 50% chance that a missing limb will be healed
            if (Rand.Value < 0.5f)
                return;

            Building_AIPawnRechargeStation rechargeStation;
            if (IsInAIRechargeStation(Map, out rechargeStation))
            {

                IEnumerable<Thing> things = rechargeStation.AllItemsInHopper;
                if (things == null || things.Count() == 0)
                    return;

                // Get count of all items
                int stackCountAll = 0;
                for (int i = 0; i < things.Count(); i++)
                {
                    stackCountAll += things.ElementAt(i).stackCount;
                }
                if (stackCountAll < neededMaterialPerRecreatedBodyPart)
                    return;

                // Enough material available, reduce it
                int neededAmmount = neededMaterialPerRecreatedBodyPart;
                for (int i = 0; i < things.Count(); i++)
                {
                    neededAmmount -= things.ElementAt(i).stackCount;
                    if (neededAmmount >= 0)
                        things.ElementAt(i).Destroy();
                    else
                        things.ElementAt(i).stackCount -= neededMaterialPerRecreatedBodyPart;

                    if (neededAmmount <= 0)
                        break;
                }

                // Get random part
                Hediff_MissingPart missingBodyPart = missingBodyParts.RandomElement();
                
                //Log.Error("Restoring missing part..");

                // restore random part
                BodyPartRecord part = missingBodyPart.Part;
                this.health.RestorePart(part, null, true);

                // Do damage to restored part, so that the health of it is 1
                float maxDamage = health.hediffSet.GetPartHealth(part);
                int applyDamage = (int)maxDamage / 2;
                this.TakeDamage(new DamageInfo(DamageDefOf.Cut, applyDamage, 0f, -1f, null, part, null));
            }
        }


        public List<Hediff_MissingPart> GetMissingBodyparts()
        {

            List<Hediff_MissingPart> missingParts = new List<Hediff_MissingPart>();

            List<Hediff> hedifflist = health.hediffSet.hediffs;

            for (int i = 0; i < hedifflist.Count; i++)
            {
                if (hedifflist[i] is Hediff_MissingPart)
                    missingParts.Add((Hediff_MissingPart)hedifflist[i]);
            }

            if (missingParts.Count == 0)
                return null;

            return missingParts;
        }

        private void TryGoRecharging()
        {
            // Drafting active, do nothing
            if (this.Drafted || !this.Spawned)
                return;

            // No recharge station, do nothing
            if (this.ownership.OwnedBed == null)
            {

                Building_AIPawnRechargeStation rs = HelperAIPawn.FindRechargeStationFor(this);
                if (rs == null)
                    return;

                rs.TryAssignPawn(this);

                if (this.ownership.OwnedBed == null)
                    return;
            }

            Building_AIPawnRechargeStation rechargeStation = this.ownership.OwnedBed as Building_AIPawnRechargeStation;
            if (rechargeStation == null)
                return;

            if (rechargeStation.Position == this.Position)
                return;

            rechargeStation.Button_CallOwnerToRecharge();
        }


        /// <summary>
        /// Get the distance between two points
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        private double GetDistance(IntVec3 p1, IntVec3 p2)
        {
            int X = Math.Abs(p1.x - p2.x);
            int Y = Math.Abs(p1.y - p2.y);
            int Z = Math.Abs(p1.z - p2.z);

            return Math.Sqrt(X * X + Y * Y + Z * Z);

        }

        // Check if sleeping in Bed right now
        private bool IsInBed(Map map)
        {
            if (map == null)
                return false;

            Building_Bed bed = map.thingGrid.ThingAt<Building_Bed>(Position);
            return (bed != null);
        }
        // Check if sleeping in Recharge Station right now
        private bool IsInAIRechargeStation(Map map, out Building_AIPawnRechargeStation rechargeStation)
        {
            if (map == null)
            {
                rechargeStation = null;
                return false;
            }

            rechargeStation = map.thingGrid.ThingAt<Building_AIPawnRechargeStation>(Position);
            return (rechargeStation != null);
        }


    }
}
