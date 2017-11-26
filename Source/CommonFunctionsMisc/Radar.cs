using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed
//using Verse.AI; // Needed when you do something with the AI
//using Verse.Sound; // Needed when you do something with the Sound


namespace CommonMisc
{
    /// <summary>
    /// This class bundles some functions to find pawns
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>Please check the provided license info for granted permissions.</permission>
    public class Radar
    {
        // Constructor
        public Radar() {}

        /// <summary>
        /// Find enemy pawns in reach and return a list
        /// </summary>
        /// <param name="Position">The starting position</param>
        /// <param name="Distance">The maximal distance to find targets</param>
        /// <returns></returns>
        public static IEnumerable<Pawn> FindEnemyPawns(IntVec3 Position, Map map, float Distance)
        {
            // LINQ version
            return map.mapPawns.AllPawnsSpawned.Where(p => p.HostileTo(Faction.OfPlayer) && !p.InContainerEnclosed && p.Position.InHorDistOf(Position, Distance));
        }
        /// <summary>
        /// Find enemy pawns in reach and return a list
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Pawn> FindEnemyPawns(Map map)
        {
            // LINQ version
            return map.mapPawns.AllPawnsSpawned.Where(p => p.HostileTo(Faction.OfPlayer));
        }
        /// <summary>
        /// Find enemy pawns and return the count
        /// </summary>
        /// <returns></returns>
        public static int FindEnemyPawnsCount(Map map)
        {
            IEnumerable<Pawn> pawns = FindEnemyPawns(map);
            if (pawns == null)
                return 0;
            else
                return pawns.Count();
        }


        /// <summary>
        /// Find friendly pawns in reach and return a list
        /// </summary>
        /// <param name="Position">The starting position</param>
        /// <param name="Distance">The maximal distance to find targets</param>
        /// <returns></returns>
        public static IEnumerable<Pawn> FindFriendlyPawns(IntVec3 Position, Map map, float Distance)
        {
            // LINQ version
            IEnumerable<Pawn> pawns = FindAllPawns(Position, map, Distance);
            if (pawns == null)
                return null;
            else
                return pawns.Where(p => !p.InContainerEnclosed && !p.Faction.HostileTo(Faction.OfPlayer) && !p.RaceProps.Animal && !p.IsColonist && !p.IsPrisonerOfColony);
        }        /// <summary>
        /// Find friendly pawns and return a list
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Pawn> FindFriendlyPawns(Map map)
        {
            // LINQ version
            IEnumerable<Pawn> pawns = FindAllPawns(map);
            if (pawns == null)
                return null;
            else
                return pawns.Where(p => !p.InContainerEnclosed && !IsHostile(p, Faction.OfPlayer) && !p.RaceProps.Animal && !p.IsColonist && !p.IsPrisonerOfColony);
        }
        /// Find friendly pawns and return the count
        /// </summary>
        /// <returns></returns>
        public static int FindFriendlyPawnsCount(Map map)
        {
            IEnumerable<Pawn> pawns = FindFriendlyPawns(map);
            if (pawns == null)
                return 0;
            else
                return pawns.Count();
        }


        /// <summary>
        /// Find friendly pawns in reach and return a list
        /// </summary>
        /// <param name="Position">The starting position</param>
        /// <param name="Distance">The maximal distance to find targets</param>
        /// <returns></returns>
        public static IEnumerable<Pawn> FindColonyAnimals(IntVec3 Position, Map map, float Distance)
        {
            // LINQ version
            IEnumerable<Pawn> pawns = FindAllPawns(Position, map, Distance);
            if (pawns == null)
                return null;
            else
                return pawns.Where(p => !p.InContainerEnclosed && !p.Dead && p.RaceProps.Animal && p.Faction == Faction.OfPlayer);
        }        /// <summary>
                 /// Find colony animals and return a list
                 /// </summary>
                 /// <returns></returns>
        public static IEnumerable<Pawn> FindColonyAnimals(Map map)
        {
            // LINQ version
            IEnumerable<Pawn> pawns = FindAllPawns(map);
            if (pawns == null)
                return null;
            else
                return pawns.Where(p => !p.InContainerEnclosed && !p.Dead && p.RaceProps.Animal && p.Faction == Faction.OfPlayer);
        }
        /// Find friendly pawns and return the count
        /// </summary>
        /// <returns></returns>
        public static int FindColonyAnimalsCount(Map map)
        {
            IEnumerable<Pawn> pawns = FindColonyAnimals(map);
            if (pawns == null)
                return 0;
            else
                return pawns.Count();
        }



        /// <summary>
        /// Find all pawns in reach and return an IEnumerable
        /// </summary>
        /// <param name="Position">The starting position</param>
        /// <param name="Distance">The maximal distance to find targets</param>
        /// <returns></returns>
        public static IEnumerable<Pawn> FindAllPawns(IntVec3 Position, Map map, float Distance)
        {
            // LINQ version
            return map.mapPawns.AllPawnsSpawned.Where(p => !p.InContainerEnclosed && p.Position.InHorDistOf(Position, Distance));
        }
        /// <summary>
        /// Find all pawns and return an IEnumerable
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Pawn> FindAllPawns(Map map)
        {
            // LINQ version
            return map.mapPawns.AllPawnsSpawned;
        }
        /// <summary>
        /// Find all pawns and return the count
        /// </summary>
        /// <returns></returns>
        public static int FindAllPawnsCount(Map map)
        {
            IEnumerable<Pawn> pawns = FindAllPawns(map);
            if (pawns == null)
                return 0;
            else
                return pawns.Count();
        }


        /// <summary>
        /// Find all colonist pawns in reach and return an IEnumerable
        /// </summary>
        /// <param name="Position">The starting position</param>
        /// <param name="Distance">The maximal distance to find targets</param>
        /// <returns></returns>
        public static IEnumerable<Pawn> FindColonistPawns(IntVec3 Position, Map map, float Distance)
        {
            // LINQ version
            return map.mapPawns.FreeColonistsSpawned.Where(p => !p.InContainerEnclosed && p.Position.InHorDistOf(Position, Distance));
        }
        /// <summary>
        /// Find all colonist pawns and return an IEnumerable
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Pawn> FindColonistPawns(Map map)
        {
            // LINQ version
            return map.mapPawns.FreeColonistsSpawned;
        }
        /// <summary>
        /// Find all colonist pawns and return the count
        /// </summary>
        /// <returns></returns>
        public static int FindColonistPawnsCount(Map map)
        {
            IEnumerable<Pawn> pawns = FindColonistPawns(map);
            if (pawns == null)
                return 0;
            else
                return pawns.Count();
        }


        /// <summary>
        /// Find all prisoner pawns in reach and return an IEnumerable
        /// </summary>
        /// <param name="Position">The starting position</param>
        /// <param name="Distance">The maximal distance to find targets</param>
        /// <returns></returns>
        public static IEnumerable<Pawn> FindPrisonerPawns(IntVec3 Position, Map map, float Distance)
        {
            // LINQ version
            return map.mapPawns.PrisonersOfColonySpawned.Where(p => !p.InContainerEnclosed && p.Position.InHorDistOf(Position, Distance));
        }
        /// <summary>
        /// Find all prisoner pawns and return an IEnumerable
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Pawn> FindPrisonerPawns(Map map)
        {
            // LINQ version
            return map.mapPawns.PrisonersOfColonySpawned;
        }
        /// <summary>
        /// Find all prisoner pawns and return the count
        /// </summary>
        /// <returns></returns>
        public static int FindPrisonerPawnsCount(Map map)
        {
            IEnumerable<Pawn> pawns = FindPrisonerPawns(map);
            if (pawns == null)
                return 0;
            else
                return pawns.Count();
        }


        /// <summary>
        /// Find all pawns in room and return an IEnumerable
        /// </summary>
        /// <param name="room">Find in which room</param>
        /// <returns></returns>
        public static IEnumerable<Pawn> FindAllPawnsInRoom(Room room)
        {
            // LINQ version
            return room.Map.mapPawns.AllPawnsSpawned.Where(p => !p.InContainerEnclosed && (room == RegionAndRoomQuery.RoomAt(p.Position, room.Map)));
        }
        /// <summary>
        /// Find all pawns in room within a certain distance and return an IEnumerable
        /// </summary>
        /// <param name="positon">The source position</param>
        /// <param name="distance">The maximal distance</param>
        /// <returns></returns>
        public static IEnumerable<Pawn> FindAllPawnsInRoom(IntVec3 position, Room room, float distance)
        {
            // LINQ version
            return room.Map.mapPawns.AllPawnsSpawned.Where(p => !p.InContainerEnclosed && 
                                                        (RegionAndRoomQuery.RoomAt(p.Position, room.Map) == room) && 
                                                        (p.Position.InHorDistOf(position, distance)));
        }
        /// <summary>
        /// Find all pawns in reach and return an IEnumerable
        /// </summary>
        /// <param name="room">Find in which room</param>
        /// <returns></returns>
        public static int FindAllPawnsInRoomCount(Room room)
        {
            IEnumerable<Pawn> pawns = FindAllPawnsInRoom(room);
            if (pawns == null)
                return 0;
            else
                return pawns.Count();
        }


        /// <summary>
        /// Find all animals nearby and return an IEnumerable
        /// </summary>
        /// <param name="position">The starting position.</param>
        /// <param name="distance">The max distance from the position, where the animals must be.</param>
        /// <returns></returns>
        public static IEnumerable<Pawn> FindAllAnimals(IntVec3 position, Map map, float distance)
        {
            // LINQ version
            return map.mapPawns.AllPawnsSpawned.Where(p => !p.InContainerEnclosed && p.RaceProps.Animal && p.Position.InHorDistOf(position, distance));
        }


        /// <summary>
        /// Check if the target is hostile
        /// </summary>
        /// <param name="baseThing">Source Thing</param>
        /// <param name="targetThing">Target Thing</param>
        /// <returns></returns>
        public static bool IsHostile(Thing baseThing, Thing targetThing)
        {
            return GenHostility.HostileTo(baseThing, targetThing.Faction);
        }
        /// <summary>
        /// Check if the target is hostile
        /// </summary>
        /// <param name="baseThing">Source Thing</param>
        /// <param name="targetFaction">Target Faction</param>
        /// <returns></returns>
        public static bool IsHostile(Thing baseThing, Faction targetFaction)
        {
            return GenHostility.HostileTo(baseThing, targetFaction);
        }


        /// <summary>
        /// An easy function to check if it is day
        /// </summary>
        /// <returns></returns>
        public static bool IsDayTime(Map map)
        { 
            return (GenLocalDate.HourInteger(map) >= 5 && GenLocalDate.HourInteger(map) <= 19);
        }


        /// <summary>
        /// An easy function to check if it is night
        /// </summary>
        /// <returns></returns>
        public static bool IsNightTime(Map map)
        {
            return !IsDayTime(map);
        }


        /// <summary>
        /// Find all things in reach and return an IEnumerable
        /// </summary>
        /// <param name="position">The starting position</param>
        /// <param name="distance">The maximal distance to find targets</param>
        /// <returns></returns>
        public static IEnumerable<Thing> FindAllThings(IntVec3 position, Map map, float distance)
        {
            // LINQ version
            return map.listerThings.AllThings.Where(t => t.Position.InHorDistOf(position, distance));
        }

        /// <summary>
        /// Find defined things in reach and return an IEnumerable
        /// </summary>
        /// <param name="position">The starting position</param>
        /// <returns></returns>
        public static IEnumerable<Thing> FindThingsInRoom(IntVec3 position, Map map)
        {
            Room room = RegionAndRoomQuery.RoomAt(position, map);
            // LINQ version
            return map.listerThings.AllThings.Where(t => room == RegionAndRoomQuery.RoomAt(t.Position, map));
        }
        /// <summary>
        /// Find defined things in reach and return an IEnumerable
        /// </summary>
        /// <param name="position">The starting position</param>
        /// <param name="room">The containing room</param>
        /// <returns></returns>
        public static IEnumerable<Thing> FindThingsInRoom(IntVec3 position, Room room)
        {
            // LINQ version
            return room.Map.listerThings.AllThings.Where(t => room == RegionAndRoomQuery.RoomAt(t.Position, room.Map));
        }
        /// <summary>
        /// Find defined things in reach and return an IEnumerable
        /// </summary>
        /// <param name="position">The starting position</param>
        /// <param name="room">The containing room</param>
        /// <param name="distance">The maximal distance to find targets</param>
        /// <returns></returns>
        public static IEnumerable<Thing> FindThingsInRoom(IntVec3 position, Map map, float distance)
        {
            Room room = RegionAndRoomQuery.RoomAt(position, map);
            // LINQ version
            return map.listerThings.AllThings.Where(t => t.Position.InHorDistOf(position, distance) && room == RegionAndRoomQuery.RoomAt(t.Position, map));
        }
        /// <summary>
        /// Find defined things in reach and return an IEnumerable
        /// </summary>
        /// <param name="position">The starting position</param>
        /// <param name="room">The containing room</param>
        /// <param name="distance">The maximal distance to find targets</param>
        /// <returns></returns>
        public static IEnumerable<Thing> FindThingsInRoom(IntVec3 position, Room room, float distance)
        {
            // LINQ version
            return room.Map.listerThings.AllThings.Where(t => t.Position.InHorDistOf(position, distance) && room == RegionAndRoomQuery.RoomAt(t.Position, room.Map));
        }


        /// <summary>
        /// Checks if a pawn is outdoors
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static bool IsOutdoors(Pawn pawn, bool countRooflessAsOutdoors = false)
        {
            if (countRooflessAsOutdoors)
            {
                return !pawn.Map.roofGrid.Roofed(pawn.Position);
            }
            else
            {
                Room room = pawn.Position.GetRoom(pawn.Map);
                return IsOutdoors(room); 
            }
        }
        /// <summary>
        /// Checks if a room is outdoors
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static bool IsOutdoors(Room room)
        {
            return (room != null && room.TouchesMapEdge);
        }
        
    }
}
