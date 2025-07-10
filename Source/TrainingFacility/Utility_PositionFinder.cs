using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace TrainingFacility
{

    public class Utility_PositionFinder
    {

        public static IntVec3 TryFindWatchBuildingPosition(Pawn pawn, Building building, IntRange standDistanceRange, bool desireToSit , out Thing chair)
        {
            chair = null;

            if (standDistanceRange.Equals(IntRange.Zero))
                return building.Position;

            List<IntVec3> foundCells = FindAllWatchBuildingCells(building, standDistanceRange);
            
            for (int j = 0; j < foundCells.Count; j++)
            {
                IntVec3 intVec = foundCells[j];
                    
                bool flag = false;
                if (desireToSit)
                {
                    chair = intVec.GetEdifice(pawn.Map);
                    if (chair != null && chair.def.building.isSittable && pawn.CanReserve(chair, 1))
                    {
                        flag = true;
                    }
                    else
                    {
                        // Not sittable or reservable => null
                        chair = null;
                    }
                }
                else if (intVec.InBounds(pawn.Map) && intVec.Standable(pawn.Map) && !intVec.IsForbidden(pawn) && pawn.CanReserve(intVec, 1) && pawn.Map.pawnDestinationReservationManager.CanReserve(intVec, pawn))
                {
                    flag = true;
                }

                if (flag && GenSight.LineOfSight(intVec, building.Position, pawn.Map, false))
                {
                    return intVec;
                }
            }
            
            return IntVec3.Invalid;
        }

        public static List<IntVec3> FindAllWatchBuildingCells(Building building, IntRange standDistanceRange)
        {
            return FindAllWatchBuildingCells(building.Position, building.Map, building.def.rotatable, building.Rotation, standDistanceRange);
        }

        public static List<IntVec3> FindAllWatchBuildingCells(IntVec3 center, Map map, bool rotatable, Rot4 rot, IntRange standDistanceRange)
        {
            List<IntVec3> foundIntVec3 = new List<IntVec3>();

            List<int> allowedDirections = new List<int>();
            if (rotatable)
            {
                allowedDirections.Add(rot.AsInt);
            }
            else
            {
                allowedDirections.Add(0);
                allowedDirections.Add(1);
                allowedDirections.Add(2);
                allowedDirections.Add(3);
            }
            for (int i = 0; i < allowedDirections.Count; i++)
            {
                int newRot = allowedDirections[i];
                Rot4 workRot = new Rot4(newRot);
                CellRect cellRect;
                if (workRot.IsHorizontal)
                {
                    int num = center.x + GenAdj.CardinalDirections[allowedDirections[i]].x * standDistanceRange.min;
                    int num2 = center.x + GenAdj.CardinalDirections[allowedDirections[i]].x * standDistanceRange.max;
                    int num3 = center.z + 1;
                    int num4 = center.z - 1;
                    cellRect = new CellRect(Mathf.Min(num, num2), num4, Mathf.Abs(num - num2) + 1, num3 - num4 + 1);
                }
                else
                {
                    int num5 = center.z + GenAdj.CardinalDirections[allowedDirections[i]].z * standDistanceRange.min;
                    int num6 = center.z + GenAdj.CardinalDirections[allowedDirections[i]].z * standDistanceRange.max;
                    int num7 = center.x + 1;
                    int num8 = center.x - 1;
                    cellRect = new CellRect(num8, Mathf.Min(num5, num6), num7 - num8 + 1, Mathf.Abs(num5 - num6) + 1);
                }
                IntVec3 centerCell = cellRect.CenterCell;
                int num9 = cellRect.Area * 4;
                for (int j = 0; j < num9; j++)
                {
                    IntVec3 intVec = centerCell + GenRadial.RadialPattern[j];
                    if (cellRect.Contains(intVec) && GenSight.LineOfSight(intVec, center, map, false))
                        foundIntVec3.Add(intVec);
                }
            }

            return foundIntVec3;
        }


        public static bool IsCellInRadius(IntVec3 checkCell, IntVec3 centerOfRadius, float radius)
        {
            // True when '<' means it is inside the radius
            // True when '==' means it is on the radius border
            // True when '>' means it is outside the radius

            return Mathf.Pow(checkCell.x - centerOfRadius.x, 2) + Mathf.Pow(checkCell.z - centerOfRadius.z, 2) <= Mathf.Pow(radius, 2);
        }
    }
}
