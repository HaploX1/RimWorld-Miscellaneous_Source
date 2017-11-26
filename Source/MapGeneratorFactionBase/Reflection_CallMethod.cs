using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection;

namespace MapGenerator
{
    public static class Reflection_Call_Method
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
            var mi = o.GetType().GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (mi != null)
            {
                return mi.Invoke(o, args);
            }
            return null;
        }
    }
}
