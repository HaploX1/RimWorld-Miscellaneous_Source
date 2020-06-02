using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Incidents
{
    public class GenStep_Battlefield : GenStep
    {
        public FloatRange defaultPointsRange = new FloatRange(600f, 1800f);

        private float chanceForMechanoids = 0.35f;

        // Mechanoid fight multiplicators
        float multiMechFight_Mechanoids = 0.75f;
        float multiMechFight_Industrial = 2.05f;
        float multiMechFight_Medieval = 2.75f;

        // Normal fight multiplicator (weaker friendlies)
        float multiNormalFight_Friendlies = 0.92f;

        private Faction enemyFaction, friendlyFaction;
        
        public override int SeedPart
        {
            get
            {
                return 313176313;
            }
        }

        public override void Generate(Map map, GenStepParams parms)
        {
            int cycle = 0;
            while (1==1)
            {
                cycle += 1;
                if (cycle > 20)
                    return;

                if (!TryFindFightingFactions(out enemyFaction, out friendlyFaction))
                    continue;

                // Check result for valid pawnGroupMakers --> This should normally not be invalid as it is checked in the faction finder, but someone still got invalid factions..
                if (enemyFaction == null || enemyFaction.def.pawnGroupMakers.NullOrEmpty() ||
                    friendlyFaction == null || friendlyFaction.def.pawnGroupMakers.NullOrEmpty())
                    continue;

                break;
            }

            float defaultPoints = defaultPointsRange.RandomInRange;
            if ((parms.sitePart != null) && parms.sitePart.parms.threatPoints < defaultPoints)
                parms.sitePart.parms.threatPoints = defaultPoints;

            GenStepParams parmsEnemy = parms;
            GenStepParams parmsFriendly = parms;

            float randomForMechanoids = Rand.Value;
            if (randomForMechanoids < chanceForMechanoids && parms.sitePart != null)
            {
                // 50% chance that the fighting faction against the mechanoid is an enemy of the colony
                if (Rand.Value > 0.5f)
                    friendlyFaction = enemyFaction;

                // define mechanoid side
                enemyFaction = Faction.OfMechanoids;
                parmsEnemy.sitePart.parms.threatPoints = parmsEnemy.sitePart.parms.threatPoints * multiMechFight_Mechanoids;

                // define human side
                if (friendlyFaction.def.techLevel > TechLevel.Medieval)
                    parmsFriendly.sitePart.parms.threatPoints = parmsFriendly.sitePart.parms.threatPoints * multiMechFight_Industrial;
                else
                    parmsFriendly.sitePart.parms.threatPoints = parmsFriendly.sitePart.parms.threatPoints * multiMechFight_Medieval;
            }
            else // --> not Mechanoid fighting
            {
                // Friendlies are a bit weaker!
                if (parmsFriendly.sitePart != null)
                    parmsFriendly.sitePart.parms.threatPoints = parmsFriendly.sitePart.parms.threatPoints * multiNormalFight_Friendlies;

                // 20% chance that friendlies are NOT the attacker but the defender
                // This is a bit more dangerous for the watching colonists as they might spawn directly next to you..
                if (Rand.Value < 0.20f)
                {
                    Faction tmpFaction = friendlyFaction;
                    friendlyFaction = enemyFaction;
                    enemyFaction = tmpFaction;

                    GenStepParams parmsTmp = parmsFriendly;
                    parmsFriendly = parmsEnemy;
                    parmsEnemy = parmsTmp;
                }
            }

            // Spawn side 1 (defending)
            CellRect rectToDefend1;
            IntVec3 singleCellToSpawnNear1;
            if (SiteGenStepUtility.TryFindRootToSpawnAroundRectOfInterest(out rectToDefend1, out singleCellToSpawnNear1, map))
            {
                if (rectToDefend1 == null || !rectToDefend1.InBounds(map) || !map.AllCells.Contains(rectToDefend1.CenterCell) || rectToDefend1.Cells.Count() == 0)
                    rectToDefend1 = CellRect.CenteredOn(map.Center, 10);

                List<Pawn> list = new List<Pawn>();
                foreach (Pawn item in this.GeneratePawns(parmsEnemy, map, this.enemyFaction))
                {
                    IntVec3 spawnCell;
                    if (!SiteGenStepUtility.TryFindSpawnCellAroundOrNear(rectToDefend1, singleCellToSpawnNear1, map, out spawnCell))
                    {
                        Find.WorldPawns.PassToWorld(item, PawnDiscardDecideMode.Decide);
                        break;
                    }
                    GenSpawn.Spawn(item, spawnCell, map, WipeMode.Vanish);
                    list.Add(item);
                }
                if (list.Any())
                {
                    if (this.enemyFaction == Faction.OfMechanoids)
                        //LordMaker.MakeNewLord(Faction.OfMechanoids, new LordJob_SleepThenAssaultColony(Faction.OfMechanoids), map, list);
                        LordMaker.MakeNewLord(Faction.OfMechanoids, new LordJob_DefendAndExpandHive(), map, list);
                    else
                        LordMaker.MakeNewLord(this.enemyFaction, new LordJob_DefendBase(this.enemyFaction, rectToDefend1.Cells.RandomElement()), map, list);

                    for (int i = 0; i < list.Count; i++)
                    {
                        list[i].jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                    }
                }
            }

            // Spawn side 2 (attacking)
            //CellRect rectToDefend2;
            //IntVec3 singleCellToSpawnNear2;
            IntVec3 spawnCellPawns;
            //if (SiteGenStepUtility.TryFindRootToSpawnAroundRectOfInterest(out rectToDefend2, out singleCellToSpawnNear2, map) && 
            //        RCellFinder.TryFindRandomPawnEntryCell(out spawnCellPawns, map, 0.5f))
            if (RCellFinder.TryFindRandomPawnEntryCell(out spawnCellPawns, map, 0.5f))
            {
                List<Pawn> list2 = new List<Pawn>();
                foreach (Pawn item in this.GeneratePawns(parmsFriendly, map, this.friendlyFaction))
                {
                    IntVec3 spawnCell;
                    //if (!SiteGenStepUtility.TryFindSpawnCellAroundOrNear(rectToDefend2, singleCellToSpawnNear2, map, out spawnCell))
                    if (!SiteGenStepUtility.TryFindSpawnCellAroundOrNear(rectToDefend1, spawnCellPawns, map, out spawnCell))
                    {
                        Find.WorldPawns.PassToWorld(item, PawnDiscardDecideMode.Decide);
                        break;
                    }
                    GenSpawn.Spawn(item, spawnCell, map, WipeMode.Vanish);
                    list2.Add(item);
                }
                if (list2.Any())
                {
                    LordMaker.MakeNewLord(this.friendlyFaction, new LordJob_AssistColony(this.friendlyFaction, rectToDefend1.Cells.RandomElement()), map, list2);
                    for (int i = 0; i < list2.Count; i++)
                    {
                        list2[i].jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                    }
                }
            }
        }

        private IEnumerable<Pawn> GeneratePawns(GenStepParams parms, Map map, Faction faction)
        {
            float points = (parms.sitePart == null) ? defaultPointsRange.RandomInRange : parms.sitePart.parms.threatPoints;
            PawnGroupMakerParms pawnGroupMakerParms = new PawnGroupMakerParms();
            pawnGroupMakerParms.groupKind = PawnGroupKindDefOf.Combat;
            pawnGroupMakerParms.tile = map.Tile;
            pawnGroupMakerParms.faction = faction;
            pawnGroupMakerParms.points = points;
            if (parms.sitePart != null)
                pawnGroupMakerParms.seed = SeedPart;
            return PawnGroupMakerUtility.GeneratePawns(pawnGroupMakerParms, true);
        }

        public static bool TryFindFightingFactions(out Faction enemyFaction, out Faction friendFaction)
        {
            bool active = true;
            int cycle = 0;

            enemyFaction = null;
            friendFaction = null;

            while (active)
            {
                cycle++;

                Faction tmpEnemyFaction = null;
                Faction tmpFriendFaction = null;

                bool foundEnemy = (from x in Find.FactionManager.AllFactionsListForReading
                                   where !x.IsPlayer && !x.defeated && x.HostileTo(Faction.OfPlayer) && !x.def.hidden && x.def.humanlikeFaction &&
                                            !x.def.pawnGroupMakers.NullOrEmpty()
                                   select x).TryRandomElement(out tmpEnemyFaction);

                if (tmpEnemyFaction == null)
                    return false;

                bool foundFriend = (from x in Find.FactionManager.AllFactionsListForReading
                                    where !x.IsPlayer && !x.defeated && !x.HostileTo(Faction.OfPlayer) && x.HostileTo(tmpEnemyFaction) && !x.def.hidden && x.def.humanlikeFaction &&
                                            !x.def.pawnGroupMakers.NullOrEmpty()
                                    select x).TryRandomElement(out tmpFriendFaction);

                if (foundEnemy && foundFriend)
                {
                    enemyFaction = tmpEnemyFaction;
                    friendFaction = tmpFriendFaction;
                    return true;
                }

                if (cycle <= 50)
                    continue;

                active = false;
                break;
            }

            return false;
        }

    }
}
