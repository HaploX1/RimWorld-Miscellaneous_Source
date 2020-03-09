using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Incidents
{
    public class SitePartWorker_Battlefield : SitePartWorker
    {
        public override string GetArrivedLetterPart(Map map, out LetterDef preferredLetterDef, out LookTargets lookTargets)
        {
            string arrivedLetterPart = base.GetArrivedLetterPart(map, out preferredLetterDef, out lookTargets);
            IEnumerable<Pawn> source = from x in map.mapPawns.AllPawnsSpawned
                                       where x.RaceProps.Humanlike
                                       select x;
            Pawn pawn = (from x in source
                         where x.GetLord() != null && x.GetLord().LordJob is LordJob_DefendBase
                         select x).FirstOrDefault();
            if (pawn == null)
            {
                pawn = source.FirstOrDefault();
            }
            lookTargets = pawn;
            return arrivedLetterPart;
        }

        public override string GetPostProcessedThreatLabel(Site site, SitePart sitePart)
        {
            return base.GetPostProcessedThreatLabel(site, sitePart) + " (" + GetPawnCount(site, sitePart.parms) + ")";
        }

        public override SitePartParams GenerateDefaultParams(float myThreatPoints, int tile, Faction faction)
        {
            SitePartParams parms = base.GenerateDefaultParams(myThreatPoints, tile, faction);
            parms.threatPoints = Mathf.Max(parms.threatPoints, FactionDefOf.AncientsHostile.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat));
            return parms;
        }

        private int GetPawnCount(Site site, SitePartParams parms)
        {
            PawnGroupMakerParms pawnGroupMakerParms1 = new PawnGroupMakerParms();
            pawnGroupMakerParms1.tile = site.Tile;
            pawnGroupMakerParms1.faction = Faction.OfAncients;
            pawnGroupMakerParms1.groupKind = PawnGroupKindDefOf.Combat;
            pawnGroupMakerParms1.points = parms.threatPoints;
            pawnGroupMakerParms1.seed = SleepingMechanoidsSitePartUtility.GetPawnGroupMakerSeed(parms);

            PawnGroupMakerParms pawnGroupMakerParms2 = new PawnGroupMakerParms();
            pawnGroupMakerParms2.tile = site.Tile;
            pawnGroupMakerParms2.faction = Faction.OfAncientsHostile;
            pawnGroupMakerParms2.groupKind = PawnGroupKindDefOf.Combat;
            pawnGroupMakerParms2.points = parms.threatPoints;
            pawnGroupMakerParms2.seed = SleepingMechanoidsSitePartUtility.GetPawnGroupMakerSeed(parms);

            return PawnGroupMakerUtility.GeneratePawnKindsExample(pawnGroupMakerParms1).Count() + PawnGroupMakerUtility.GeneratePawnKindsExample(pawnGroupMakerParms2).Count();
        }
    }
}
