using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TrainingFacility
{
    public static class Utility_MaxAllowedTrainingLevel
    {

        public static int GetMaxAllowedTrainingLevel(Pawn pawn)
        {
            Vector2 v1 = new Vector2( 0,   1);
            Vector2 v2 = new Vector2( 6,   5);
            Vector2 v3 = new Vector2( 12,  8);
            Vector2 v4 = new Vector2( 15, 12);

            if (pawn?.ageTracker?.AgeBiologicalYears == null)
            {
                return 100;
            }
            if (pawn.ageTracker.AgeBiologicalYears < v2.x)
                return (int)Lerp(pawn.ageTracker.AgeBiologicalYears, v1, v2);
            if (pawn.ageTracker.AgeBiologicalYears < v3.x)
                return (int)Lerp(pawn.ageTracker.AgeBiologicalYears, v2, v3);
            if (pawn.ageTracker.AgeBiologicalYears < v4.x)
                return (int)Lerp(pawn.ageTracker.AgeBiologicalYears, v3, v4);

            return 100;
        }

        static public double Lerp(float x, Vector2 v0, Vector2 v1)
        {
            if ((v1.x - v0.x) == 0)
            {
                return (v0.y + v1.y) / 2;
            }
            return v0.y + (x - v0.x) * (v1.y - v0.y) / (v1.x - v0.x);
        }
    }
}
