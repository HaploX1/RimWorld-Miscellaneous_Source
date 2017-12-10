using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TacticalComputer
{
    // Most of this is based on the CompLongRangeMineralScanner
    public class CompLongRangeAnomalyScanner : ThingComp
    {
        private static readonly int AnomalyDistanceMin = 3;
        private static readonly IntRange ThingsCountRange = new IntRange(5, 9);
        private static readonly FloatRange TotalMarketValueRange = new FloatRange(2000f, 4000f);
        private static readonly IntRange NeurotrainersCountRange = new IntRange(3, 5);
        private const float AIPersonaCoreExtraChance = 0.25f;

        private static readonly string railgunDefName = "Gun_RailgunMKI";

        private static readonly string defName_ItemStash = "Anomaly_ItemStash";
        private static readonly string defName_Nothing = "Anomaly_Nothing";
        private static readonly string defName_PreciousLumb = "Anomaly_PreciousLump";

        private CompPowerTrader powerComp;

        private List<Pair<Vector3, float>> otherActiveScanners = new List<Pair<Vector3, float>>();

        private float cachedEffectiveAreaPct;

        public CompProperties_LongRangeAnomalyScanner Props
        {
            get
            {
                return (CompProperties_LongRangeAnomalyScanner)props;
            }
        }

        private List<SitePartDef> possibleSitePartsInt = null;
        private List<SitePartDef> GetRandomSitePartDefs
        {
            get
            {
                if (possibleSitePartsInt == null)
                {
                    possibleSitePartsInt = new List<SitePartDef>();
                    possibleSitePartsInt.Add(SitePartDefOf.Manhunters);
                    possibleSitePartsInt.Add(SitePartDefOf.Outpost);
                    possibleSitePartsInt.Add(SitePartDefOf.Turrets);
                    possibleSitePartsInt.Add(SitePartDefOf.SleepingMechanoids);
                }
                List<SitePartDef> list = new List<SitePartDef>();
                SitePartDef sitePartDef = possibleSitePartsInt.RandomElement();
                list.Add(sitePartDef);

                // Outpost may also have turrets
                if (sitePartDef == SitePartDefOf.Outpost && Rand.Value > 0.6f)
                    list.Add(SitePartDefOf.Turrets);

                return list;
            }
        }

        private List<SiteCoreDef> siteCoreDefs = null;
        public SiteCoreDef GetRandomSiteCoreDef()
        {
            if (siteCoreDefs == null)
            {
                siteCoreDefs = new List<SiteCoreDef>();
                siteCoreDefs.Add(DefDatabase<SiteCoreDef>.GetNamed(defName_Nothing));
                siteCoreDefs.Add(DefDatabase<SiteCoreDef>.GetNamed(defName_ItemStash));
                siteCoreDefs.Add(DefDatabase<SiteCoreDef>.GetNamed(defName_ItemStash));
                siteCoreDefs.Add(DefDatabase<SiteCoreDef>.GetNamed(defName_ItemStash));
                siteCoreDefs.Add(DefDatabase<SiteCoreDef>.GetNamed(defName_ItemStash));
                siteCoreDefs.Add(DefDatabase<SiteCoreDef>.GetNamed(defName_ItemStash));
                siteCoreDefs.Add(DefDatabase<SiteCoreDef>.GetNamed(defName_PreciousLumb));
                siteCoreDefs.Add(DefDatabase<SiteCoreDef>.GetNamed(defName_PreciousLumb));
            }

            return siteCoreDefs.RandomElement();
        }

        public bool Active
        {
            get
            {
                return parent.Spawned && (powerComp == null || powerComp.PowerOn) && parent.Faction == Faction.OfPlayer;
            }
        }

        private float EffectiveMtbDays
        {
            get
            {
                float effectiveAreaPct = EffectiveAreaPct;
                if (effectiveAreaPct <= 0.001f)
                    return -1f;

                return Props.mtbDays / effectiveAreaPct;
            }
        }

        private float EffectiveAreaPct
        {
            get
            {
                return cachedEffectiveAreaPct;
            }
        }

        public bool HasImprovedSensors
        {
            get
            {
                if (Props == null || Props.researchSensorsDef == null)
                    return false;

                return Props.researchSensorsDef.IsFinished;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.GetComp<CompPowerTrader>();
            RecacheEffectiveAreaPct();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                RecacheEffectiveAreaPct();
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            RecacheEffectiveAreaPct();
            TryFindAnomaly(250);
        }

        private void TryFindAnomaly(int interval)
        {
            if (!Active)
                return;

            float effectiveMtbDays = EffectiveMtbDays;
            if (effectiveMtbDays <= 0f)
                return;

            if (Rand.MTBEventOccurs(effectiveMtbDays, GenDate.TicksPerDay, (float)interval))
                FoundAnomaly();
        }

        private void FoundAnomaly()
        {
            int tile;
            if (!TryFindNewAnomalyTile(out tile))
                return;

            Site site;
            bool spacerUsable = false;

            if (Rand.Chance(Props.chanceForNoSitePart))
            {

                site = SiteMaker.TryMakeSite(GetRandomSiteCoreDef(), null, false, null);
                spacerUsable = true;
            }
            else
            {
                
                site = SiteMaker.TryMakeSite(GetRandomSiteCoreDef(), GetRandomSitePartDefs, true, null);
            }

            // if spacerUsable -> 35% chance that the faction is spacer
            if (site != null && spacerUsable && Rand.Chance(0.35f))
            {
                Faction spacerFaction = null;
                if ((from x in Find.FactionManager.AllFactionsListForReading
                     where x.def == FactionDefOf.Spacer || x.def == FactionDefOf.SpacerHostile
                     select x).TryRandomElement(out spacerFaction))
                    site.SetFaction(spacerFaction);



            }

            if (site != null)
            {
                // Try to add a railgun :)
                Thing railgun = null;
                ThingDef railgunDef = DefDatabase<ThingDef>.GetNamedSilentFail(railgunDefName);
                if (railgunDef != null &&
                    site.Faction != null && site.Faction.def.techLevel >= TechLevel.Industrial &&
                    Rand.Value > 0.90)
                {
                    railgun = ThingMaker.MakeThing(railgunDef);
                }


                List<Thing> items = null;
                // Improved Sensors -> Add Items
                if (HasImprovedSensors)
                {
                    ItemStashContentsComp itemStash = site.GetComponent<ItemStashContentsComp>();
                    if (itemStash != null && site.core.defName == defName_ItemStash)
                    {
                        items = GenerateItems(site.Faction);
                        itemStash.contents.TryAddRangeOrTransfer(items);

                        if (railgun != null)
                            itemStash.contents.TryAdd(railgun);
                    }
                }
                

                site.Tile = tile;
                Find.WorldObjects.Add(site);
                Find.LetterStack.ReceiveLetter("TacticalComputer_LetterLabel_AnomalyFound".Translate(), "TacticalComputer_Message_AnomalyFound".Translate(), LetterDefOf.PositiveEvent, site);

                // Add a site timeout ???
                site.GetComponent<TimeoutComp>().StartTimeout(Rand.RangeInclusive(15, 60) * 60000);
            }
        }

        // Added for testing purposes...
        private Site CreateSite(int tile, SitePartDef sitePart, int days, Faction siteFaction, List<Thing> items)
        {
            WorldObjectDef woDef;
            float chance = Rand.Value;
            //if (chance < 0.5f)
            //    woDef = WorldObjectDefOf.AbandonedFactionBase;
            //else
                woDef = WorldObjectDefOf.Site;
            
            Site site = (Site)WorldObjectMaker.MakeWorldObject(woDef);
            //site.Tile = tile;
            site.core = DefDatabase<SiteCoreDef>.GetNamed("Anomaly_ItemStash");

            if (sitePart != null)
                site.parts.Add(sitePart);

            if (siteFaction != null)
                site.SetFaction(siteFaction);

            if (days > 0)
                site.GetComponent<TimeoutComp>().StartTimeout(days * 60000);

            if (items != null && items.Count > 0)
                site.GetComponent<ItemStashContentsComp>().contents.TryAddRangeOrTransfer(items);

            //Find.WorldObjects.Add(site);
            return site;
        }


        // From RimWorld.Planet.TileFinder.TryFindNewSiteTile(..)
        private bool TryFindNewAnomalyTile(out int tile)
        {
            int rootTile;
            if (!TileFinder.TryFindRandomPlayerTile(out rootTile, true))
            {
                tile = -1;
                return false;
            }
            return TileFinder.TryFindPassableTileWithTraversalDistance(rootTile, AnomalyDistanceMin, (int)Props.radius, out tile, (int x) => !Find.WorldObjects.AnyWorldObjectAt(x), false);
        }

        private void CalculateOtherActiveAnomalyScanners()
        {
            otherActiveScanners.Clear();
            List<Map> maps = Find.Maps;
            WorldGrid worldGrid = Find.WorldGrid;
            for (int i = 0; i < maps.Count; i++)
            {
                List<Thing> list = maps[i].listerThings.AllThings.Where(t => t.def == parent.def).ToList();
                for (int j = 0; j < list.Count; j++)
                {
                    CompLongRangeAnomalyScanner compLongRangeScanner = list[j].TryGetComp<CompLongRangeAnomalyScanner>();
                    if (compLongRangeScanner != null && InterruptsMe(compLongRangeScanner))
                    {
                        Vector3 tileCenter = worldGrid.GetTileCenter(maps[i].Tile);
                        float second = worldGrid.TileRadiusToAngle(compLongRangeScanner.Props.radius);
                        otherActiveScanners.Add(new Pair<Vector3, float>(tileCenter, second));
                    }
                }
            }
        }

        private bool InterruptsMe(CompLongRangeAnomalyScanner otherScanner)
        {
            if (otherScanner == null || otherScanner == this)
                return false;

            if (!otherScanner.Active)
                return false;

            if (Props.mtbDays != otherScanner.Props.mtbDays)
                return otherScanner.Props.mtbDays < Props.mtbDays;

            return otherScanner.parent.thingIDNumber < parent.thingIDNumber;
        }

        private void RecacheEffectiveAreaPct()
        {
            if (!Active)
            {
                cachedEffectiveAreaPct = 0f;
                return;
            }
            CalculateOtherActiveAnomalyScanners();
            if (!otherActiveScanners.Any<Pair<Vector3, float>>())
            {
                cachedEffectiveAreaPct = 1f;
                return;
            }
            CompProperties_LongRangeAnomalyScanner props = Props;
            WorldGrid worldGrid = Find.WorldGrid;
            Vector3 tileCenter = worldGrid.GetTileCenter(parent.Tile);
            float angle = worldGrid.TileRadiusToAngle(props.radius);
            int num = 0;
            int count = otherActiveScanners.Count;
            Rand.PushState(parent.thingIDNumber);
            for (int i = 0; i < 400; i++)
            {
                Vector3 point = Rand.PointOnSphereCap(tileCenter, angle);
                bool flag = false;
                for (int j = 0; j < count; j++)
                {
                    Pair<Vector3, float> pair = otherActiveScanners[j];
                    if (MeshUtility.Visible(point, 1f, pair.First, pair.Second))
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                    num++;
            }
            Rand.PopState();
            cachedEffectiveAreaPct = (float)num / 400f;
        }

        public override string CompInspectStringExtra()
        {
            if (Active)
            {
                RecacheEffectiveAreaPct();

                return "TacticalComputer_LongRangeAnomalyScannerEfficiency".Translate(new object[]
                {
                    EffectiveAreaPct.ToStringPercent()
                });
            }
            else
            {
                return null;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
                yield return gizmo;

            if (!Active || !Prefs.DevMode || !DebugSettings.godMode)
                yield break;

            yield return new Command_Action
            {
                defaultLabel = "Debug: Anomaly found",
                action = delegate
                {
                    FoundAnomaly();
                }
            };

        }








        // from IncidentWorker_QuestItemStash

        private List<Pair<ItemCollectionGeneratorDef, ItemCollectionGeneratorParams>> possibleItemCollectionGenerators = new List<Pair<ItemCollectionGeneratorDef, ItemCollectionGeneratorParams>>();
        
        private List<Thing> GenerateItems(Faction siteFaction)
        {
            TechLevel? techLevel = siteFaction != null ? siteFaction.def.techLevel : TechLevel.Undefined;
            TechLevel techLevel2 = (techLevel == TechLevel.Undefined) ? TechLevel.Spacer : techLevel.Value;
            this.CalculatePossibleItemCollectionGenerators(techLevel2);
            Pair<ItemCollectionGeneratorDef, ItemCollectionGeneratorParams> pair = this.possibleItemCollectionGenerators.RandomElement<Pair<ItemCollectionGeneratorDef, ItemCollectionGeneratorParams>>();
            return pair.First.Worker.Generate(pair.Second);
        }
        private void CalculatePossibleItemCollectionGenerators(TechLevel techLevel)
        {
            this.possibleItemCollectionGenerators.Clear();
            if (techLevel >= ThingDefOf.AIPersonaCore.techLevel)
            {
                ItemCollectionGeneratorDef standard = ItemCollectionGeneratorDefOf.Standard;
                ItemCollectionGeneratorParams second = default(ItemCollectionGeneratorParams);
                second.extraAllowedDefs = Gen.YieldSingle<ThingDef>(ThingDefOf.AIPersonaCore);
                second.count = new int?(1);
                this.possibleItemCollectionGenerators.Add(new Pair<ItemCollectionGeneratorDef, ItemCollectionGeneratorParams>(standard, second));
                if (Rand.Chance(0.25f) && !this.PlayerOrItemStashHasAIPersonaCore())
                {
                    return;
                }
            }
            //if (techLevel >= ThingDefOf.MechSerumNeurotrainer.techLevel)
            //{
            //    ItemCollectionGeneratorDef standard2 = ItemCollectionGeneratorDefOf.Standard;
            //    ItemCollectionGeneratorParams second2 = default(ItemCollectionGeneratorParams);
            //    second2.extraAllowedDefs = Gen.YieldSingle<ThingDef>(ThingDefOf.MechSerumNeurotrainer);
            //    second2.count = new int?(ItemCollectionGenerator_ItemStashQuest.NeurotrainersCountRange.RandomInRange);
            //    this.possibleItemCollectionGenerators.Add(new Pair<ItemCollectionGeneratorDef, ItemCollectionGeneratorParams>(standard2, second2));
            //}
            List<ThingDef> allGeneratableItems = ItemCollectionGeneratorUtility.allGeneratableItems;
            for (int i = 0; i < allGeneratableItems.Count; i++)
            {
                ThingDef thingDef = allGeneratableItems[i];
                if (techLevel >= thingDef.techLevel && thingDef.itemGeneratorTags != null && thingDef.itemGeneratorTags.Contains(ItemCollectionGeneratorUtility.SpecialRewardTag))
                {
                    ItemCollectionGeneratorDef standard3 = ItemCollectionGeneratorDefOf.Standard;
                    ItemCollectionGeneratorParams second3 = default(ItemCollectionGeneratorParams);
                    second3.extraAllowedDefs = Gen.YieldSingle<ThingDef>(thingDef);
                    second3.count = new int?(1);
                    this.possibleItemCollectionGenerators.Add(new Pair<ItemCollectionGeneratorDef, ItemCollectionGeneratorParams>(standard3, second3));
                }
            }
            ItemCollectionGeneratorParams second4 = default(ItemCollectionGeneratorParams);
            second4.count = new int?(ThingsCountRange.RandomInRange);
            second4.totalMarketValue = new float?(TotalMarketValueRange.RandomInRange);
            second4.techLevel = new TechLevel?(techLevel);
            this.possibleItemCollectionGenerators.Add(new Pair<ItemCollectionGeneratorDef, ItemCollectionGeneratorParams>(ItemCollectionGeneratorDefOf.Weapons, second4));
            this.possibleItemCollectionGenerators.Add(new Pair<ItemCollectionGeneratorDef, ItemCollectionGeneratorParams>(ItemCollectionGeneratorDefOf.RawResources, second4));
            this.possibleItemCollectionGenerators.Add(new Pair<ItemCollectionGeneratorDef, ItemCollectionGeneratorParams>(ItemCollectionGeneratorDefOf.Apparel, second4));
            // Added !!!
            this.possibleItemCollectionGenerators.Add(new Pair<ItemCollectionGeneratorDef, ItemCollectionGeneratorParams>(ItemCollectionGeneratorDefOf.AncientTempleContents, second4));

        }

        private bool PlayerOrItemStashHasAIPersonaCore()
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].listerThings.ThingsOfDef(ThingDefOf.AIPersonaCore).Count > 0)
                {
                    return true;
                }
            }
            List<Caravan> caravans = Find.WorldObjects.Caravans;
            for (int j = 0; j < caravans.Count; j++)
            {
                if (caravans[j].IsPlayerControlled && CaravanInventoryUtility.HasThings(caravans[j], ThingDefOf.AIPersonaCore, 1, null))
                {
                    return true;
                }
            }
            List<Site> sites = Find.WorldObjects.Sites;
            for (int k = 0; k < sites.Count; k++)
            {
                ItemStashContentsComp component = sites[k].GetComponent<ItemStashContentsComp>();
                if (component != null)
                {
                    ThingOwner contents = component.contents;
                    for (int l = 0; l < contents.Count; l++)
                    {
                        if (contents[l].def == ThingDefOf.AIPersonaCore)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }


    }
}
