using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed
using Verse.AI; // Needed when you do something with the AI
//using Verse.Sound; // Needed when you do something with the Sound

//using CommonMisc; // Helper classes

namespace Incidents
{
    public class IncidentWorker_RumorOf : IncidentWorker
    {
        private MapComponent_ColonistsOutsideMap_RumorOf mapComponent = null;

        /// <summary>
        /// Check, if the storyteller can use this
        /// </summary>
        /// <returns></returns>
        protected override bool CanFireNowSub(IIncidentTarget target)
        {
            Map map = (Map)target;
            // If MapComponent isn't available, try to add it
            if (!MapComponent_ColonistsOutsideMap_RumorOf.IsMapComponentAvailable(out mapComponent))
            {
                MapComponent_ColonistsOutsideMap_RumorOf.TryAddMapComponent();
            }

            // Not, if MapComponent isn't available
            if (!MapComponent_ColonistsOutsideMap_RumorOf.IsMapComponentAvailable(out mapComponent))
                return false;

            if (mapComponent.Active)
                return false;

            //IEnumerable<Pawn> friendlies = TryFindFriendlyPawns();
            //if (friendlies == null || friendlies.Count() == 0)
            //    return false;

            return true;
        }

        /// <summary>
        /// Find some friendlies on the map
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Pawn> TryFindFriendlyPawns(Map map)
        {
            IEnumerable<Pawn> friendlies = map.mapPawns.AllPawnsSpawned.Where(p =>
                                                                    !p.IsColonist &&
                                                                    !p.IsPrisonerOfColony &&
                                                                    !p.Faction.HostileTo(Faction.OfPlayer) &&
                                                                    !p.RaceProps.Animal);
            return friendlies;
        }

        /// <summary>
        /// If no visitor group is available, make your own and afterwards reexecute this incident
        /// </summary>
        /// <param name="parms"></param>
        /// <returns></returns>
        private bool TryExecuteVisitorsAndRestartSelf(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            IEnumerable<Faction> factions = Find.FactionManager.AllFactionsListForReading.Where(fac =>
                                                                   fac != Faction.OfPlayer && !fac.HostileTo(Faction.OfPlayer)); // && 
                                                                   // (float)GenDate.DaysPassed >= fac.def.earliestRaidDays );

            Faction faction = null;
            if (factions != null)
                faction = factions.RandomElement();
            else
            {
                //Log.Warning("IncidentWorker_RumorOf: Tried to create visitor group with null faction. Only possible, if there aren't anymore friendly factions available!");
                return true; // This will be handled as if it was successful.. ???
            }

            //Log.Error("OutdoorTemp:" + GenTemperature.OutdoorTemp.ToString() + " / SeasonalTemp:" + GenTemperature.SeasonalTemp.ToString() + " Faction:" + faction.def.defName);

            IntVec3 visitorSpawnCell = IntVec3.Invalid;
            CellFinder.TryFindRandomEdgeCellWith((IntVec3 c) => c.Standable(map) && map.reachability.CanReachColony(c), map, out visitorSpawnCell);

            IncidentParms VisitorParms = StorytellerUtility.DefaultParmsNow(Find.Storyteller.def, IncidentCategory.Misc, map);
            VisitorParms.forced = true;
            VisitorParms.faction = faction;
            VisitorParms.points = (float)Rand.Range(40, 140);
            VisitorParms.spawnCenter = visitorSpawnCell;
            VisitorParms.raidArrivalMode = PawnsArriveMode.EdgeWalkIn;
            
            // Execute Visitor Group
            int occurTick = Find.TickManager.TicksGame + 60;
            int occurTick2 = Find.TickManager.TicksGame + Rand.RangeInclusive(6500, 10000);

            QueuedIncident qi = new QueuedIncident(new FiringIncident(IncidentDefOf.VisitorGroup, null, VisitorParms), occurTick);

            //Log.Error("CanFireNow: " + qi.FiringIncident.def.Worker.CanFireNow().ToString());

            if (qi.FiringIncident.def.Worker.CanFireNow(map))
            {
                Find.Storyteller.incidentQueue.Add(qi);
            }
            else
            {
                VisitorParms.points /= 2;

                QueuedIncident qi_temp = new QueuedIncident(new FiringIncident(IncidentDef.Named("TraderCaravanArrival"), null, VisitorParms), occurTick);
                Find.Storyteller.incidentQueue.Add(qi_temp);
            }

            // Re-Execute Self
            parms.forced = true;
            QueuedIncident qi2 = new QueuedIncident(new FiringIncident(IncidentDef.Named(this.def.defName), null, parms), occurTick2);
            Find.Storyteller.incidentQueue.Add(qi2);
            
            return true;
        }

        public override bool TryExecute(IncidentParms parms)
        {

            // Do nothing, if MapComponent isn't available
            if (!MapComponent_ColonistsOutsideMap_RumorOf.IsMapComponentAvailable(out mapComponent))
                return false;

            Map map = parms.target as Map;
            if (map == null)
                return false;

            // Reinit a new story
            mapComponent.def = null;
            mapComponent.DoUpdateDef();

            // Set a new travel time
            mapComponent.SetNewTravelTime();

            IEnumerable<Pawn> friendlies = TryFindFriendlyPawns(map);
            //if (friendlies == null || friendlies.Count() == 0)
            //    return false;
            if (friendlies == null || friendlies.Count() == 0)
            {
                return TryExecuteVisitorsAndRestartSelf(parms);
            }

            Pawn pawnTellingRumor = friendlies.RandomElement();

            IntVec3 exitMapCell;
            Predicate<IntVec3> predicate = (IntVec3 c) => 
                {
                    if (!c.Standable(map) || !map.reachability.CanReachColony(c))
                        return false;

                    return true;
                };
            if (!CellFinder.TryFindRandomEdgeCellWith(predicate, map, out exitMapCell))
                return false;


            // Create initial selection dialog
            string str = mapComponent.def.InitialMessageVariable.Translate(new object[] { pawnTellingRumor.Name.ToStringFull, pawnTellingRumor.Faction.ToString() });
            DiaNode diaNode = new DiaNode( str.AdjustedFor( pawnTellingRumor ) );

            DiaOption diaOption = new DiaOption( mapComponent.def.InitialMessageButtonAcceptVariable.Translate() )
            {
                action = () =>
                {
                    mapComponent.ExitMapCell = exitMapCell;
                    mapComponent.IncidentPoints = parms.points;

                    // Create colonists selection dialog
                    Dialog_RumorOf_AssignColonists.CreateColonistSelectionDialog();
                },
                resolveTree = true
            };
            diaNode.options.Add( diaOption );

            string str1 = mapComponent.def.InitialMessageRejectedVariable.Translate(new object[] { pawnTellingRumor.NameStringShort });
            DiaNode diaNode1 = new DiaNode( str1.AdjustedFor( pawnTellingRumor ) );
            DiaOption diaOption1 = new DiaOption("OK".Translate())
            {
                resolveTree = true
            };
            diaNode1.options.Add(diaOption1);

            DiaOption diaOption2 = new DiaOption(mapComponent.def.InitialMessageButtonRejectVariable.Translate())
            {
                link = diaNode1
            };
            diaNode.options.Add(diaOption2);

            Find.WindowStack.Add( new Dialog_NodeTree( diaNode ) );
            return true;
        }




    }
}
