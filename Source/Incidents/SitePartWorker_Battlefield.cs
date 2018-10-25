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

        public override string GetPostProcessedDescriptionDialogue(Site site, SiteCoreOrPartBase siteCoreOrPart)
        {
            return string.Format(base.GetPostProcessedDescriptionDialogue(site, siteCoreOrPart), GetPawnCount(site, siteCoreOrPart.parms));
        }

        public override string GetPostProcessedThreatLabel(Site site, SiteCoreOrPartBase siteCoreOrPart)
        {
            return base.GetPostProcessedThreatLabel(site, siteCoreOrPart) + " (" + GetPawnCount(site, siteCoreOrPart.parms) + ")";
        }

        public override SiteCoreOrPartParams GenerateDefaultParams(Site site, float myThreatPoints)
        {
            SiteCoreOrPartParams siteCoreOrPartParams = base.GenerateDefaultParams(site, myThreatPoints);
            siteCoreOrPartParams.threatPoints = Mathf.Max(siteCoreOrPartParams.threatPoints, FactionDefOf.AncientsHostile.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat));
            return siteCoreOrPartParams;
        }

        private int GetPawnCount(Site site, SiteCoreOrPartParams parms)
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
