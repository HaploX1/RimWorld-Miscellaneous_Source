using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed
using Verse.AI; // Needed when you do something with the AI
using Verse.AI.Group;
using RimWorld.Planet;
//using Verse.Sound; // Needed when you do something with the Sound

namespace ArtefactFound
{
    /// <summary>
    /// This is the placed artefact
    /// </summary>
    [StaticConstructorOnStartup]
    public class Building_Artefact : Building
    {
        // Graphics
        public static Texture2D UI_Activate;
        public string UI_ActivatePath = "UI/Commands/ArtefactFound/UI_ActivateArtefact";
        // Text
        public string txtActivateArtefact = "ArtefactFound_ActivateArtefact";
        public string txtArtefactActivatedMechanoids = "ArtefactFound_ActivatedArtefactMechanoids";
        public string txtArtefactActivatedRaiders = "ArtefactFound_ActivatedArtefactRaiders";
        public string txtArtefactActivatedResources = "ArtefactFound_ActivatedArtefactResources";
        public string txtArtefactActivatedPawnReleasedJoinsColony = "ArtefactFound_ActivatedArtefactPawnReleased";
        public string txtArtefactActivatedNothing = "ArtefactFound_ActivatedArtefactNothing";
        public string labelLetterArtefact = "ArtefactFound_LabelLetterArtefact";

        public float pointsToSpend;
        private const float pointsToSpendMin = 150;

        // ToDo: Change mechanoid selection to auto selection
        private string pawnKindDefNameScyther = "Scyther";
        private string pawnKindDefNameCentipede = "Centipede";

        // Defined valid resources. I don't want to take all resources (and therefore some not needed resources) as found resources
        // ToDo: Export to XML-file
        private List<ThingDef> resourceDefs = new List<ThingDef>()
        {
            ThingDef.Named("Steel"),
            ThingDef.Named("Silver"),
            ThingDef.Named("Gold"),
            ThingDef.Named("Plasteel"),
            ThingDef.Named("Uranium"),
            ThingDef.Named("Jade"),
            ThingDef.Named("Synthread"),
            ThingDef.Named("Hyperweave"),
            ThingDef.Named("GlitterworldMedicine")
        };

        // Todo: Find function to auto select non tribal, non mechanoid humans, who can fight
        private List<PawnKindDef> pawnKindDefs = new List<PawnKindDef>()
        {
            PawnKindDef.Named("MercenaryGunner"),
            PawnKindDef.Named("MercenarySniper"),
            PawnKindDef.Named("GrenadierDestructive"),
            PawnKindDef.Named("MercenarySlasher"),
            PawnKindDef.Named("MercenaryHeavy"),
            PawnKindDef.Named("Drifter"),
            PawnKindDef.Named("Scavenger"),
            PawnKindDef.Named("Pirate"),
            PawnKindDef.Named("Thrasher"),
            PawnKindDef.Named("SpaceSoldier")
        };


        // ===================== Setup Work =====================

        /// <summary>
        /// Do something after the object is initialized, but before it is spawned
        /// </summary>
        public override void PostMake()
        {
            base.PostMake();
        }

        /// <summary>
        /// This is called when this is spawned into the world
        /// </summary>
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            LongEventHandler.ExecuteWhenFinished(SpawnSetup_Part2);

        }

        /// <summary>
        /// This is called seperately when the Mod-Thread is done.
        /// It is needed to be seperately from SpawnSetup, so that the graphics can be found
        /// </summary>
        private void SpawnSetup_Part2()
        {

            UI_Activate = ContentFinder<Texture2D>.Get(UI_ActivatePath, true);
            
        }



        // ===================== Destroy =====================

        /// <summary>
        /// Clean up when it is destroyed
        /// </summary>
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);
        }



        // ===================== Inspections =====================

        /// <summary>
        /// This creates selection buttons
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<Gizmo> GetGizmos()
        {
            
            // Key-Binding F - Activate
            Command_Action cmdF;
            cmdF = new Command_Action();
            cmdF.icon = UI_Activate;
            cmdF.defaultDesc = txtActivateArtefact.Translate();
            cmdF.hotKey = KeyBindingDefOf.Misc1; //B
            cmdF.activateSound = SoundDef.Named("Click");
            cmdF.action = ActivateArtefact;
            cmdF.groupKey = 3137677;
            yield return cmdF;

            foreach (Command cmd in base.GetGizmos())
                yield return cmd;

        }



        /// <summary>
        /// Do work for activating this artefact
        /// </summary>
        public void ActivateArtefact()
        {
            Find.TickManager.slower.SignalForceNormalSpeedShort();

            // savety, if points to spend is too low
            if (pointsToSpend <= 0)
                pointsToSpend = pointsToSpendMin * (1 + UnityEngine.Random.Range(0.0f, 2.5f));

            float value = Rand.Range(0.0f, 100.0f);

            if (value < 5.1f) // Pawn Released (5%)
            {
                DoPawnReleased();
            }
            else if (value < 30.1f) // Raider attack (25%)
            {
                DoRaiderAttack();
            }
            else if (value < 50.1f) // Mechanoid attack (20%)
            {
                DoMechanoidAttack();
            }
            else if (value < 85.1f) // Found resources (40%)
            {
                DoSpawnResources();
            }
            else // Found nothing (15%)
            {
                DoNothing();
            }


            // Destroy the artefact
            Destroy(DestroyMode.Vanish);
        }


        private void DoMechanoidAttack()
        {
            IntVec3 startPos;
            string str = txtArtefactActivatedMechanoids.Translate();

            // Find a valid spawn position
            startPos = CellFinderLoose.RandomCellWith((IntVec3 c) =>
            {

                if (Map.fogGrid.IsFogged(c))
                    return false;

                if (!Map.reachability.CanReachColony( c ))
                    return false;

                if (Map.roofGrid.Roofed(c))
                    return false;
                
                return true;
            }, Map);


            int countPawns;
            Faction faction = Faction.OfMechanoids;
            List<Pawn> pawns = new List<Pawn>();
            PawnKindDef pawnKindDef;

            // Spawn mechanoids

            // Random: What type is spawned?
            float rvalue = UnityEngine.Random.@value;

            if (rvalue < 0.5f)
            {
                // Spawn Centipedes

                rvalue = UnityEngine.Random.@value; // Random: How many?
                if (rvalue < 0.30f)
                    countPawns = 1;
                else if (rvalue < 0.65f)
                    countPawns = 2;
                else
                    countPawns = 5; // max or when points to spend are done

                pawnKindDef = PawnKindDef.Named(pawnKindDefNameCentipede);

            }
            else
            {
                // Spawn Scythers

                rvalue = UnityEngine.Random.@value; // Random: How many?
                if (rvalue < 0.30f)
                    countPawns = 1;
                else if (rvalue < 0.55f)
                    countPawns = 2;
                else
                    countPawns = 7; // max or when points to spend are done

                pawnKindDef = PawnKindDef.Named(pawnKindDefNameScyther);

            }

            // Create mechanoids
            for (int i = 0; i < countPawns; i++)
            {
                IntVec3 spawnPos;
                if (i == 0)
                    spawnPos = startPos;
                else
                    spawnPos = CellFinder.RandomClosewalkCellNear(startPos, Map, 15);

                Pawn pawn = PawnGenerator.GeneratePawn(pawnKindDef, faction);
                if (GenPlace.TryPlaceThing(pawn, spawnPos, Map, ThingPlaceMode.Near))
                {
                    pawns.Add(pawn);
                    pointsToSpend -= pawn.kindDef.combatPower;
                    //Log.Error("Points: " + pointsToSpend.ToString() + " // Costs: " + pawn.kindDef.pointsCost.ToString()); // TEST!!!
                    if (pointsToSpend <= 0.5f)
                        break;
                }
            }

            // Create Lord
            string empty = string.Empty;
            string empty2 = string.Empty;
            PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(pawns, ref empty, ref empty2, "LetterFamilyMembersRaidFriendly".Translate(), false);
            if (!empty2.NullOrEmpty())
            {
                Find.LetterStack.ReceiveLetter(empty, empty2, LetterDefOf.PositiveEvent, new GlobalTargetInfo(pawns[0].Position, pawns[0].Map), null);
            }

            LordJob lordJob = new LordJob_AssaultColony(faction, true, false, false);
            Lord lord = LordMaker.MakeNewLord(faction, lordJob, Map, pawns);
            AvoidGridMaker.RegenerateAvoidGridsFor(faction, Map);

            LessonAutoActivator.TeachOpportunity(ConceptDefOf.EquippingWeapons, OpportunityType.Critical);

            string label = labelLetterArtefact.Translate();

            // add game event
            Find.LetterStack.ReceiveLetter(label, str, LetterDefOf.ThreatSmall, pawns[0], null);

            // add raid to story watcher
            StatsRecord storyWatcher = Find.StoryWatcher.statsRecord;
            storyWatcher.numRaidsEnemy = storyWatcher.numRaidsEnemy + 1;

        }


        private void DoRaiderAttack()
        {

            IntVec3 startPos;
            string str = txtArtefactActivatedRaiders.Translate();

            // Find a valid spawn position
            if (!RCellFinder.TryFindRandomPawnEntryCell(out startPos, Map, CellFinder.EdgeRoadChance_Hostile, null))
                return;

            int countPawns;
            IEnumerable<Faction> factions = Find.FactionManager.AllFactions.Where<Faction>(f => f.HostileTo(Faction.OfPlayer) && f != Faction.OfMechanoids && f != Faction.OfInsects );
            Faction faction;
            if ( !factions.TryRandomElement(out faction))
            {
                return;
            }

            List<Pawn> pawns = new List<Pawn>();

            // Spawn raiders
            float rvalue = UnityEngine.Random.@value;

            countPawns = (int)UnityEngine.Random.Range(0.0f, 15.0f); // Random: max. count of pawns
            if (countPawns <= 0)
                countPawns = 1;

            // Create raider
            for (int i = 0; i < countPawns; i++)
            {
                IntVec3 spawnPos;
                if (i == 0)
                    spawnPos = startPos;
                else
                    spawnPos = CellFinder.RandomClosewalkCellNear(startPos, Map, 8);

                //pawnKindDefs = DefDatabase<PawnKindDef>.AllDefs.Where<PawnKindDef>(pk => pk.defaultFactionType.defName == "Spacer" || pk.defaultFactionType.defName == "Pirate").ToList();

                Pawn pawn = PawnGenerator.GeneratePawn(pawnKindDefs.RandomElement(), faction);
                if (GenPlace.TryPlaceThing(pawn, spawnPos, Map, ThingPlaceMode.Near))
                {
                    pawns.Add(pawn);
                    pointsToSpend -= pawn.kindDef.combatPower;
                    //Log.Error("Points: " + pointsToSpend.ToString() + " // Costs: " + pawn.kindDef.pointsCost.ToString()); // TEST!!!
                    if (pointsToSpend <= 0.0f)
                        break;
                }
            }

            // Create Lord
            string empty = string.Empty;
            string empty2 = string.Empty;
            PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(pawns, ref empty, ref empty2, "LetterFamilyMembersRaidFriendly".Translate(), false);
            if (!empty2.NullOrEmpty())
                Find.LetterStack.ReceiveLetter(empty, empty2, LetterDefOf.PositiveEvent, new GlobalTargetInfo(pawns[0].Position, pawns[0].Map), null);

            LordJob lordJob = new LordJob_AssaultColony(faction, false, false, false);
            Lord lord = LordMaker.MakeNewLord(faction, lordJob, Map, pawns);
            AvoidGridMaker.RegenerateAvoidGridsFor(faction, Map);

            LessonAutoActivator.TeachOpportunity(ConceptDefOf.EquippingWeapons, OpportunityType.Critical);

            string label = labelLetterArtefact.Translate();

            // add game event
            Find.LetterStack.ReceiveLetter(label, str, LetterDefOf.ThreatSmall, pawns[0], null);


            // add raid to story watcher
            StatsRecord storyWatcher = Find.StoryWatcher.statsRecord;
            storyWatcher.numRaidsEnemy = storyWatcher.numRaidsEnemy + 1;

        }


        private void DoSpawnResources()
        {

            IntVec3 startPos;
            string str = txtArtefactActivatedResources.Translate();

            startPos = CellFinderLoose.RandomCellWith((IntVec3 c) =>
            {
                // only allow standable, not fogged positions, not home region
                if (!c.Standable(Map) || Map.fogGrid.IsFogged(c) || Map.areaManager.Home[c])
                    return false;
                else
                    return true;
            }, Map);

            // Get random resource
            ThingDef thingDef = resourceDefs.RandomElement();

            List<Thing> things = new List<Thing>();
            while (true)
            {
                int maxValuePerStack = UnityEngine.Random.Range(200, 2000); // max value per stack

                Thing thing = ThingMaker.MakeThing(thingDef);
                thing.stackCount = UnityEngine.Random.Range(15, 75); // random stack count

                if (thing.stackCount > thing.def.stackLimit)
                    thing.stackCount = thing.def.stackLimit;

                if ((float)thing.stackCount * thing.def.BaseMarketValue > (float)maxValuePerStack) // check if stack is more worth than max value
                {
                    thing.stackCount = Mathf.CeilToInt((float)maxValuePerStack / thing.def.BaseMarketValue);
                }
                things.Add(thing);
                if (things.Count < UnityEngine.Random.Range(6, 13)) // create 6-12 stacks
                    continue;

                break;
            }

            //DropPodUtility.DropThingsNear(startPos, things, 0, true, false);
            foreach (Thing thing2 in things)
            {
                IntVec3 spawnPos = CellFinder.RandomClosewalkCellNear(startPos, Map, 5);
                GenPlace.TryPlaceThing(thing2, spawnPos, Map, ThingPlaceMode.Near);
            }

            string label = labelLetterArtefact.Translate();

            // add game event
            Find.LetterStack.ReceiveLetter(label, str, LetterDefOf.PositiveEvent, new GlobalTargetInfo( startPos, Map ), null);

        }


        private void DoPawnReleased()
        {
            // A pawn is released and joins the colony?
            IntVec3 startPos;
            string str;

            str = txtArtefactActivatedPawnReleasedJoinsColony.Translate();

            // Find a valid spawn position
            startPos = CellFinderLoose.RandomCellWith((IntVec3 c) =>
            {

                if (Map.fogGrid.IsFogged(c))
                    return false;

                if (!Map.reachability.CanReachColony(c))
                    return false;

                if (Map.roofGrid.Roofed(c))
                    return false;
                
                return true;
            }, Map );

            Faction faction = Faction.OfPlayer;

            Pawn pawn = PawnGenerator.GeneratePawn(pawnKindDefs.RandomElement(), faction);

            float value = UnityEngine.Random.Range(0.0f, 100.0f);
            if (value < 25.0f) // 25% chance: The pawn remains with weapon and clothes
            {
                // Do nothing...
            }
            else if (value < 60.0f) // 35% chance: The pawn has no weapon
            {
                pawn.equipment = new Pawn_EquipmentTracker(pawn);
            }
            else // 40% chance: The pawn has no weapon and is naked
            {
                pawn.apparel = new Pawn_ApparelTracker(pawn);
                pawn.equipment = new Pawn_EquipmentTracker(pawn);
            }

            // Spawn the pawn
            GenPlace.TryPlaceThing(pawn, startPos, Map, ThingPlaceMode.Direct);

            // Notify the Storyteller
            Find.Storyteller.intenderPopulation.Notify_PopulationGained();

            string label = labelLetterArtefact.Translate();

            // add game event
            Find.LetterStack.ReceiveLetter(label, str, LetterDefOf.PositiveEvent, pawn, null);

        }


        private void DoNothing()
        {

            string str = txtArtefactActivatedNothing.Translate();
            string label = labelLetterArtefact.Translate();

            // add game event
            Find.LetterStack.ReceiveLetter(label, str, LetterDefOf.NeutralEvent, null);
        }


    }
}
