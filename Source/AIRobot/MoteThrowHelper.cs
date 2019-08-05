using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using RimWorld;
using UnityEngine;

namespace AIRobot
{
    public static class MoteThrowHelper
    {

        public static Mote ThrowBatteryRed(Vector3 loc, Map map, float scale)
        {
            return ThrowBatteryXYZ(DefDatabase<ThingDef>.GetNamed("Mote_BatteryRed"), loc, map, scale);
        }

        public static Mote ThrowBatteryYellowYellow(Vector3 loc, Map map, float scale)
        {
            return ThrowBatteryXYZ(DefDatabase<ThingDef>.GetNamed("Mote_BatteryYellowYellow"), loc, map, scale);
        }

        public static Mote ThrowBatteryYellow(Vector3 loc, Map map, float scale)
        {
            return ThrowBatteryXYZ(DefDatabase<ThingDef>.GetNamed("Mote_BatteryYellow"), loc, map, scale);
        }

        public static Mote ThrowBatteryGreen(Vector3 loc, Map map, float scale)
        {
            return ThrowBatteryXYZ(DefDatabase<ThingDef>.GetNamed("Mote_BatteryGreen"), loc, map, scale);
        }


        public static Mote ThrowBatteryXYZ(ThingDef moteDef, Vector3 loc, Map map, float scale)
        {
            // Note: For code comparism look at RimWorld.MoteMaker.ThrowMetaIcon(..)
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.Saturated)
            {
                return null;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(moteDef);
            moteThrown.Scale = scale;
            moteThrown.rotationRate = (float)Rand.Range(-1, 1);
            moteThrown.exactPosition = loc;
            moteThrown.exactPosition += new Vector3(0.35f, 0f, 0.35f);
            moteThrown.exactPosition += new Vector3(Rand.Value, 0f, Rand.Value) * 0.1f;
            //moteThrown.SetVelocity((float)Rand.Range(0, 360), Rand.Range(0.35f, 0.55f));
            moteThrown.SetVelocity(Rand.Range(30f, 60f), Rand.Range(0.35f, 0.55f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
            return moteThrown;
        }

        public static Mote ThrowNoRobotSign(Vector3 loc, Map map, float scale)
        {
            // Note: For code comparism look at RimWorld.MoteMaker.ThrowMetaIcon(..)
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.Saturated)
            {
                return null;
            }

            ThingDef moteDef = DefDatabase<ThingDef>.GetNamedSilentFail("Mote_NoRobotSign");

            if (moteDef == null)
                return null;

            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(moteDef);
            moteThrown.Scale = scale;
            moteThrown.rotationRate = (float)Rand.Range(-1, 1);
            moteThrown.exactPosition = loc;
            moteThrown.exactPosition += new Vector3(0.35f, 0f, 0.35f);
            moteThrown.exactPosition += new Vector3(Rand.Value, 0f, Rand.Value) * 0.1f;
            //moteThrown.SetVelocity((float)Rand.Range(0, 360), Rand.Range(0.35f, 0.55f));
            moteThrown.SetVelocity(Rand.Range(30f, 60f), Rand.Range(0.15f, 0.35f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
            return moteThrown;
        }

    }
}
