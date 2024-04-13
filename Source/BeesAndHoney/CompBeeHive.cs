using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed


namespace BeeAndHoney
{
    public class CompBeeHive : CompHasGatherableBodyResource
    {

        private const int doUpdateAfterXTicks = 50;
        private List<Thing> foundThingsInt;

        protected override bool Active
        {
            get
            {
                return IsTemperatureGood();
                //return base.Active && IsTemperatureGood();
            }
        }

        private bool IsTemperatureGood()
        {
            if (!parent.Spawned || parent.Map == null)
                return false;

            float tempCell = GenTemperature.GetTemperatureForCell(this.parent.PositionHeld, parent.Map);
            tempCell = Mathf.RoundToInt(tempCell);
            return (tempCell > Props.activeTempRange.min && tempCell < Props.activeTempRange.max);
        }

        private Season lastSearchedSeason = Season.Undefined;
        private float lastFoundMulti = 1f;
        private float SeasonGrowthMultiplicator
        {
            get
            {
                if (!parent.Spawned || parent.Map == null)
                    return 0f;

                Season season = GenLocalDate.Season(parent.Map);

                if (season == lastSearchedSeason)
                    return lastFoundMulti;

                float multi = 1.0f;
                foreach (HoneySeasonMultiplicator honeyData in Props.seasonData)
                {
                    if (honeyData.season == season)
                    {
                        multi = honeyData.multi;
                        break;
                    }
                }

                lastFoundMulti = multi;
                lastSearchedSeason = season;

                return multi;
            }
        }
        
        
        //public float FullnessReflected
        //{
        //    set
        //    {
        //        // With this Reflection you can access a private variable! Here: The private float "fullness" is set 
        //        System.Reflection.FieldInfo fi = typeof(CompHasGatherableBodyResource).GetField("fullness", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        //        fi.SetValue(this, value);
        //    }
        //}

        
        public CompProperties_BeeHive Props
        {
            get
            {
                return (CompProperties_BeeHive)this.props;
            }
        }

        protected override int GatherResourcesIntervalDays
        {
            get
            {
                return Props.resourceIntervalDays;
            }
        }

        protected override string SaveKey
        {
            get
            {
                return "honeycombGrowth";
            }
        }

        protected override int ResourceAmount
        {
            get
            {
                if (Props == null || Props.resources == null)
                    return 0;

                return Props.resources.resourceCount;
            }
        }

        protected override ThingDef ResourceDef
        {
            get
            {
                if (Props == null || Props.resources == null)
                    return null;

                return Props.resources.resourceDef;
            }
        }


        //protected List<GatherableResources> Resources
        //{
        //    get
        //    {
        //        if (Props == null || Props.resources == null)
        //            return null;

        //        return Props.resources;
        //    }
        //}





        public CompBeeHive() {  }

        public override void PostExposeData()
        {
            base.PostExposeData();
        }


        public override void CompTick()
        {

            // Without enough flower plants nearby, reduce the resource increase per tick

            if (parent.Map == null || !parent.Spawned)
                return;

            if (parent.IsHashIntervalTick(doUpdateAfterXTicks) && Active)
            {
                float increaseResourceMultiplier = 0.25f;
                if (foundThingsInt != null && foundThingsInt.Count > Props.thingsCountMin)
                    increaseResourceMultiplier = 1f;

                float resourceIncreasePerTick = (1 / ((float)GatherResourcesIntervalDays * GenDate.TicksPerDay));

                // apply temperature multiplier
                resourceIncreasePerTick = resourceIncreasePerTick * increaseResourceMultiplier;

                // apply season multiplier
                resourceIncreasePerTick = resourceIncreasePerTick * SeasonGrowthMultiplicator;

                //Log.Error("resourceIncrease:" + resourceIncreasePerTick.ToString() + " | Fullness Pre:" + Fullness.ToString());

                //if (resourceIncreasePerTick > 0)
                    resourceIncreasePerTick = resourceIncreasePerTick * doUpdateAfterXTicks; // / 2;

                fullness = Fullness + resourceIncreasePerTick;

                if (Fullness > 1f)
                    fullness = 1f;
                if (Fullness < 0.01f)
                    fullness = 0.01f;
            }


            // ===== Update the available flowers every x ticks =====

            // No plant check at speed 3 or higher --> Changes are now in SearchFlowers
            //if (Find.TickManager.CurTimeSpeed >= TimeSpeed.Superfast)
            //    return;
            
            // Update flowers if needed
            SearchForFlowers(parent.Map);
        }
        
        public override string CompInspectStringExtra()
        {
            string addTemperature = "";
            if (!IsTemperatureGood())
            {
                addTemperature = string.Concat(new string[] { "\n", "OutOfIdealTemperatureRangeNotGrowing".Translate(), " ( ", Props.activeTempRange.min.ToStringTemperature("F0"), "~", Props.activeTempRange.max.ToStringTemperature("F0"), " )" });
            }

            string addCanHarvest = "";
            if (ActiveAndFull)
            {
                addCanHarvest = string.Concat(new string[] { "\n", "ReadyToHarvest".Translate(), });
            }

            return string.Concat(new string[] { "BeeAndHoney_AvailableFlowersInRange".Translate(), ": ",
                                                    foundThingsInt == null ? 0.ToString() : foundThingsInt.Count.ToString(), " / ",
                                                    Props == null ? "999" : Props.thingsCountMin.ToString(), "\n",
                                                "BeeAndHoney_HoneycombsGrowing".Translate(), ": ", Fullness < 0.1 ? "< 10%" : Fullness.ToStringPercent(),
                                                addTemperature, addCanHarvest });
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo c in base.CompGetGizmosExtra())
                yield return c;

            int baseGroupKey = 3137645;

            if (DebugSettings.godMode)
            {
                // Key-Binding - 
                Command_Action cmd1 = new Command_Action();
                cmd1.icon = null;
                cmd1.defaultDesc = "Debug: Honeycombs 99%";
                cmd1.hotKey = KeyBindingDefOf.Misc2; //H
                cmd1.activateSound = SoundDef.Named("Click");
                cmd1.action = delegate { fullness = 0.99f; };
                cmd1.Disabled = false;
                cmd1.disabledReason = "";
                cmd1.groupKey = baseGroupKey + 1;
                yield return cmd1;

                // Key-Binding - 
                Command_Action cmd2 = new Command_Action();
                cmd2.icon = null;
                cmd2.defaultDesc = "Debug: Search for flowers";
                cmd2.hotKey = KeyBindingDefOf.Misc3; //H
                cmd2.activateSound = SoundDef.Named("Click");
                cmd2.action = delegate { SearchForFlowers(parent.Map, true); };
                cmd2.Disabled = false;
                cmd2.disabledReason = "";
                cmd2.groupKey = baseGroupKey + 2;
                yield return cmd2;

                // Key-Binding - 
                Command_Action cmd3 = new Command_Action();
                cmd3.icon = null;
                cmd3.defaultDesc = "Debug: Show flowers";
                cmd3.hotKey = KeyBindingDefOf.Misc4; //H
                cmd3.activateSound = SoundDef.Named("Click");
                cmd3.action = delegate { ShowAllValidFlowers(parent.Map); };
                cmd3.Disabled = false;
                cmd3.disabledReason = "";
                cmd3.groupKey = baseGroupKey + 3;
                yield return cmd3;
            }


        }

        private void ShowAllValidFlowers(Map map)
        {
            if (foundThingsInt == null)
                return;

            foreach (Thing item in foundThingsInt)
            {
                //MoteMaker.MakeStaticMote(item.Position, map, ThingDefOf.Mote_FeedbackGoto);
                FleckMaker.Static(item.Position, map, FleckDefOf.FeedbackGoto);
            }
        }
        
        private void SearchForFlowers(Map map, bool forced = false)
        {
            int interval = Props.updateTicks;
            if (Find.TickManager.CurTimeSpeed >= TimeSpeed.Superfast && 
                    (   GenLocalDate.Season(parent.Map) == Season.Winter ||
                        (foundThingsInt != null && foundThingsInt.Count > 1) 
                    ) 
                )
                interval += 5000; // return;

            if (!forced && !Gen.IsHashIntervalTick(parent, interval))
                return;

            // Update flowers if needed
            IEnumerable<Thing> foundThings = FindValidThingsInRange(map);
            foundThingsInt = foundThings.ToList();
        }

        private IEnumerable<Thing> FindValidThingsInRange(Map map)
        {
            Room room = parent.GetRoom();

            // Does not block wind, is a plant, not a tree, not any kind of grass and ( harvestable or beauty > 2 )
            IEnumerable<Thing> foundThings = map.listerThings.AllThings.Where<Thing>(t => !t.def.blockWind &&
                                                                                            t.def.plant != null && !t.def.plant.IsTree &&
                                                                                            !t.def.defName.ToLower().Contains("grass") &&
                                                                                            (t.def.plant.Harvestable || t.GetStatValue(StatDefOf.Beauty, true) >= 2) &&
                                                                                            t.GetRoom() == room &&
                                                                                            (t as Plant) != null && (t as Plant).Growth > 0.07f);
            if (foundThings == null)
                yield break;

            foreach (Thing t in foundThings)
            {
                if (BeeAndHoneyUtility.IsCellInRadius(t.Position, this.parent.PositionHeld, Props.rangeThings))
                    yield return t;
            }
        }
    }
}
