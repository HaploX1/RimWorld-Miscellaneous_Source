using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using Verse.AI;
using Verse.AI.Group;
using RimWorld.BaseGen;

namespace MapGenerator
{
    public class GenStep_CreateBlueprintBaseClass : GenStep_Scatterer
    {
        public override int SeedPart
        {
            get
            {
                return 131314176;
            }
        }

        protected static DateTime usedCells_lastChange = DateTime.UtcNow;
        protected static HashSet<IntVec3> usedCells = new HashSet<IntVec3>();

        // RimWorld 1.7: large vanilla structures (ancient complexes, gravship wreckage, orbital
        // mechhives, ...) register their footprint in MapGenerator.UsedRects and reserve it with
        // map.layoutStructureSketches. The base scatterer's CanScatterAt only rejects the sketch
        // center cell, not a whole blueprint rect, so the mod checks both itself before placing.
        protected static bool OverlapsVanillaStructure(CellRect rect, Map map)
        {
            // Layout sketches (ancient complex etc.) — same check vanilla's own
            // GenStep_Scatterer.CanScatterAt performs for a single cell, here for the full rect.
            foreach (LayoutStructureSketch sketch in map.layoutStructureSketches)
            {
                if (rect.Overlaps(sketch.layoutSketch.OccupiedRect))
                    return true;
            }

            // UsedRects (critical + non-critical large structures).
            // Note: vanilla's Verse.MapGenerator is shadowed by our namespace name, so qualify it.
            if (Verse.MapGenerator.TryGetVar<List<CellRect>>(Verse.MapGenerator.UsedRectsName, out List<CellRect> usedRects))
            {
                foreach (CellRect usedRect in usedRects)
                {
                    if (rect.Overlaps(usedRect))
                        return true;
                }
            }
            return false;
        }


        // ---------------------------------------------------------------------
        // Distance to the player start spot
        // ---------------------------------------------------------------------

        /// <summary>
        /// Minimum distance in cells (Chebyshev) a blueprint has to keep from the player
        /// start spot: blueprints may be built up to this distance from the start, never
        /// closer. Set to 0 to allow blueprints right next to the start.
        /// </summary>
        protected const int MinDistToPlayerStart = 5;

        /// <summary>
        /// True when a single cell keeps the minimum distance to the player start spot.
        /// Used as the cheap pre-check of the cell finder, so the scatterer keeps looking
        /// for another cell instead of wasting the placement.
        /// </summary>
        protected static bool KeepsDistanceToPlayerStart(IntVec3 c, Map map)
        {
            if (!Verse.MapGenerator.PlayerStartSpotValid)
                return true;

            IntVec3 start = Verse.MapGenerator.PlayerStartSpot;
            if (!start.InBounds(map))
                return true;

            return Math.Max(Math.Abs(c.x - start.x), Math.Abs(c.z - start.z)) >= MinDistToPlayerStart;
        }

        /// <summary>
        /// True when the whole footprint of a blueprint keeps the minimum distance to the
        /// player start spot. This is the check that actually guarantees the rule, because
        /// a blueprint rectangle can reach towards the start even when its anchor cell is
        /// far away from it.
        /// </summary>
        protected static bool KeepsDistanceToPlayerStart(CellRect rect, Map map)
        {
            if (!Verse.MapGenerator.PlayerStartSpotValid)
                return true;

            IntVec3 start = Verse.MapGenerator.PlayerStartSpot;
            if (!start.InBounds(map))
                return true;

            int dx = 0;
            int dz = 0;

            if (start.x < rect.Min.x)
                dx = rect.Min.x - start.x;
            else if (start.x > rect.Max.x)
                dx = start.x - rect.Max.x;

            if (start.z < rect.Min.z)
                dz = rect.Min.z - start.z;
            else if (start.z > rect.Max.z)
                dz = start.z - rect.Max.z;

            return Math.Max(dx, dz) >= MinDistToPlayerStart;
        }

        // ---------------------------------------------------------------------
        // No-build zone along the map borders (configured per GenStepDef in XML)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Width in cells of the strip along each of the four map borders (north, east,
        /// south, west) in which no blueprint may be placed. Read from the GenStepDef XML
        /// as &lt;minDistanceFromBorder&gt;.
        /// </summary>
        public int minDistanceFromBorder = DefaultMinDistanceFromBorder;

        /// <summary>
        /// Maps whose width or height is smaller than this cannot spare the configured
        /// border and use FallbackNoBuildZoneBorder instead. Read from the GenStepDef XML
        /// as &lt;minMapSizeForMinDistance&gt;.
        /// </summary>
        public int minMapSizeForMinDistance = DefaultMinMapSizeForMinDistance;

        /// <summary>Value used when the XML does not configure minDistanceFromBorder.</summary>
        protected const int DefaultMinDistanceFromBorder = 60;

        /// <summary>Value used when the XML does not configure minMapSizeForMinDistance.</summary>
        protected const int DefaultMinMapSizeForMinDistance = 180;

        /// <summary>
        /// Hardcoded border for maps that are smaller than minMapSizeForMinDistance in at
        /// least one dimension.
        /// </summary>
        protected const int FallbackNoBuildZoneBorder = 30;

        /// <summary>
        /// Effective width of the no-build border for a map: minDistanceFromBorder on maps
        /// that are at least minMapSizeForMinDistance wide *and* high, otherwise the
        /// hardcoded FallbackNoBuildZoneBorder. The result is clamped so that a usable
        /// area of at least one cell always remains, whatever the XML asks for.
        /// </summary>
        protected int ResolveNoBuildZoneBorder(Map map)
        {
            int border = minDistanceFromBorder;

            if (map.Size.x < minMapSizeForMinDistance || map.Size.z < minMapSizeForMinDistance)
                border = FallbackNoBuildZoneBorder;

            int maxBorder = (Math.Min(map.Size.x, map.Size.z) - 1) / 2;
            if (maxBorder < 0)
                maxBorder = 0;
            if (border > maxBorder)
                border = maxBorder;

            return border;
        }

        /// <summary>
        /// True when a single cell lies outside the no-build border. Cheap pre-check for the
        /// cell finder, so it keeps looking for another spot instead of wasting the placement.
        /// </summary>
        protected bool KeepsBorderDistance(IntVec3 c, Map map)
        {
            int border = ResolveNoBuildZoneBorder(map);

            return c.x >= border && c.z >= border
                && c.x <= map.Size.x - 1 - border
                && c.z <= map.Size.z - 1 - border;
        }

        /// <summary>
        /// True when the whole footprint of a blueprint lies outside the no-build border.
        /// This is the check that actually guarantees the rule, because a blueprint
        /// rectangle reaches towards a border even when its anchor cell is far away from it.
        /// </summary>
        protected bool KeepsBorderDistance(CellRect rect, Map map)
        {
            int border = ResolveNoBuildZoneBorder(map);

            return rect.Min.x >= border && rect.Min.z >= border
                && rect.Max.x <= map.Size.x - 1 - border
                && rect.Max.z <= map.Size.z - 1 - border;
        }

        /// <summary>
        /// Puts freshly spawned mechanoids to sleep, exactly like vanilla's sleeping-mech quest
        /// (QuestPart_SleepingMechs does the same): LordJob_SleepThenMechanoidsDefend keeps them
        /// on a SleepForever duty while CompCanBeDormant marks them dormant - they are neither
        /// attacked nor treated as threats - and the area signal wakes them when a pawn arrives.
        /// Needed because Pawn.mindState.Active is reset by Pawn.Notify_Unfogged, so without a
        /// lord a ruin's scythers assault the colony right after the map is generated.
        /// </summary>
        protected static void MakeMechanoidsDormant(List<Pawn> mechanoids, Map map, string wakeUpSignalTag)
        {
            if (mechanoids == null || mechanoids.Count == 0 || map == null)
                return;

            Faction faction = mechanoids[0].Faction ?? Faction.OfMechanoids;

            // The list doubles as the lord's defend targets; vanilla's lord job reads
            // things.First() when it wakes up, so it must not be empty.
            Lord lord = LordMaker.MakeNewLord(faction,
                new LordJob_SleepThenMechanoidsDefend(new List<Thing>(mechanoids), faction, 40f,
                                                     mechanoids[0].Position, canAssaultColony: true, isMechCluster: false),
                map);

            foreach (Pawn mechanoid in mechanoids)
            {
                lord.AddPawn(mechanoid);

                // Vanilla's mechanoid races ship CompWakeUpDormant with a 30 cell check radius but
                // wakeUpIfAnyTargetClose switched off, so a dormant mech never notices an approaching
                // colonist by itself. The shrine signal only reaches mechs within 40 cells of the pawn
                // that entered the ruin, which leaves mechs in side rooms or large villages asleep.
                // Enabling the proximity check per instance closes that gap: the first mech that sees
                // a colonist wakes, and its WakeUp() makes the lord run TransitionAction_WakeAll,
                // which wakes every pawn of the group.
                CompWakeUpDormant proximityWakeUp = mechanoid.TryGetComp<CompWakeUpDormant>();
                if (proximityWakeUp != null)
                    proximityWakeUp.wakeUpIfTargetClose = true;

                CompCanBeDormant dormant = mechanoid.TryGetComp<CompCanBeDormant>();
                if (dormant == null)
                    continue;

                // Wakes the mech when a pawn triggers the ruin's RectTrigger (the trigger
                // passes that pawn as SUBJECT, which CompCanBeDormant.Notify_SignalReceived
                // needs) - plus the lord's own triggers on harm.
                if (!wakeUpSignalTag.NullOrEmpty())
                    dormant.wakeUpSignalTag = wakeUpSignalTag;

                dormant.ToSleep();
            }
        }

        /// <summary>
        /// Clamps an anchor cell into the anchor area that keeps a placementSize footprint
        /// outside the no-build border. Returns IntVec3.Invalid when the footprint cannot fit
        /// into the usable area at all (border larger than the map allows for that size).
        /// </summary>
        protected IntVec3 ClampIntoNoBuildZone(IntVec3 loc, Map map, IntVec2 placementSize)
        {
            int border = ResolveNoBuildZoneBorder(map);

            int minX = border;
            int minZ = border;
            int maxX = map.Size.x - 1 - border - placementSize.x + 1;
            int maxZ = map.Size.z - 1 - border - placementSize.z + 1;

            if (maxX < minX || maxZ < minZ)
                return IntVec3.Invalid;

            return new IntVec3(Mathf.Clamp(loc.x, minX, maxX), loc.y, Mathf.Clamp(loc.z, minZ, maxZ));
        }

        /// <summary>
        /// Returns an anchor cell whose whole footprint lies outside the no-build border and
        /// which the given validator accepts when possible. The border rule itself is always
        /// enforced by clamping, so this never returns IntVec3.Invalid just because the
        /// original cell was too close to a border - a rejected placement would leave the map
        /// without a blueprint.
        /// </summary>
        protected IntVec3 AnchorInNoBuildZone(IntVec3 loc, Map map, IntVec2 placementSize,
                                              Func<IntVec3, bool> validator = null, int maxTries = 40)
        {
            // 1) The cell vanilla picked is fine as it is.
            if (KeepsBorderDistance(new CellRect(loc.x, loc.z, placementSize.x, placementSize.z), map)
                && (validator == null || validator(loc)))
                return loc;

            int border = ResolveNoBuildZoneBorder(map);
            int allowedWidth = map.Size.x - (2 * border) - placementSize.x + 1;
            int allowedHeight = map.Size.z - (2 * border) - placementSize.z + 1;
            if (allowedWidth <= 0 || allowedHeight <= 0)
                return IntVec3.Invalid;

            // 2) Clamp towards the map centre; keeps the intended spot as close as possible.
            IntVec3 clamped = ClampIntoNoBuildZone(loc, map, placementSize);
            if (validator == null || validator(clamped))
                return clamped;

            // 3) Spread out: random anchors inside the allowed area.
            CellRect allowedAnchors = new CellRect(border, border, allowedWidth, allowedHeight);
            for (int tryCount = 0; tryCount < maxTries; tryCount++)
            {
                IntVec3 candidate = allowedAnchors.RandomCell;
                if (validator(candidate))
                    return candidate;
            }

            // 4) Last resort: the clamped cell. The border rule wins; the caller still runs its
            // own placement checks (used cells, vanilla structures, ...) on the result.
            return clamped;
        }

        /// <summary>
        /// Logs the resolved border once per generated map, so a report about missing blueprints
        /// can be checked against the values that were actually used at runtime.
        /// </summary>
        public override void Generate(Map map, GenStepParams parms)
        {
            if (map != null)
            {
                Log.Message("Misc. MapGenerator -- " + GetType().Name + " on map " + map.Size.x + "x" + map.Size.z
                            + ": no-build border " + ResolveNoBuildZoneBorder(map)
                            + " (minDistanceFromBorder=" + minDistanceFromBorder
                            + ", minMapSizeForMinDistance=" + minMapSizeForMinDistance + ")");
            }

            base.Generate(map, parms);
        }

        /// <summary>
        /// Size of a blueprint after rotating it by quarterTurns * 90 degrees clockwise.
        /// A non-square blueprint swaps width and height.
        /// </summary>
        protected static IntVec2 RotatedSize(IntVec2 size, int quarterTurns)
        {
            return quarterTurns % 2 == 0 ? size : new IntVec2(size.z, size.x);
        }

        /// <summary>
        /// Maps every cell of a rotated blueprint back onto the cell of the blueprint data
        /// as authored. Index the result with (x + rotatedSize.x * z) of the rotated grid;
        /// the value is the item position inside the data strings, i.e. the same value the
        /// loops use for TryGetThingDefFromBuildingData(..) and friends. The mapping is a
        /// bijection, so every authored cell is used exactly once.
        /// </summary>
        protected static int[] BuildRotatedIndexMap(IntVec2 size, int quarterTurns, out IntVec2 rotatedSize)
        {
            int w = size.x;
            int h = size.z;
            rotatedSize = RotatedSize(size, quarterTurns);

            int rw = rotatedSize.x;
            int rh = rotatedSize.z;
            int[] sourceIndex = new int[rw * rh];

            for (int zn = 0; zn < rh; zn++)
            {
                for (int x = 0; x < rw; x++)
                {
                    int sx;
                    int sz;

                    switch (quarterTurns % 4)
                    {
                        case 1: // 90 degrees clockwise
                            sx = zn;
                            sz = h - 1 - x;
                            break;
                        case 2: // 180 degrees
                            sx = w - 1 - x;
                            sz = h - 1 - zn;
                            break;
                        case 3: // 270 degrees clockwise
                            sx = w - 1 - zn;
                            sz = x;
                            break;
                        default: // 0 degrees, as authored
                            sx = x;
                            sz = zn;
                            break;
                    }

                    sourceIndex[x + rw * zn] = sx + w * sz;
                }
            }
            return sourceIndex;
        }

        /// <summary>
        /// Rotates a thing together with the blueprint, so doors, beds, workbenches and
        /// every other rotatable building keeps the orientation the blueprint author gave
        /// it. A thing the blueprint has no rotation data for counts as north, which is
        /// what GenSpawn would have used anyway.
        /// </summary>
        protected static Rot4 RotateWithBlueprint(Rot4 thingRotation, int quarterTurns)
        {
            int rot = thingRotation.IsValid ? thingRotation.AsInt : Rot4.NorthInt;
            return new Rot4(rot + quarterTurns);
        }

        protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int count = 1)
        {
            throw new NotImplementedException();
        }
    }
}
