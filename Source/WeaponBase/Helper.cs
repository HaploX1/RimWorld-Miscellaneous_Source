using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;


namespace TurretWeaponBase
{
    // This is a copy of the common functions...
    public class WeaponBaseHelper
    {

        /// <summary>
        /// Find the thing, that is nearest to the position
        /// </summary>
        /// <param name="things"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static Thing FindNearestThing(IEnumerable<Thing> things, IntVec3 pos)
        {
            if (things == null)
                return null;

            return FindNearestThing(new List<Thing>(things), pos);
        }
        /// <summary>
        /// Find the thing, that is nearest to the position
        /// </summary>
        /// <param name="things"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static Thing FindNearestThing(List<Thing> things, IntVec3 pos)
        {
            double nearestDistance = double.MaxValue; //999999999.0d;
            Thing foundThing = null;

            if (things == null)
                return foundThing;

            //foreach (Thing t in things)
            for (int i = 0; i < things.Count; i++)
            {
                Thing t = things[i];
                double dist = GetDistanceSquared(t.Position, pos);
                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    foundThing = t;
                }
            }

            return foundThing;
        }


        /// <summary>
        /// For distance comparism this is better (faster)
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double GetDistanceSquared(IntVec3 p1, IntVec3 p2)
        {
            int X = Math.Abs(p1.x - p2.x);
            int Y = Math.Abs(p1.y - p2.y);
            int Z = Math.Abs(p1.z - p2.z);

            return (X * X + Y * Y + Z * Z);
        }

    }
}
