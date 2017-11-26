using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine; // Always needed
using RimWorld; // Need
using Verse; // Needed


namespace BeeAndHoney
{
    public static class BeeAndHoneyUtility
    {

        /// <summary>
        /// Checks if a 2D cell is inside a defined radius (without using Squareroots)
        /// </summary>
        /// <param name="checkCell">The cell to check if its in range</param>
        /// <param name="centerOfRadius">The center cell of the radius</param>
        /// <param name="radius">The radius</param>
        /// <returns></returns>
        public static bool IsCellInRadius(IntVec3 checkCell, IntVec3 centerOfRadius, float radius)
        {
            // True when '<' means it is inside the radius
            // True when '==' means it is on the radius border
            // True when '>' means it is outside the radius

            return Mathf.Pow(checkCell.x - centerOfRadius.x, 2) + Mathf.Pow(checkCell.z - centerOfRadius.z, 2) <= Mathf.Pow(radius, 2);
        }


        /// <summary>
        /// Get all cells inside a defined radius (without using Squareroots)
        /// </summary>
        /// <param name="center">The center cell</param>
        /// <param name="radius">The radius</param>
        /// <returns></returns>
        public static IEnumerable<IntVec3> CalculateAllCellsInsideRadius(IntVec3 center, Map map, int radius)
        {

            for (int z = -radius; z <= radius; z++)
                for (int x = -radius; x <= radius; x++)
                {
                    IntVec3 cell = new IntVec3(center.x + x, center.y, center.z + z);
                    if ((x * x) + (z * z) <= (radius * radius) && GenGrid.InBounds(cell, map))
                        yield return cell;
                }
        }


    }
}
