using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection;

namespace CommonMisc
{
    public static class Reflection__Call_Private_Method
    {

        /// <summary>
        /// Call a protected or private method via Reflection
        /// </summary>
        /// <param name="o">The object where the method can be found.</param>
        /// <param name="methodName">The name of the method to be called.</param>
        /// <param name="args">The arguments for the method.</param>
        /// <returns></returns>
        public static object ReflectCall(this object o, string methodName, params object[] args)
        {
            var mi = o.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (mi != null)
            {
                return mi.Invoke(o, args);
            }
            return null;
        }
        public static object ReflectCall(this string typeName, string methodName, params object[] args)
        {
            var mi = Type.GetType(typeName).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (mi != null)
            {
                return mi.Invoke(null, args);
            }
            return null;
        }

        public static object ReflectCall<T>(this string typeName, string methodName, params object[] args)
        {
            Type type = Type.GetType(typeName);
            object instance = Activator.CreateInstance(type);

            //MethodInfo mi = Type.GetType(typeName).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            //MethodInfo closed = mi.MakeGenericMethod(typeof(T));
            //MethodInfo definition = closed.GetGenericMethodDefinition();

            MethodInfo openMethod = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo closed = openMethod.MakeGenericMethod(typeof(T));
            if (closed != null)
                closed.Invoke(instance, args);

            //Type testType = typeof(Test);
            //object testInstance = Activator.CreateInstance(testType);

            //MethodInfo openMethod = testType.GetMethod("ShowType");
            //MethodInfo toInvoke = openMethod.MakeGenericMethod(typeof(int));
            //toInvoke.Invoke(testInstance, null);

            //if (definition != null)
            //{
            //    return definition.Invoke(null, args);
            //}
            return null;

        }

        public static object ReflectCall<T>(this Type type, string methodName, params object[] args)
        {
            object instance = Activator.CreateInstance(type);

            //MethodInfo mi = Type.GetType(typeName).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            //MethodInfo closed = mi.MakeGenericMethod(typeof(T));
            //MethodInfo definition = closed.GetGenericMethodDefinition();

            MethodInfo openMethod = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo closed = openMethod.MakeGenericMethod(typeof(T));
            if (closed != null)
                closed.Invoke(instance, args);

            //Type testType = typeof(Test);
            //object testInstance = Activator.CreateInstance(testType);

            //MethodInfo openMethod = testType.GetMethod("ShowType");
            //MethodInfo toInvoke = openMethod.MakeGenericMethod(typeof(int));
            //toInvoke.Invoke(testInstance, null);

            //if (definition != null)
            //{
            //    return definition.Invoke(null, args);
            //}
            return null;

        }
    }
}
