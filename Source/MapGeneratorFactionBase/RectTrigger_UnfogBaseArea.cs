using System.Collections.Generic;

using Verse;
using RimWorld;

namespace MapGenerator
{
    /// <summary>
    /// This is a rect trigger that unfoggs an area, but only as far as the pawn can reach. Nonreachable areas stay fogged. 
    /// The trigger destroyes itself only after every fog in the area is revealed.
    /// </summary>
    public class RectTrigger_UnfogBaseArea : RectTrigger
    {
        
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.destroyIfUnfogged, "destroyIfUnfogged", true, false);
        }


        public override void Tick()
        {
            if (this.IsHashIntervalTick(60))
            {
                for (int i = Rect.minZ; i <= Rect.maxZ; i++)
                {
                    for (int j = Rect.minX; j <= Rect.maxX; j++)
                    {
                        IntVec3 c = new IntVec3(j, 0, i);
                        List<Thing> thingList = c.GetThingList(Map);
                        for (int k = 0; k < thingList.Count; k++)
                        {
                            if (thingList[k].def.category == ThingCategory.Pawn &&
                                thingList[k].def.race.intelligence == Intelligence.Humanlike &&
                                thingList[k].Faction == Faction.OfPlayer)
                            {
                                ActivatedBy((Pawn)thingList[k], Map);
                            }
                        }
                    }

                    if (destroyIfUnfogged && IsRectUnfogged(Map))// !Rect.Center.Fogged())
                    {
                        Destroy(DestroyMode.Vanish);
                        return;
                    }
                }
            }
        }

        private void ActivatedBy(Pawn p, Map map)
        {
            if (signalTag != null && signalTag != "")
                Find.SignalManager.SendSignal(new Signal(signalTag, p ));

            FloodFillerFog.FloodUnfog(p.Position, map);
            p.Position.GetRoom(map).Notify_RoomShapeOrContainedBedsChanged();

            if (!destroyIfUnfogged && !base.Destroyed)
            {
                Destroy(DestroyMode.Vanish);
            }
        }

        private void UnfogRect(Map map)
        {
            HashSet<Room> rooms = new HashSet<Room>();
            for (int i = Rect.minZ; i <= Rect.maxZ; i++)
            {
                for (int j = Rect.minX; j <= Rect.maxX; j++)
                {
                    IntVec3 cell = new IntVec3(j, 0, i);
                    map.fogGrid.Unfog(cell);
                    rooms.Add(cell.GetRoom(map));
                }
            }

            foreach (Room r in rooms)
            {
                r.Notify_RoomShapeOrContainedBedsChanged();
            }
            
        }

        private bool IsRectUnfogged(Map map)
        {
            foreach (IntVec3 cell in Rect.Cells)
            {
                if (map.fogGrid.IsFogged(cell))
                {
                    return false;
                }
            }
            return true;
        }

    }
}
