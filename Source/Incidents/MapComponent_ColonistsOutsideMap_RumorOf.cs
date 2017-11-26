using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;

//using CommonMisc;

namespace Incidents
{

    public class MapComponent_ColonistsOutsideMap_RumorOf : MapComponent
    {

        #region variables

        public bool _active = false;
        public bool Active
        {
            get
            {
                return _active;
            }
            set
            {
                // Something tried to deactivate this, but there are still pawns out of the map -> reactivate this!
                if (value == false && PawnsOutOfMap != null && PawnsOutOfMap.Count > 0)
                {
                    Log.Warning("RumorOf: Tried to set active to false, but there are still pawns out of the map!");
                    _active = true;
                    return;
                }

                _active = value;
            }
        }

        public List<Thing> PawnsOutOfMap = new List<Thing>();
        public IntVec3 ExitMapCell = IntVec3.Invalid;
        public float IncidentPoints;
        private IntVec3 ReturnToBaseCell = IntVec3.Invalid;

        public CustomRumorOfDef def = null;

        private int returnTicks = -1;

        private float pointMultiplier = 1.35f;
        private float incidentPointsMin = 50.0f;
        public int autoDeactivateTicks;

        public bool DialogDelayActive = false;
        public int DialogDelayTicks = -1;


        public float TraveltimeInDays
        {
            get
            {
                return GenDate.TicksToDays(returnTicks);
            }
        }



        #endregion


        #region load / save

        public override void ExposeData()
        {
            Scribe_Values.LookValue<bool>(ref this.DialogDelayActive, "DialogDelayActive", false, false);
            Scribe_Values.LookValue<bool>(ref this._active, "Active", true, true);
            Scribe_Values.LookValue<IntVec3>(ref this.ExitMapCell, "ExitMapCell", IntVec3.Invalid, false);
            Scribe_Values.LookValue<float>(ref this.IncidentPoints, "Points", incidentPointsMin, false);
            Scribe_Values.LookValue<int>(ref this.returnTicks, "returnTicks", -1, false);
            Scribe_Values.LookValue<int>(ref this.autoDeactivateTicks, "autoDeactivateTicks", 0, false);
            Scribe_Collections.LookList<Thing>(ref this.PawnsOutOfMap, "PawnsOutOfMap", LookMode.Deep, null);
            Scribe_Defs.LookDef<CustomRumorOfDef>(ref this.def, "def");

            if (PawnsOutOfMap == null)
                PawnsOutOfMap = new List<Thing>();

            //if (Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.PostLoadInit)
            //    autoDeactivateTicks = GenDate.TicksPerDay;
        }

        #endregion


        #region MapComponent Tick Handling

        public override void MapComponentTick()
        {
            // Read settings from xml file 
            DoUpdateDef();

            // Check Active status
            DoActiveCheck();

            DoDialogDelayHandling();
            //DoPawnAwayHandling();
            DoPawnReturningHandling();
        }

        #endregion


        #region various functions

        private void DoDialogDelayHandling()
        {
            if (!DialogDelayActive)
                return;


            if (DialogDelayTicks < 0)
                DialogDelayTicks = GenDate.TicksPerHour * 6;

            else if (DialogDelayTicks > 0)
                DialogDelayTicks--;

            else //if (dialogDelayTicks == 0)
            {
                // Create colonists selection dialog
                Dialog_RumorOf_AssignColonists.CreateColonistSelectionDialog();

                DialogDelayActive = false;
                DialogDelayTicks = -1;
            }

        }

        public void DoUpdateDef()
        {
            if (def != null)
                return;

            if (!DefDatabase<CustomRumorOfDef>.AllDefs.TryRandomElementByWeight(def => def.occureChance, out def))
                def = null;

            //Log.Error("Count of defs: " + DefDatabase<CustomRumorOfDef>.AllDefs.Count());
        }

        private void DoActiveCheck()
        {
            if (!Active)
            {
                if (autoDeactivateTicks <= GenDate.TicksPerDay)
                    autoDeactivateTicks = GenDate.TicksPerDay * 2; // => 2 days

                return;
            }

            if (PawnsOutOfMap == null || PawnsOutOfMap.Count == 0)
            {
                // Deactivate incident if there is no pawn available after one day
                // This is done to make it available again after x Ticks of no pawns
                autoDeactivateTicks--;
                if (autoDeactivateTicks <= 0)
                    Active = false;

                return;
            }
        }

        //private void DoPawnAwayHandling()
        //{
        //    if (!Active)
        //        return;

        //    if (PawnsOutOfMap == null || PawnsOutOfMap.Count == 0)
        //        return;

        //    Dialog_RumorOf_Fight.CreateThisDialog();


        //}

        private void DoPawnReturningHandling()
        {
            if (!Active)
                return;

            if (PawnsOutOfMap == null || PawnsOutOfMap.Count == 0)
                return;

            // Set the time the pawns will be gone, if not already set
            if (returnTicks < 0)
                SetNewTravelTime();

            returnTicks--;
            if (returnTicks > 0)
                return;


            // returnTicks == 0 -> Do this:

            if (IncidentPoints < incidentPointsMin)
                IncidentPoints = incidentPointsMin;

            if (GatherSpotLister.activeSpots.Count > 0)
            {
                // Find a gather spot not inside a prison cell
                IntVec3 GatherSpotCell = IntVec3.Invalid;
                int n = 0;
                while (n < 25)
                {
                    CompGatherSpot gs = GatherSpotLister.activeSpots.RandomElement();
                    GatherSpotCell = gs.parent.Position;

                    if (GatherSpotCell.IsInPrisonCell())
                        GatherSpotCell = IntVec3.Invalid;

                    if (GatherSpotCell != IntVec3.Invalid)
                    {
                        ReturnToBaseCell = GatherSpotCell;
                        break;
                    }

                    n++;
                }
            }

            //// Nothing -> Try TradeDropSpot
            //if (ReturnToBaseCell == IntVec3.Invalid)
            //    ReturnToBaseCell = RCellFinder.TradeDropSpot();
            //Nothing -> Try Colonist Position
            if (ReturnToBaseCell == IntVec3.Invalid)
                ReturnToBaseCell = Find.MapPawns.FreeColonistsSpawned.RandomElement().Position;


            // Infos for return message
            bool treasureFound = false;
            Pawn treasureCarrier = null;
            bool enemyPursuit = false;
            List<Thing> treasures = new List<Thing>();

            // Calculate a value of the weapons taken with them. Result may reduce the spawn enemy chance
            float weaponPoints = 0f;
            for (int i = 0; i < PawnsOutOfMap.Count; i++)
            {
                Pawn weaponPawn = PawnsOutOfMap[i] as Pawn;
                if (weaponPawn == null || weaponPawn.Destroyed || weaponPawn.Spawned)
                    continue;

                weaponPoints += CalcVerbValue(weaponPawn.TryGetAttackVerb(true)); // Result: between 5 and 25
            }


            // spawn the returning pawns
            float remainingPoints = IncidentPoints;

            if (GenDate.YearsPassed >= 2)
                remainingPoints = remainingPoints * GenDate.YearsPassed / 3;

            IntVec3 spawnCell;
            Predicate<IntVec3> predicateSpawnCell = (IntVec3 c) =>
            {
                if (!c.Standable() || !c.CanReachColony())
                    return false;

                return true;
            };

            bool allowTreasure = Rand.RangeInclusive(1, 100) < this.def.treasureChance;
            if (!allowTreasure)
                remainingPoints = 0;

            for (int i = 0; i < PawnsOutOfMap.Count; i++)
            {
                Pawn retPawn = PawnsOutOfMap[i] as Pawn;
                if (retPawn == null || retPawn.Destroyed || retPawn.Spawned)
                    continue;

                CellFinder.TryFindRandomReachableCellNear(ExitMapCell, 13, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false), predicateSpawnCell, null, out spawnCell);

                retPawn = GenSpawn.Spawn(retPawn, spawnCell) as Pawn;

                if (retPawn == null)
                    continue;

                Thing treasure = null;

                //Create the treasure (as long as there are points remaining) and carry it back
                if (remainingPoints > 0)
                    treasure = TryGetTreasure();

                // Treasure found: Add treasure to pawn and bring it to return cell
                if (treasure != null)
                {
                    treasure = GenSpawn.Spawn(treasure, retPawn.Position);
                    treasure.SetForbidden(true);

                    IntVec3 deliverCell = CellFinder.RandomClosewalkCellNear(ReturnToBaseCell, 6);

                    Job job = new Job(JobDefOf.HaulToCell, treasure, deliverCell);
                    job.maxNumToCarry = treasure.stackCount;
                    job.haulMode = HaulMode.ToCellNonStorage;
                    job.ignoreForbidden = true;
                    retPawn.jobs.StartJob(job);

                    remainingPoints -= (treasure.MarketValue * treasure.stackCount / 10);

                    //Log.Error("Remaining Points: " + remainingPoints.ToString());

                    // Infos for return message
                    if (!treasureFound)
                        treasureCarrier = retPawn;

                    treasures.Add(treasure);
                    treasureFound = true;
                }
                // No treasure? go to return cell
                else
                {
                    IntVec3 returnCell = CellFinder.RandomClosewalkCellNear(ReturnToBaseCell, 6);
                    retPawn.jobs.StartJob(new Job(JobDefOf.Goto, returnCell));
                }
            }

            // Add a new found pawn
            Pawn pawnFound = null;
            if (def.pawnFoundChance > 0)
            {
                bool pawnFoundSuccess = Rand.RangeInclusive(0, 100) <= def.pawnFoundChance;
                if (pawnFoundSuccess && def.pawnFoundDefs != null && def.pawnFoundDefs.Count > 0)
                {
                    Faction faction1 = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Outlander);
                    pawnFound = PawnGenerator.GeneratePawn(def.pawnFoundDefs.RandomElement(), faction1);

                    /// Test-Equipment
                    //ThingWithComps t = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Gun_Pistol")) as ThingWithComps;
                    //pawnFound.equipment.AddEquipment(t);

                    if (!def.pawnWithEquipment)
                    {
                        List<ThingWithComps> equipment = new List<ThingWithComps>();
                        foreach (ThingWithComps eq in pawnFound.equipment.AllEquipment)
                        {
                            equipment.Add(eq);
                        }

                        for (int i = equipment.Count; i > 0; i--)
                        {
                            ThingWithComps eq = equipment[i - 1];

                            if (!eq.def.IsApparel)
                                pawnFound.equipment.DestroyEquipment(eq);
                        }
                    }

                    CellFinder.TryFindRandomReachableCellNear(ExitMapCell, 10, TraverseParms.For(TraverseMode.NoPassClosedDoors), predicateSpawnCell, null, out spawnCell);

                    GenSpawn.Spawn(pawnFound, spawnCell);
                    pawnFound.SetFaction(Faction.OfPlayer);
                }
            }



            // Maybe start enemy spawn counter
            float randomValue = (float)Rand.RangeInclusive(0, 100);

            // If the weapons are good enough, reduce the chance for enemies (value * 2)
            if (weaponPoints * 3 > IncidentPoints / 10 ? true : false)
                randomValue *= 2;
            
            if ((treasureFound && randomValue < def.enemySpawnChanceTreasure) || (!treasureFound && randomValue < def.enemySpawnChanceNoTreasure))
            {
                // Start enemy spawn ticks
                enemyPursuit = TryExecuteRaid();
            }

            // Do returned letter handling
            LetterType letterType = LetterType.Good;
            StringBuilder returnMessage = new StringBuilder();

            returnMessage.AppendLine(def.LetterMessage_ReturnedBaseVariable.Translate()).AppendLine();

            if (treasureFound)
            {
                returnMessage.AppendLine(def.LetterMessage_ReturnedWithTreasureVariable.Translate()).AppendLine();
                StringBuilder treasureString = new StringBuilder();
                for (int i = 0; i < treasures.Count; i++)
                {
                    //if (i > 0)
                    //    treasureString.Append(", ");
                    //treasureString.Append(treasures[i].LabelCap);
                    treasureString.AppendLine(treasures[i].LabelCap);
                }
                returnMessage.AppendLine(treasureString.ToString());
            }
            else
            {
                returnMessage.AppendLine(def.LetterMessage_ReturnedNoTreasureVariable.Translate());
            }

            if (pawnFound != null)
            {
                string message = def.LetterMessage_ReturnedWithNewColonistVariable.Translate();
                returnMessage.AppendLine(message.AdjustedFor(pawnFound));
            }

            if (enemyPursuit)
            {
                //returnMessage.AppendLine();
                returnMessage.Append(def.LetterMessage_ReturnedWithEnemyVariable.Translate());
                letterType = LetterType.BadNonUrgent;
            }
            
            // Show letter
            Letter letter = new Letter(def.LetterLabel_ReturnedVariable.Translate(), returnMessage.ToString(), letterType, treasureCarrier);
            Find.LetterStack.ReceiveLetter(letter, null);

            // Add letter to history
            //Find.History.AddHistoryRecordFromLetter(letter, null);

            // Reset Incident
            PawnsOutOfMap.Clear();
            returnTicks = -1;
            autoDeactivateTicks = -1;
            Active = false;

            // Load random new def for next start
            def = null;
        }


        /// <summary>
        /// Try to generate a random treasure
        /// </summary>
        /// <returns></returns>
        private Thing TryGetTreasure()
        {
            Thing treasure = null;

            // find fitting treasure
            TreasureCollection foundTreasure = null;

            if (!def.treasureDefs.TryRandomElementByWeight<TreasureCollection>(f => f.Chance, out foundTreasure))
                return null;

            // make treasure
            if (foundTreasure == null)
                return null;

            treasure = ThingMaker.MakeThing(foundTreasure.TreasureDef, foundTreasure.StuffDef);

            // try adjust quality
            CompQuality treasureCQ = treasure.TryGetComp<CompQuality>();
            if (treasureCQ != null)
                treasureCQ.SetQuality(QualityUtility.RandomCreationQuality(Rand.RangeInclusive(10, 20)), ArtGenerationContext.Outsider);

            // adjust Stack to a random stack size
            if (foundTreasure.GiveStack && treasure.def.stackLimit > 1)
                treasure.stackCount = Rand.RangeInclusive(1, treasure.def.stackLimit);

            // adjust Hitpoints (60 to Max)
            if (treasure.stackCount == 1)
                treasure.HitPoints = Rand.RangeInclusive((int)(treasure.MaxHitPoints * 0.6), treasure.MaxHitPoints);

            return treasure;
        }

        /// <summary>
        /// Sets the time for the pawns to be away
        /// </summary>
        public void SetNewTravelTime()
        {
            int tmpTicks = Rand.RangeInclusive((int)(GenDate.TicksPerDay * def.daysPawnsAreGoneMin), (int)(GenDate.TicksPerDay * def.daysPawnsAreGoneMax));
            
            // Colonies >= 2 years get an away time offset
            if (GenDate.YearsPassed >= 2)
                tmpTicks = Rand.RangeInclusive((int)(GenDate.TicksPerDay * (def.daysPawnsAreGoneMin + GenDate.YearsPassed / 2)), (int)(GenDate.TicksPerDay * (def.daysPawnsAreGoneMax + GenDate.YearsPassed)));

            returnTicks = tmpTicks;
        }

        #region From IncidentWorker_RaidEnemy and IncidentWorker_RefugeeChased

        /// <summary>
        /// This is taken and adapted from IncidentWorker_RefugeeChased
        /// </summary>
        /// <param name="parms"></param>
        /// <returns></returns>
        private bool TryExecuteRaid()
        {
            Faction faction;
            if ( !(
                from f in Find.FactionManager.AllFactions
                where ( f.def.hidden ? false : f.HostileTo( Faction.OfPlayer ) )
                select f ).TryRandomElement<Faction>( out faction ) )
            {
                return false;
            }

            IncidentParms pursuerAttackParms = StorytellerUtility.DefaultParmsNow( Find.Storyteller.def, IncidentCategory.ThreatBig );
            pursuerAttackParms.forced = true;
            pursuerAttackParms.faction = faction;
            //immediateAttack.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            ResolveRaidStrategy_NoSiege(ref pursuerAttackParms);
            pursuerAttackParms.raidArrivalMode = PawnsArriveMode.EdgeWalkIn;
            pursuerAttackParms.spawnCenter = ExitMapCell;

            if (pursuerAttackParms.points < IncidentPoints)
                pursuerAttackParms.points = IncidentPoints;
            if (pursuerAttackParms.points <= 0)
                pursuerAttackParms.points = Rand.RangeInclusive(40, 140);

            pursuerAttackParms.points *= pointMultiplier;

            int occurTick = Find.TickManager.TicksGame + Rand.RangeInclusive(def.enemySpawnTimeMin, def.enemySpawnTimeMax);
            Find.Storyteller.incidentQueue.Add(IncidentDef.Named("RaidEnemy"), occurTick, pursuerAttackParms);

            return true;
        }

        /// <summary>
        /// This is all taken and adapted from IncidentWorker_RaidEnemy 
        /// </summary>
        /// <param name="parms"></param>
        /// <returns></returns>
        private void ResolveRaidStrategy_NoSiege(ref IncidentParms parms)
        {
            if (parms != null && parms.raidStrategy != null && parms.raidStrategy.defName != "Siege")
                return;

            IncidentParms parmsCopy = parms;

            IEnumerable<RaidStrategyDef> strategyList = from d in DefDatabase<RaidStrategyDef>.AllDefs
                                                        where d.Worker.CanUseWith(parmsCopy) && d.defName != "Siege"
                                                        select d;

            // Nothing found, use default
            if (strategyList == null)
            {
                Log.Warning("RumorOf - ResolveRaidStrategy_NoSiege: strategyList is null, using default.");
                parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            }

            // Find valid element
            if (strategyList.TryRandomElementByWeight<RaidStrategyDef>((RaidStrategyDef d) => d.Worker.SelectionChance, out parms.raidStrategy))
                return;

            Log.Warning("RumorOf - ResolveRaidStrategy_NoSiege: No valid RaidStrategyFound, using default.");

            // Still nothing valid found, use default
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;

        }

        #endregion

        /// <summary>
        /// This function provides the info, if this component is available or not
        /// </summary>
        /// <param name="mc"></param>
        /// <returns></returns>
        public static bool IsMapComponentAvailable(out MapComponent_ColonistsOutsideMap_RumorOf mc)
        {
            mc = null;

            for (int i = 0; i < Find.Map.components.Count; i++)
            {
                mc = Find.Map.components[i] as MapComponent_ColonistsOutsideMap_RumorOf;
                if (mc != null)
                    break;
            }

            if (mc == null)
                return false;

            return true;
        }

        /// <summary>
        /// This function tries to add the mapcomponent if it isn't already available
        /// </summary>
        /// <returns></returns>
        public static bool TryAddMapComponent()
        {
            MapComponent_ColonistsOutsideMap_RumorOf mc;

            if (IsMapComponentAvailable(out mc))
                return true;

            mc = new MapComponent_ColonistsOutsideMap_RumorOf();
            Find.Map.components.Add(mc);

            return IsMapComponentAvailable(out mc);
        }


        /// <summary>
        /// Calculate a value for the weapon verbs
        /// </summary>
        /// <param name="verb"></param>
        /// <returns></returns>
        public static float CalcVerbValue(Verb verb)
        {
            float value = 0;

            if (verb == null || verb.verbProps == null)
                return 0f;

            ThingDef thingDef = (verb.ownerEquipment == null ? verb.caster.def : verb.ownerEquipment.def);

            try
            {

                Verb_Shoot verbShoot = verb as Verb_Shoot;

                //if (verbShoot != null && !verb.verbProps.MeleeRange && verb.verbProps.projectileDef != null)
                if (verbShoot != null && verb.verbProps.range > 1 && verb.verbProps.projectileDef != null)
                {
                    // Gun
                    float burstCount = verb.verbProps.burstShotCount;
                    float damage = verb.verbProps.projectileDef.projectile.damageAmountBase;
                    float cooldown = verb.ownerEquipment == null ? (float)verb.verbProps.defaultCooldownTicks : verb.ownerEquipment.GetStatValue(StatDefOf.RangedWeapon_Cooldown, true);
                    float warmup = GenDate.TicksToSeconds(verb.verbProps.warmupTicks);
                    float accuracy = verb.ownerEquipment == null ? (float)verb.verbProps.accuracyMedium : verb.ownerEquipment.GetStatValue(StatDefOf.AccuracyMedium, true);
                    //verb.verbProps.accuracyMedium; //(verb.verbProps.accuracyLong + verb.verbProps.accuracyMedium + verb.verbProps.accuracyShort) / 3;

                    // calculate gun value
                    value = ((burstCount * damage) / (warmup + cooldown)) * accuracy;

                    // offset
                    value *= 3f;

                    //Log.Error("Gun: " + thingDef.defName + " " + burstCount.ToString() + " " + damage.ToString() + " " + cooldown.ToString() + " " + warmup.ToString() + " " + accuracy.ToString() + " = " + value.ToString());

                }
                else
                {
                    // Melee Weapon
                    float damage = verb.ownerEquipment == null ? (float)verb.verbProps.meleeDamageBaseAmount : verb.ownerEquipment.GetStatValue(StatDefOf.MeleeWeapon_DamageAmount, true);
                    float cooldown = verb.ownerEquipment == null ? GenDate.TicksToSeconds(verb.verbProps.defaultCooldownTicks) : verb.ownerEquipment.GetStatValue(StatDefOf.MeleeWeapon_Cooldown, true);
                    float accuracy = verb.ownerEquipment == null ? (float)verb.verbProps.accuracyTouch : verb.ownerEquipment.GetStatValue(StatDefOf.AccuracyTouch, true);
                    //verb.verbProps.accuracyTouch;

                    // calculate melee value
                    value = (damage / cooldown) * accuracy;

                    // offset
                    value *= 2f;

                    //Log.Error("Melee: " + thingDef.defName + " " + damage.ToString() + " " + cooldown.ToString() + " " + accuracy.ToString() + " = " + value.ToString());

                }

            } catch (Exception ex) {
                // Error happening? return 0
                // Some mod weapons throw an error when trying to get the GetStatValue..
                if (thingDef != null)
                    Log.Warning("Couldn't calculate the weapon value of verb "+ thingDef + " -> using 0. Exception:" + ex.Message + "\n" + ex.StackTrace);
                else
                    Log.Warning("Couldn't calculate the weapon value (thing == null) -> using 0. Exception:" + ex.Message + "\n" + ex.StackTrace);

                return 0f;
            } 

            value = (value > 1 ? value : 1);
            return value;
        }


        #endregion

    }




}
