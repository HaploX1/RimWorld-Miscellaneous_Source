using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace CommonMisc
{

    // This is a small Reflection helper class
    public static class Reflect<T>
    {
        private const BindingFlags fieldBindings =
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.GetField |
            BindingFlags.GetProperty;

        /// <summary>
        /// To get the value of a private/internal variable of a base object.
        /// Usage Example:
        /// public class Building_PowerPlantSimpleSolar : Building_PowerPlant {
        ///    readonly float FullSunPower = Reflect<Building_PowerPlantSolar>.GetStatic<float>("FullSunPower");
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static TResult GetValue<TResult>(string fieldName)
        {
            // To get the value of a private/internal variable of a base object
            // Usage example: 
            // public class Building_PowerPlantSimpleSolar : Building_PowerPlant {
            //    readonly float FullSunPower = Reflect<Building_PowerPlantSolar>.GetStatic<float>("FullSunPower");
            // ...

            return (TResult)typeof(T).InvokeMember(fieldName, fieldBindings, null, null, null);
        }

        /// <summary>
        /// To set a private/instance variable.
        /// Usage Example: 
        /// Reflect<CompFlickable>.SetValue(flickableComp, "wantSwitchOn", on);
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="target">The object which carries the field</param>
        /// <param name="fieldName">The name of the field</param>
        /// <param name="value">The value to write on the field</param>
        public static void SetValue(object target, string fieldName, object value, BindingFlags bindingFlags = BindingFlags.Default)
        {
            if (bindingFlags == BindingFlags.Default)
                bindingFlags = fieldBindings; 

            FieldInfo fieldInfo = GetFieldInfo(target, fieldName, bindingFlags);
            fieldInfo.SetValue(target, value);
        }

        public static object GetValue(object target, string fieldName, BindingFlags bindingFlags = BindingFlags.Default)
        {
            if (bindingFlags == BindingFlags.Default)
                bindingFlags = fieldBindings;

            FieldInfo fieldInfo = GetFieldInfo(target, fieldName, bindingFlags);
            return fieldInfo.GetValue(target);
        }


        public static FieldInfo GetFieldInfo(object target, string fieldName, BindingFlags bindingFlags = BindingFlags.Default)
        {
            if (bindingFlags == BindingFlags.Default)
                bindingFlags = fieldBindings;

            FieldInfo fieldInfo = typeof(T).GetField(fieldName, bindingFlags);
            return fieldInfo;
        }

    }
}
