using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Incidents
{
    // Base: RimWorld.IncidentWorker_QuestPrisonerRescue
    public class IncidentWorker_Battlefield : IncidentWorker
    {
        private IntRange distance = new IntRange(4, 15);

        private List<SitePartDef> possibleSitePartsInt = null;
        private List<SitePartDef> GetRandomSitePartDefs
        {
            get
            {
                if (possibleSitePartsInt == null)
                {
                    possibleSitePartsInt = new List<SitePartDef>();
                    possibleSitePartsInt.Add(SitePartDefOf.Manhunters);
                    possibleSitePartsInt.Add(SitePartDefOf.SleepingMechanoids);
                    possibleSitePartsInt.Add(SitePartDefOf.SleepingMechanoids);
                    possibleSitePartsInt.Add(SitePartDefOf.Outpost);
                    possibleSitePartsInt.Add(SitePartDefOf.Outpost);
                    possibleSitePartsInt.Add(SitePartDefOf.Outpost);
                    possibleSitePartsInt.Add(SitePartDefOf.Turrets);
                    possibleSitePartsInt.Add(SitePartDefOf.Turrets);
                    possibleSitePartsInt.Add(SitePartDefOf.Turrets);
                    possibleSitePartsInt.Add(SitePartDefOf.Turrets);
                }
                List<SitePartDef> list = new List<SitePartDef>();
                SitePartDef sitePartDef = possibleSitePartsInt.RandomElement();
                list.Add(sitePartDef);

                // Outpost may also have turrets
                if (sitePartDef == SitePartDefOf.Outpost && Rand.Value < 0.35f)
                    list.Add(SitePartDefOf.Turrets);

                return list;
            }
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms))
                return false;

            if (Find.AnyPlayerHomeMap == null)
                return false;

            int tile;
            if (!RimWorld.Planet.TileFinder.TryFindNewSiteTile(out tile, distance.min, distance.max, false))
                return false;

            Faction factionEnemies, factionFriends;
            GenStep_Battlefield.TryFindFightingFactions(out factionEnemies, out factionFriends);
            if (factionEnemies == null || factionFriends == null)
                return false;

            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            int tile = default(int);
            if (!RimWorld.Planet.TileFinder.TryFindNewSiteTile(out tile, distance.min, distance.max, false))
                return false;

            Site site;

            SiteCoreDef core;
            float chance = Rand.Value;
            if (chance < 0.25f)
                core = SiteCoreDefOf.ItemStash;
            else if (chance < 0.35f)
                core = SiteCoreDefOf.PreciousLump;
            else
                core = SiteCoreDefOf.Nothing;

            List<SitePartDef> parts = new List<SitePartDef>();
            if (Rand.Value > 0.60f)
                parts = GetRandomSitePartDefs;

            // And allways add the Misc_Battlefield part
            parts.Add(DefDatabase<SitePartDef>.GetNamed("Misc_Battlefield"));

            site = SiteMaker.TryMakeSite(core, parts, tile, false);
            
            if (site != null)
            {
                // Try to add a railgun :)
                Thing railgun = null;
                ThingDef railgunDef = DefDatabase<ThingDef>.GetNamedSilentFail("Gun_RailgunMKI");
                if (railgunDef != null && Rand.Value < 0.15)
                {
                    railgun = ThingMaker.MakeThing(railgunDef);

                    ItemStashContentsComp itemStash = site.GetComponent<ItemStashContentsComp>();
                    if (itemStash != null && site.core.def == SiteCoreDefOf.ItemStash ) 
                    {
                        if (railgun != null)
                            itemStash.contents.TryAdd(railgun);
                    }
                }

                // Add a site timeout ???
                site.GetComponent<TimeoutComp>().StartTimeout(Rand.RangeInclusive(10, 30) * 60000);

                Find.WorldObjects.Add(site);
                Find.LetterStack.ReceiveLetter("Misc_Incident_Label_Battlefield".Translate(), "Misc_Incident_Message_Battlefield".Translate(), LetterDefOf.NeutralEvent, site);

                return true;
            }
            return false;
        }
    }
}
