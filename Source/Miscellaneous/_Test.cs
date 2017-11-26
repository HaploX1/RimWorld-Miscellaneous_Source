using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;


//namespace ScarRemoving
//{

//    public class Recipe_RemoveHediff_wo_Brain : Recipe_RemoveHediff
//    {

//        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
//        {
//            List<Hediff> allHediffs = pawn.health.hediffSet.hediffs;
//            for (int i = 0; i < allHediffs.Count; i++)
//            {
//                if (allHediffs[i].Part != null)
//                {
//                    if (allHediffs[i].def == recipe.removesHediff && 
//                        allHediffs[i].Part.def.defName != "Brain")
//                    {
//                        yield return allHediffs[i].Part;
//                    }
//                }
//            }
//        }

//    }
//}






//namespace RimWorld
//{
//    public static class GenDate // This file uses A12 default values.
//    {
//        public const int TicksPerRealSecond = 60;
//        public const float SecondsPerTickAsFractionOfDay = 2f;
//        public static int TicksPerHour = 1250;
//        public static int HoursPerDay = 24;

//        // This section will allow you to change the names of a month and the count of the days.
//        public static List<MonthInfo> Months = new List<MonthInfo>() {
//            new MonthInfo { name="January", dayCount= 10 }, new MonthInfo { name = "Februrary", dayCount = 10 },
//            new MonthInfo { name="March", dayCount= 10 }, new MonthInfo { name = "April", dayCount = 10 },
//            new MonthInfo { name="May", dayCount= 10 }, new MonthInfo { name = "June", dayCount = 10 },
//            new MonthInfo { name="July", dayCount= 10 }, new MonthInfo { name = "August", dayCount = 10 },
//            new MonthInfo { name="September", dayCount= 10 }, new MonthInfo { name = "October", dayCount = 10 },
//            new MonthInfo { name="November", dayCount= 10 }, new MonthInfo { name = "December", dayCount = 10 },
//        };

//        public static int MonthsPerYear
//        {
//            get
//            { 
//                return Months.Count;
//            }
//        }

//        public static int DefaultStartingYear = 5500;
//        public static bool LeapYear = false;
//        public static int YearsToLeap = 4; // Every 4 years, add a day to the months listed below.
//        public static List<string> LeapMonths = new List<string>() { "February" }; // An attempt at making a default list that will be overriden by the XML's list.
//    }

//    public class MonthInfo
//    {
//        public string name;
//        public int dayCount;

//    }
//}
