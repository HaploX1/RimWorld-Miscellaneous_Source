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
        private static readonly int AnomalyDistanceMax = 15;
        private static readonly IntRange ThingsCountRange = new IntRange(5, 9);
        private static readonly FloatRange TotalMarketValueRange = new FloatRange(2000f, 4000f);
        private static readonly IntRange NeurotrainersCountRange = new IntRange(3, 5);
        private const float AIPersonaCoreExtraChance = 0.15f;

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
                    //possibleSitePartsInt.Add(SitePartDefOf.Manhunters);
                    possibleSitePartsInt.Add(SitePartDefOf.SleepingMechanoids);
                    possibleSitePartsInt.Add(SitePartDefOf.Outpost);
                    possibleSitePartsInt.Add(SitePartDefOf.Outpost);
                    possibleSitePartsInt.Add(SitePartDefOf.Turrets);
                    possibleSitePartsInt.Add(SitePartDefOf.PreciousLump);
                    possibleSitePartsInt.Add(DefDatabase<SitePartDef>.GetNamedSilentFail("BanditCamp"));
                    possibleSitePartsInt.Add(DefDatabase<SitePartDef>.GetNamedSilentFail("ItemStash"));
                    possibleSitePartsInt.Add(SitePartDefOf.PossibleUnknownThreatMarker);
                    possibleSitePartsInt.Add(SitePartDefOf.PossibleUnknownThreatMarker);

                    possibleSitePartsInt.AddRange( DefDatabase<SitePartDef>.AllDefsListForReading );

                    SitePartDef spdBattlefield = DefDatabase<SitePartDef>.GetNamedSilentFail("Misc_Battlefield");
                    if (spdBattlefield != null)
                        possibleSitePartsInt.Add(spdBattlefield);
                }

                List<SitePartDef> list = new List<SitePartDef>();

                int maxRounds = Rand.RangeInclusive(0, 2);
                while (true)
                {
                    SitePartDef sitePartDef = possibleSitePartsInt.RandomElement();
                    list.Add(sitePartDef);

                    // Outpost may also have turrets
                    if (sitePartDef == SitePartDefOf.Outpost && Rand.Value < 0.4f)
                    {
                        list.Add(SitePartDefOf.Turrets);
                        break;
                    }

                    if (maxRounds <= 0)
                        break;

                    maxRounds -= 1;
                }

                return list;
            }
        }

        private List<SitePartDef> siteCoreDefs = null;
        public SitePartDef GetRandomSiteCoreDef()
        {
            if (siteCoreDefs == null)
            {
                siteCoreDefs = new List<SitePartDef>();
                siteCoreDefs.Add(DefDatabase<SitePartDef>.GetNamed(defName_Nothing));
                siteCoreDefs.Add(DefDatabase<SitePartDef>.GetNamed(defName_ItemStash));
                siteCoreDefs.Add(DefDatabase<SitePartDef>.GetNamed(defName_ItemStash));
                siteCoreDefs.Add(DefDatabase<SitePartDef>.GetNamed(defName_ItemStash));
                siteCoreDefs.Add(DefDatabase<SitePartDef>.GetNamed(defName_ItemStash));
                siteCoreDefs.Add(DefDatabase<SitePartDef>.GetNamed(defName_ItemStash));
                siteCoreDefs.Add(DefDatabase<SitePartDef>.GetNamed(defName_PreciousLumb));
                siteCoreDefs.Add(DefDatabase<SitePartDef>.GetNamed(defName_PreciousLumb));
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
        public override void CompTick()
        {
            base.CompTick();
            //RecacheEffectiveAreaPct();
            TryFindAnomaly(1);
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
            int min = AnomalyDistanceMin;
            int max = AnomalyDistanceMax;

            int tile2 = base.parent.Tile;
            int tile = default(int);
            if (!TryFindNewAnomalyTile(out tile, min, max, false, true, tile2))
                return;

            Site site;
            //bool spacerUsable = false;

            List<SitePartDef> siteParts = new List<SitePartDef>();
            if (Rand.Chance(Props.chanceForNoSitePart))
            {
                siteParts.Add(GetRandomSiteCoreDef());

                site = SiteMaker.TryMakeSite(siteParts, tile, false, null, true);
                //spacerUsable = true;
            }
            else
            {
                siteParts.Add(GetRandomSiteCoreDef());
                siteParts.AddRange(GetRandomSitePartDefs);
                site = SiteMaker.TryMakeSite(siteParts, tile, true, null, false);
            }

            //// if spacerUsable -> 35% chance that the faction is spacer
            //if (site != null && spacerUsable && Rand.Chance(0.35f))
            //{
            //    Faction spacerFaction = null;
            //    if ((from x in Find.FactionManager.AllFactionsListForReading
            //         where x.def == FactionDefOf.Ancients || x.def == FactionDefOf.AncientsHostile
            //         select x).TryRandomElement(out spacerFaction))
            //        site.SetFaction(spacerFaction);
            //}

            if (site != null)
            {
                // Try to add a railgun :)
                Thing railgun = null;
                ThingDef railgunDef = DefDatabase<ThingDef>.GetNamedSilentFail(railgunDefName);
                if (railgunDef != null &&
                    site.Faction != null && site.Faction.def.techLevel >= TechLevel.Industrial &&
                    Rand.Value < 0.10)
                {
                    railgun = ThingMaker.MakeThing(railgunDef);
                }


                List<Thing> items = null;
                // Improved Sensors -> Add Items
                if (HasImprovedSensors)
                {
                    ItemStashContentsComp itemStash = site.GetComponent<ItemStashContentsComp>();
                    if (itemStash != null && siteParts.Contains(DefDatabase<SitePartDef>.GetNamedSilentFail(defName_ItemStash)))
                    {
                        items = GenerateItems(site.Faction, StorytellerUtility.DefaultSiteThreatPointsNow());
                        itemStash.contents.TryAddRangeOrTransfer(items);

                        if (railgun != null)
                            itemStash.contents.TryAdd(railgun);
                    }
                }

                //site.Tile = tile;

                // Add a site timeout ???
                site.GetComponent<TimeoutComp>().StartTimeout(Rand.RangeInclusive(15, 60) * 60000);

                Find.WorldObjects.Add(site);
                Find.LetterStack.ReceiveLetter("TacticalComputer_LetterLabel_AnomalyFound".Translate(), "TacticalComputer_Message_AnomalyFound".Translate(), LetterDefOf.PositiveEvent, site);

            }
        }

        //// Added for testing purposes...
        //private Site CreateSite(int tile, SitePartDef sitePart, int days, Faction siteFaction, List<Thing> items)
        //{
        //    WorldObjectDef woDef;
        //    float chance = Rand.Value;
        //    //if (chance < 0.5f)
        //    //    woDef = WorldObjectDefOf.AbandonedFactionBase;
        //    //else
        //        woDef = WorldObjectDefOf.Site;
            
        //    Site site = (Site)WorldObjectMaker.MakeWorldObject(woDef);
        //    //site.Tile = tile;
        //    site.core = DefDatabase<SiteCoreDef>.GetNamed("Anomaly_ItemStash");

        //    if (sitePart != null)
        //        site.parts.Add(sitePart);

        //    if (siteFaction != null)
        //        site.SetFaction(siteFaction);

        //    if (days > 0)
        //        site.GetComponent<TimeoutComp>().StartTimeout(days * 60000);

        //    if (items != null && items.Count > 0)
        //        site.GetComponent<ItemStashContentsComp>().contents.TryAddRangeOrTransfer(items);

        //    //Find.WorldObjects.Add(site);
        //    return site;
        //}


        // From RimWorld.Planet.TileFinder.TryFindNewSiteTile(..)
        private bool TryFindNewAnomalyTile(out int tile, int minDist = 7, int maxDist = 27, bool allowCaravans = false, bool preferCloserTiles = true, int nearThisTile = -1)
        {
            Func<int, int> findTile = delegate (int root)
            {
                int minDist2 = minDist;
                int maxDist2 = maxDist;
                Predicate<int> validator = (int x) => !Find.WorldObjects.AnyWorldObjectAt(x) && TileFinder.IsValidTileForNewSettlement(x, null);
                TileFinderMode preferCloserTiles2 = TileFinderMode.Random;
                if (preferCloserTiles)
                    preferCloserTiles2 = TileFinderMode.Near;
                int result = default(int);
                if (TileFinder.TryFindPassableTileWithTraversalDistance(root, minDist2, maxDist2, out result, validator, false, preferCloserTiles2))
                {
                    return result;
                }
                return -1;
            };
            int arg = default(int);
            if (nearThisTile != -1)
            {
                arg = nearThisTile;
            }
            else if (!TileFinder.TryFindRandomPlayerTile(out arg, allowCaravans, (Predicate<int>)((int x) => findTile(x) != -1)))
            {
                tile = -1;
                return false;
            }
            tile = findTile(arg);
            return tile != -1;
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

                return "TacticalComputer_LongRangeAnomalyScannerEfficiency".Translate( EffectiveAreaPct.ToStringPercent() );
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
        protected List<Thing> GenerateItems(Faction siteFaction, float siteThreatPoints)
        {
            ThingSetMakerParams parms = default(ThingSetMakerParams);
            parms.totalMarketValueRange = new FloatRange(0.7f, 1.3f);

            ThingSetMakerDef thingSetMakerDef = DefDatabase<ThingSetMakerDef>.GetNamedSilentFail("Reward_Incidents_ItemStashQuestContents");

            return thingSetMakerDef.root.Generate(parms);
        }
        protected List<Thing> GenerateItems_w_AIPersonaCore(Faction siteFaction, float siteThreatPoints)
        {
            List<Thing> list = GenerateItems(siteFaction, siteThreatPoints);
            list.Add(ThingMaker.MakeThing(ThingDefOf.AIPersonaCore, null));
            return list;
        }

    }
}
