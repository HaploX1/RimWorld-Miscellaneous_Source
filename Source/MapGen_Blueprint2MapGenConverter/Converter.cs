using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Blueprint2MapGenConverter
{
    public class Converter
    {

        public static bool FillMiscBlueprintFromFluffyBlueprint(Fluffy_Blueprint source, ref Misc_Blueprint target)
        {
            if (source == null)
                return false;
            if (target == null)
                target = new Misc_Blueprint();


            target.size = source.size;
            //target.defName = source.name;


            // Fill legend data
            List<Misc_Blueprint.LegendData> buildingLegend = null;
            List<Misc_Blueprint.LegendData> rotationLegend = null;
            List<Misc_Blueprint.LegendData> floorLegend = null;
            Dictionary<string, string> mappingBuildingRotation2LegendKey = null;
            Dictionary<string, string> mappingFloor2LegendKey = null;

            FillLegendData(source, ref buildingLegend, ref rotationLegend, ref floorLegend,
                                    ref mappingBuildingRotation2LegendKey, ref mappingFloor2LegendKey);

            // fill data list
            List<string> buildingDataList = GetMapBase(source.size);
            List<string> floorDataList = GetMapBase(source.size);

            FillDataList(source, ref buildingDataList, ref floorDataList,
                                 ref mappingBuildingRotation2LegendKey, ref mappingFloor2LegendKey);

            // Data must be inverted
            buildingDataList = InvertList(buildingDataList);
            floorDataList = InvertList(floorDataList);

            StringBuilder buildingData = new StringBuilder();
            for (int i = 0; i < buildingDataList.Count; i++)
            {
                buildingData.AppendLine();
                buildingData.Append(buildingDataList[i]);
            }
            buildingData.AppendLine();
            target.buildingData = buildingData.ToString();
            target.buildingLegend = buildingLegend;
            target.rotationLegend = rotationLegend;

            StringBuilder floorData = new StringBuilder();
            for (int i = 0; i < floorDataList.Count; i++)
            {
                floorData.AppendLine();

                floorData.Append(floorDataList[i]);
            }
            floorData.AppendLine();
            target.floorData = floorData.ToString();
            target.floorLegend = floorLegend;



            // Add example for itemData
            target.itemData = buildingData.ToString();
            Misc_Blueprint.LegendData itemLegendData = new Misc_Blueprint.LegendData();
            itemLegendData.key = "s"; itemLegendData.value = "Silver";
            target.itemLegend.Add(itemLegendData);
            target.itemSpawnChance = 60;

            // Add example for pawnData
            target.pawnData = buildingData.ToString();
            Misc_Blueprint.LegendData pawnLegendData = new Misc_Blueprint.LegendData();
            pawnLegendData.key = "x"; pawnLegendData.value = "MercenarySniper";
            target.pawnLegend.Add(pawnLegendData);
            target.pawnSpawnChance = 50;


            
            // MAIN Presets
            target.defName = "TODO_-_Blueprint_needs_a_unique_defName";
            target.createdBy = "TODO_-_Please_enter_your_username_here";
            target.techLevelRequired = "Neolithic";
            target.chance = 10;
            //target.size = IntVec2.Invalid;
            target.canHaveHoles = false;
            target.createTrigger = false;



            buildingData = null;
            floorData = null;

            return true;
        }


        private static List<string> GetMapBase(IntVec2 size)
        {
            List<string> mapBaseWIP = new List<string>();

            string mapBaseRow = "";
            for (int x = 0; x < size.x; x++)
            {
                mapBaseRow += ".";
            }
            for (int y = 0; y < size.z; y++)
                mapBaseWIP.Add(mapBaseRow);

            return mapBaseWIP;
        }

        private static void FillLegendData(Fluffy_Blueprint source, ref List<Misc_Blueprint.LegendData> buildingLegend,
                                                                    ref List<Misc_Blueprint.LegendData> rotationLegend,
                                                                     ref List<Misc_Blueprint.LegendData> floorLegend,
                                                                    ref Dictionary<string, string> mappingBuildingRotation2LegendKey,
                                                                    ref Dictionary<string, string> mappingFloor2LegendKey)
        {
            string alphabet = "abcdefghijklmnopqrstuvwxyz";

            // Find all different items
            HashSet<string> buildingRotationHash = new HashSet<string>();
            HashSet<string> floorHash = new HashSet<string>();
            foreach (Fluffy_BlueprintElement element in source.contents)
            {
                if (element.ThingDef != null)
                {
                    // found building ( + stuff? ) + rotation
                    buildingRotationHash.Add(element.ThingDef + element.Rotation);

                }
                else if (element.TerrainDef != null)
                {
                    // found floor
                    floorHash.Add(element.TerrainDef);
                }
            }
            Dictionary<string, string> buildingRotationMap = new Dictionary<string, string>();
            for (int i = 0; i < buildingRotationHash.Count; i++)
            {
                buildingRotationMap.Add(buildingRotationHash.ElementAt(i), alphabet[i].ToString());
            }
            Dictionary<string, string> floorMap = new Dictionary<string, string>();
            for (int i = 0; i < floorHash.Count; i++)
            {
                floorMap.Add(floorHash.ElementAt(i), alphabet[i].ToString());
            }


            // Preparation done, now fill the legend data
            buildingLegend = new List<Misc_Blueprint.LegendData>();
            rotationLegend = new List<Misc_Blueprint.LegendData>();
            floorLegend = new List<Misc_Blueprint.LegendData>();

            mappingBuildingRotation2LegendKey = new Dictionary<string, string>();
            mappingFloor2LegendKey = new Dictionary<string, string>();

            foreach (Fluffy_BlueprintElement element in source.contents)
            {
                if (element.ThingDef != null)
                {
                    string keyMap = element.ThingDef + element.Rotation;
                    string key = buildingRotationMap[keyMap];
                    bool found = false;
                    foreach (Misc_Blueprint.LegendData ld in buildingLegend)
                    {
                        if (ld.key == key)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        Misc_Blueprint.LegendData buildingLD = new Misc_Blueprint.LegendData(key, element.ThingDef);
                        buildingLegend.Add(buildingLD);

                        Misc_Blueprint.LegendData rotationLD = new Misc_Blueprint.LegendData(key, element.Rotation.ToString());

                        rotationLegend.Add(rotationLD);
                        mappingBuildingRotation2LegendKey.Add(keyMap, key);
                    }
                }
                if (element.TerrainDef != null)
                {
                    string keyMap = element.TerrainDef;
                    string key = floorMap[keyMap];
                    bool found = false;
                    foreach (Misc_Blueprint.LegendData ld in floorLegend)
                    {
                        if (ld.key == key)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        Misc_Blueprint.LegendData floorLD = new Misc_Blueprint.LegendData(key, element.TerrainDef);

                        floorLegend.Add(floorLD);
                        mappingFloor2LegendKey.Add(keyMap, key);
                    }
                }
            }
        }

        private static void FillDataList(Fluffy_Blueprint source, ref List<string> buildingDataList,
                                                             ref List<string> floorDataList,
                                                             ref Dictionary<string, string> mappingBuildingRotation2LegendKey,
                                                             ref Dictionary<string, string> mappingFloor2LegendKey)
        {
            IntVec2 offset = source.size / 2;
            if (source.size.x % 2 != 0)
                offset.x += 1;
            if (source.size.z % 2 != 0)
                offset.z += 1;

            foreach (Fluffy_BlueprintElement element in source.contents)
            {
                if (element.ThingDef != null)
                {
                    // found building ( + stuff? ) + rotation
                    IntVec2 pos = element.Position.ToIntVec2 + offset;
                    string c = null;
                    foreach (KeyValuePair<string, string> kv in mappingBuildingRotation2LegendKey)
                    {
                        if (kv.Key == element.ThingDef + element.Rotation)
                        {
                            c = kv.Value;
                            break;
                        }
                    }
                    if (c == null)
                        continue;

                    string rowData = buildingDataList[pos.z - 1];
                    string pre, post, old = "";

                    old = rowData.Substring(pos.x - 1, 1);
                    if (old != ".") continue; // Here is already something! Do not replace it (first entry remains)!

                    pre = rowData.Substring(0, pos.x - 1);
                    post = rowData.Substring(pos.x);
                    rowData = pre + c + post;
                    buildingDataList[pos.z - 1] = rowData;
                }
                else if (element.TerrainDef != null)
                {
                    // found floor
                    IntVec2 pos = element.Position.ToIntVec2 + offset;
                    string c = null;
                    foreach (KeyValuePair<string, string> kv in mappingFloor2LegendKey)
                    {
                        if (kv.Key == element.TerrainDef)
                        {
                            c = kv.Value;
                            break;
                        }
                    }
                    if (c == null)
                        continue;

                    string rowData = floorDataList[pos.z - 1];
                    string pre, post, old = "";

                    old = rowData.Substring(pos.x - 1, 1);
                    if (old != ".") continue; // Here is already something! Do not replace it (first entry remains)!

                    pre = rowData.Substring(0, pos.x - 1);
                    post = rowData.Substring(pos.x);
                    rowData = pre + c + post;
                    floorDataList[pos.z - 1] = rowData;
                }
            }
        }

        public static List<string> InvertList(List<string> input)
        {
            if (input == null || input.Count == 0)
                return input;

            List<string> output = new List<string>();
            for (int i = input.Count - 1; i >= 0; i--)
                output.Add(input[i]);

            return output;
        }


    }
}
