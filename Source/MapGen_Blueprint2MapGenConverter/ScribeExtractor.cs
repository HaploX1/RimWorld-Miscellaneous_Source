using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace Blueprint2MapGenConverter
{
    public static class ScribeExtractor
    {
        public static T ValueFromNode<T>(XmlNode subNode, T defaultValue)
        {
            if (subNode == null)
            {
                return defaultValue;
            }
            T result;
            try
            {
                try
                {
                    result = (T)((object)Helper_Parsing.FromString(subNode.InnerText, typeof(T)));
                    return result;
                }
                catch //(Exception ex)
                {
                    //Log.Error(string.Concat(new object[]
                    //{
                    //    "Exception parsing node ",
                    //    subNode.OuterXml,
                    //    " into a ",
                    //    typeof(T),
                    //    ":\n",
                    //    ex.ToString()
                    //}));
                }
                result = default(T);
            }
            catch //(Exception arg)
            {
                //Log.Error("Exception loading XML: " + arg);
                result = defaultValue;
            }
            return result;
        }

        public static T SaveableFromNode<T>(XmlNode subNode, object[] ctorArgs)
        {
            if (subNode == null)
            {
                return default(T);
            }
            XmlAttribute xmlAttribute = subNode.Attributes["IsNull"];
            T result;
            if (xmlAttribute != null && xmlAttribute.Value == "True")
            {
                result = default(T);
            }
            else
            {
                try
                {
                    Type type;
                    //XmlAttribute xmlAttribute2 = subNode.Attributes["Class"];
                    //if (xmlAttribute2 != null)
                    //{
                    //    type = GenTypes.GetTypeInAnyAssembly(xmlAttribute2.Value);
                    //    if (type == null)
                    //    {
                    //        Log.Error(string.Concat(new object[]
                    //        {
                    //    "Could not find class ",
                    //    xmlAttribute2.Value,
                    //    " while resolving node ",
                    //    subNode.Name,
                    //    ". Trying to use ",
                    //    typeof(T),
                    //    " instead. Full node: ",
                    //    subNode.OuterXml
                    //        }));
                    //        type = typeof(T);
                    //    }
                    //}
                    //else
                    {
                        type = typeof(T);
                    }
                    if (type.IsAbstract)
                    {
                        throw new ArgumentException("Can't load abstract class " + type);
                    }
                    IExposable exposable = (IExposable)Activator.CreateInstance(type, ctorArgs);
                    //bool flag = typeof(T).IsValueType || typeof(Name).IsAssignableFrom(typeof(T));
                    //if (!flag)
                    //{
                    //    CrossRefResolver.RegisterForCrossRefResolve(exposable);
                    //}
                    XmlNode curParent = Scribe.curParent;
                    Scribe.curParent = subNode;
                    exposable.ExposeData();
                    Scribe.curParent = curParent;
                    //if (!flag)
                    //{
                    //    PostLoadInitter.RegisterForPostLoadInit(exposable);
                    //}
                    result = (T)((object)exposable);
                }
                catch (Exception ex)
                {
                    result = default(T);
                    throw new InvalidOperationException(string.Concat(new object[]
                    {
                "SaveableFromNode exception: ",
                ex,
                "\nSubnode:\n",
                subNode.OuterXml
                    }));
                }
            }
            return result;
        }


    }
}
