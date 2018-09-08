using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Verse; 
using Verse.AI; 
using Verse.Sound;

//using CommonMisc; // Helper classes


namespace Incidents
{
    public class IncidentWorker_CrashedCryptoSleepCasket : IncidentWorker
    {

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            ThingDef thingDef = DefDatabase<ThingDef>.GetNamed("CrashedCryptoSleepCasket");
            
            CellRect mapRect;
            int numXZ = thingDef.Size.x > thingDef.Size.z ?
                            thingDef.Size.x : thingDef.Size.z;
            //numXZ +=1;
            int count = 0;
            // find valid place
            while (true)
            {
                IntVec3 pos = CellFinder.RandomNotEdgeCell(20, map);
                mapRect = new CellRect(pos.x, pos.z, numXZ, numXZ);

                // Valid position
                if (IsMapRectClear(mapRect, map))
                {
                    ClearMapRect(mapRect, map);
                    break;
                }


                count++;
                if (count > 100)
                    return false;
            }


            // Create casket
            Building_CryptosleepCasket casket = TryMakeCasket(mapRect, map, thingDef);
            if (casket == null)
                return false;

            // Do bomb and flame explosion at place of impact
            GenExplosion.DoExplosion(casket.PositionHeld, map, 7, DamageDefOf.Bomb, casket);
            GenExplosion.DoExplosion(casket.PositionHeld, map, 4, DamageDefOf.Flame, casket);

            // Passenger count: 1 to 4 rnd
            int ccount = Rand.RangeInclusive(1, 4);
            for (int i = 0; i < ccount; i++)
            {
                MakeCasketContents(casket);
            }

            casket = GenSpawn.Spawn(casket, casket.Position, map) as Building_CryptosleepCasket;

            if (casket.DestroyedOrNull())
                return false;

            Letter letter = LetterMaker.MakeLetter("Letter_Label_CrashedCasket".Translate(), "Letter_Text_CrashedCasket".Translate(), LetterDefOf.ThreatSmall, casket);
            //Letter letter = LetterMaker.MakeLetter("Letter_Label_CrashedCasket".Translate(), "Letter_Text_CrashedCasket".Translate(), LetterDefOf.NeutralEvent, new TargetInfo(mapRect.CenterCell, map, false));
            
            Find.LetterStack.ReceiveLetter(letter);

            return true;
        }

        // Extracted and modified from GenStep_ScatterShrines:
        private static bool IsMapRectClear(CellRect mapRect, Map map)
        {
            foreach (IntVec3 cell in mapRect)
            {
                if (!map.pathGrid.WalkableFast(cell))
                    return false;

                List<Thing> thingList = cell.GetThingList(map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    //if (thingList[i].def.category == ThingCategory.Item || thingList[i].def.category == ThingCategory.Building || thingList[i].def.category == ThingCategory.Pawn || thingList[i].def.category == ThingCategory.Ethereal)
                    //{
                    //    return false;
                    //}
                    if (thingList[i].def.category == ThingCategory.Building || thingList[i].def.category == ThingCategory.Pawn || thingList[i].def.category == ThingCategory.Ethereal)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        private static void ClearMapRect(CellRect mapRect, Map map)
        {
            foreach (IntVec3 cell in mapRect)
            {
                List<Thing> thingList = cell.GetThingList(map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    thingList[i].Destroy(DestroyMode.Vanish);
                }
            }
        }

        private static Building_CryptosleepCasket TryMakeCasket(CellRect mapRect, Map map, ThingDef thingDef)
        {
            mapRect.ClipInsideMap(map);
            CellRect cellRect = new CellRect(mapRect.BottomLeft.x + 1, mapRect.BottomLeft.z + 1, 2, 1);
            cellRect.ClipInsideMap(map);
            foreach (IntVec3 current in cellRect)
            {
                List<Thing> thingList = current.GetThingList(map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    if (!thingList[i].def.destroyable)
                    {
                        return null;
                    }
                }
            }

            Building_CryptosleepCasket casket = (Building_CryptosleepCasket)ThingMaker.MakeThing(thingDef, null);
            casket.SetPositionDirect(cellRect.BottomLeft);

            if (Rand.Value < 0.5f)
                casket.Rotation = Rot4.East;
            else
                casket.Rotation = Rot4.North;
            
            return casket;
        }

        private static void MakeCasketContents(Building_CryptosleepCasket casket)
        {
            //Source from http://akshaya-m.blogspot.de/2015/03/elegant-way-to-switch-if-else.html
            /// NOTE: Throws Errors sometimes!!!
            //// Definition:
            //var newSwitch = new Dictionary<Func<int, bool>, Action>
            //{
            // { x => x < 10  ,   () =>  GenerateFriendlyAnimal(casket)   },  
            // { x => x < 20  ,   () =>  GenerateFriendlySpacer(casket)   },
            // { x => x < 30  ,   () =>  GenerateIncappedSpacer(casket)   },
            // { x => x < 45  ,   () =>  GenerateSlave(casket)            },
            // { x => x < 65  ,   () =>  GenerateHalfEatenSpacer(casket)  },
            // { x => x >= 65 ,   () =>  GenerateAngrySpacer(casket)      } 
            //};
            //// Call:
            //newSwitch.First(sw => sw.Key( Rand.RangeInclusive(0, 100) )).Value();



            int rnd = Rand.RangeInclusive(0, 100);
            if (rnd < 10)
                GenerateFriendlyAnimal(casket);
            else if (rnd < 20)
                GenerateFriendlySpacer(casket);
            else if (rnd < 35)
                GenerateIncappedSpacer(casket);
            else if (rnd < 50)
                GenerateSlave(casket);
            else if (rnd < 65)
                GenerateHalfEatenAncient(casket);
            else
                GenerateAngryAncient(casket);

        }

        private static void GenerateFriendlyAnimal(Building_CryptosleepCasket pod)
        {
            Faction faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.PlayerColony);
            PawnGenerationRequest request = new PawnGenerationRequest(FindRandomAnimalForSpawn(), faction, PawnGenerationContext.NonPlayer, -1, false, false, false, false, true, false, 1f, false, true, true, false, true, false, false, null, null, null, null, null, null, null, null);
            Pawn pawn = PawnGenerator.GeneratePawn(request);

            if (!pod.TryAcceptThing(pawn, false))
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
        }

        private static void GenerateFriendlySpacer(Building_CryptosleepCasket pod)
        {
            PawnGenerationRequest request = new PawnGenerationRequest(PawnKindDefOf.AncientSoldier, Faction.OfAncients, PawnGenerationContext.NonPlayer, -1, false, false, false, false, true, false, 1f, false, true, true, false, true, false, false, null, null, null, null, null, null, null, null);
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            GiveRandomLootInventoryForTombPawn(pawn);

            if (!pod.TryAcceptThing(pawn, false))
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
        }

        private static void GenerateIncappedSpacer(Building_CryptosleepCasket pod)
        {
            PawnGenerationRequest request = new PawnGenerationRequest(PawnKindDefOf.AncientSoldier, Faction.OfAncients, PawnGenerationContext.NonPlayer, -1, false, false, false, false, true, false, 1f, false, true, true, false, true, false, false, null, null, null, null, null, null, null, null);
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            HealthUtility.DamageUntilDowned(pawn, true);
            GiveRandomLootInventoryForTombPawn(pawn);

            if (!pod.TryAcceptThing(pawn, false))
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
        }

        private static void GenerateSlave(Building_CryptosleepCasket pod)
        {
            PawnGenerationRequest request = new PawnGenerationRequest(PawnKindDefOf.Slave, Faction.OfAncients, PawnGenerationContext.NonPlayer, -1, false, false, false, false, true, false, 1f, false, true, true, false, true, false, false, null, null, null, null, null, null, null, null);
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            HealthUtility.DamageUntilDowned(pawn, true);
            GiveRandomLootInventoryForTombPawn(pawn);
            if (Rand.Value < 0.5f)
            {
                HealthUtility.DamageUntilDead(pawn);
            }

            if (!pod.TryAcceptThing(pawn, false))
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
        }

        private static void GenerateAngryAncient(Building_CryptosleepCasket pod)
        {
            PawnGenerationRequest request = new PawnGenerationRequest(PawnKindDefOf.AncientSoldier, Faction.OfAncientsHostile, PawnGenerationContext.NonPlayer, -1, false, false, false, false, true, false, 1f, false, true, true, false, true, false, false, null, null, null, null, null, null, null, null);
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            GiveRandomLootInventoryForTombPawn(pawn);

            if (!pod.TryAcceptThing(pawn, false))
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
        }

        private static void GenerateHalfEatenAncient(Building_CryptosleepCasket pod)
        {
            PawnGenerationRequest request = new PawnGenerationRequest(PawnKindDefOf.AncientSoldier, Faction.OfAncients, PawnGenerationContext.NonPlayer, -1, false, false, false, false, true, false, 1f, false, true, true, false, true, false, false, null, null, null, null, null, null, null, null);
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            HediffSet hediffSet = pawn.health.hediffSet;
            int num = Rand.Range(5, 10);
            for (int i = 0; i < num; i++)
            {
                BodyPartRecord bodyPartRecord = HittablePartsViolence(hediffSet).RandomElementByWeight((BodyPartRecord x) => x.coverageAbs);
                Pawn pawnD = pawn;
                DamageDef bite = DamageDefOf.Bite;
                float amount = (float)Rand.Range(3, 8);
                float armorPenetration = 999f;
                Pawn instigator = pawn;
                BodyPartRecord hitPart = bodyPartRecord;
                DamageInfo dinfo = new DamageInfo(bite, amount, armorPenetration, -1f, null, hitPart, null, DamageInfo.SourceCategory.ThingOrUnknown, null);
                pawn.TakeDamage(dinfo);
            }

            GiveRandomLootInventoryForTombPawn(pawn);
            
            // Add a few insects
            List<Pawn> pawn2 = new List<Pawn>();
            int pawnCount;
            float rnd2 = Rand.Value;
            if (rnd2 < 0.1)
            {
                pawn2.Add( PawnGenerator.GeneratePawn(PawnKindDefOf.Spelopede, null));
                pawnCount = 1;
            }
            else if (rnd2 < 0.35) 
            {
                pawn2.Add(PawnGenerator.GeneratePawn(PawnKindDefOf.Megaspider, null));
                pawnCount = 1;
            }
            else
            {
                pawnCount = Rand.Range(3, 6);
                for (int k = 0; k < pawnCount; k++)
                    pawn2.Add(PawnGenerator.GeneratePawn(PawnKindDefOf.Megascarab, null));
            }

            for (int j = 0; j < pawnCount; j++)
            {
                Pawn pawn3 = pawn2[j];
                if (!pod.TryAcceptThing(pawn3, false))
                {
                    Find.WorldPawns.PassToWorld(pawn3, PawnDiscardDecideMode.Discard);
                    return;
                }
                pawn3.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, null, false, false, null, false);
            }
            pawn2 = null;
        }

        // from RimWorld.ItemCollectionGenerator_AncientPodContents
        private static void GiveRandomLootInventoryForTombPawn(Pawn p)
        {
            float rand = Rand.Value;
            if (rand < 0.05f)
            {
                ThingDef gun = DefDatabase<ThingDef>.GetNamedSilentFail("Gun_RailgunMKI");
                if (gun == null)
                    gun = DefDatabase<ThingDef>.GetNamedSilentFail("Gun_ChargeLance");
                MakeIntoContainer(p.inventory.innerContainer, gun, 1);
            }

            if (rand > 0.35f && rand < 0.60f)
            {
                MakeIntoContainer(p.inventory.innerContainer, ThingDefOf.ComponentSpacer, Rand.Range(-1, 5));
            }

            if (rand < 0.45f)
            {
                MakeIntoContainer(p.inventory.innerContainer, ThingDefOf.Gold, Rand.Range(10, 50));
            }
            else if (rand < 0.65f)
            {
                //MakeIntoContainer(p.inventory.innerContainer, ThingDefOf.Luciferium, Rand.Range(3, 15)); 
                MakeIntoContainer(p.inventory.innerContainer, ThingDefOf.Uranium, Rand.Range(5, 60));
            }
            else
            {
                MakeIntoContainer(p.inventory.innerContainer, ThingDefOf.Plasteel, Rand.Range(10, 50));
            }
            MakeIntoContainer(p.inventory.innerContainer, ThingDefOf.ComponentIndustrial, Rand.Range(-1, 6));
        }
        private static void MakeIntoContainer(ThingOwner container, ThingDef def, int count)
        {
            if (def == null || count <= 0)
                return;

            Thing thing = ThingMaker.MakeThing(def, null);
            thing.stackCount = count;
            container.TryAdd(thing, true);
        }

        private static IEnumerable<BodyPartRecord> HittablePartsViolence(HediffSet bodyModel)
        {
            return from x in bodyModel.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null)
                   where x.depth == BodyPartDepth.Outside || (x.depth == BodyPartDepth.Inside && x.def.IsSolid(x, bodyModel.hediffs))
                   select x;
        }

        private static PawnKindDef FindRandomAnimalForSpawn()
        {
            PawnKindDef pkDef = null; 
            (from td in DefDatabase<PawnKindDef>.AllDefs
             where td.RaceProps.Animal && td.combatPower < 200
             select td).TryRandomElement(out pkDef);
            
            return pkDef;
        }

    }
}
