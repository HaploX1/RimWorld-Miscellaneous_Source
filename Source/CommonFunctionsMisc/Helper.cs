using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
//using Verse.Sound;
using RimWorld;
//using RimWorld.Planet;
//using RimWorld.SquadAI;


namespace CommonMisc
{
    /// <summary>
    /// Various helping functions
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>Please check the provided license info for granted permissions.</permission>
    public class Helper
    {

        /// <summary>
        /// Checks if the Dev-Mode is active
        /// </summary>
        /// <param name="programState"></param>
        /// <returns></returns>
        public static bool IsDevModeActive(ProgramState programState = ProgramState.Playing)
        {
            return Prefs.DevMode && Current.ProgramState == programState;
        }



        public static string ConvertFloatToTemperatureString(float value)
        {
            return value.ToStringTemperature("F0");
        }


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
            double nearestDistance = 99999.0d;
            Thing foundThing = null;

            if (things == null)
                return foundThing;

            //foreach (Thing t in things)
            for (int i = 0; i < things.Count; i++ )
            {
                Thing t = things[i];
                double dist = GetDistance(t.Position, pos);
                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    foundThing = t;
                }
            }

            return foundThing;
        }


        /// <summary>
        /// Find Pawns next or at the provided position
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static IEnumerable<Pawn> FindAllAdjacentPawnsToPosition(IntVec3 pos, Map map)
        {
            List<Pawn> pawns = map.mapPawns.AllPawns.ToList();

            for (int pc = 0; pc < pawns.Count; pc++)
            {
                Pawn pawn1 = pawns[pc];

                if (pawn1.Position == pos)
                {
                    yield return pawn1;
                    continue;
                }

                for (int i = 0; i < 4; i++)
                {
                    IntVec3 intVec3 = pos + GenAdj.CardinalDirections[i];
                    if (intVec3.InBounds(map))
                    {
                        if (pawn1.Position == intVec3)
                        {
                            yield return pawn1;
                            continue;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Get the distance between two points
        /// Caution: Uses Sqrt (slow calculation!)
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double GetDistance(IntVec3 p1, IntVec3 p2)
        {
            int X = Math.Abs(p1.x - p2.x);
            int Y = Math.Abs(p1.y - p2.y);
            int Z = Math.Abs(p1.z - p2.z);

            return Math.Sqrt(X * X + Y * Y + Z * Z);
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



        /// <summary>
        /// Checks if a 2D cell is inside a defined radius without using Squareroots
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
        /// Checks if a Vector2 is inside a defined radius without using Squareroots
        /// </summary>
        /// <param name="target">The cell to check if its in range</param>
        /// <param name="centerOfRadius">The center cell of the radius</param>
        /// <param name="radius">The radius</param>
        /// <returns></returns>
        public static bool IsCellInRadius(Vector2 target, Vector2 centerOfRadius, float radius)
        {
            // True when '<' means it is inside the radius
            // True when '==' means it is on the radius border
            // True when '>' means it is outside the radius

            return Mathf.Pow(target.x - centerOfRadius.x, 2) + Mathf.Pow(target.y - centerOfRadius.y, 2) <= Mathf.Pow(radius, 2);
        }



        /// <summary>
        /// Get all cells inside a defined radius (without using Squareroots)
        /// </summary>
        /// <param name="center">The center cell</param>
        /// <param name="radius">The radius</param>
        /// <returns></returns>
        public static IEnumerable<IntVec3> GetAllCellsInRadius(IntVec3 center, Map map, int radius)
        {

            for (int z = -radius; z <= radius; z++)
                for (int x = -radius; x <= radius; x++)
                {
                    IntVec3 cell = new IntVec3(center.x + x, center.y, center.z + z);
                    if ((x * x) + (z * z) <= (radius * radius) && GenGrid.InBounds(cell, map))
                        yield return cell;
                }
        }



        ///// <summary>
        ///// A small helper function to load materials (based on Tynans code)
        ///// </summary>
        ///// <param name="texturePath"></param>
        ///// <returns></returns>
        //public static Material LoadMaterial(string texturePath, Shader shader)
        //{
        //    //Texture2D texture2D = ContentFinder<Texture2D>.Get(texturePath, throwError);

        //    //if (texture2D == null)
        //    //    return null;

        //    //MaterialRequest materialRequest = new MaterialRequest(texture2D, ShaderDatabase.ShaderFromType(shaderType));

        //    return MaterialPool.MatFrom(materialRequest);
        //}


        public static float GetSlopePoint(float X, IntVec3 cell1, IntVec3 cell2)
        {
            return GetSlopePoint(X, cell1.x, cell1.y, cell2.x, cell2.y);
        }
        public static float GetSlopePoint(float X, float X1, float Y1, float X2, float Y2)
        {
            return (Y2 - Y1) / (X2 - X1) * (X - X1) + Y1;
        }


    }
}
