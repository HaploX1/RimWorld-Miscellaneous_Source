using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace Blueprint2MapGenConverter
{

    public static class Scribe_Collections
    {
        public static void LookList<T>(ref List<T> list, string label, LookMode lookMode = LookMode.Undefined, params object[] ctorArgs)
        {
            Scribe_Collections.LookList<T>(ref list, false, label, lookMode, ctorArgs);
        }

        public static void LookList<T>(ref List<T> list, bool saveDestroyedThings, string label, LookMode lookMode = LookMode.Undefined, params object[] ctorArgs)
        {
            if (lookMode == LookMode.Undefined)
            {
                if (Helper_Parsing.HandlesType(typeof(T)))
                {
                    lookMode = LookMode.Value;
                }
                else
                {
                    lookMode = LookMode.Deep;
                }
            }
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                if (list == null && lookMode == LookMode.Reference)
                {
                    //Log.Warning(string.Concat(new object[]
                    //{
                    //    "Saving null list \"",
                    //    label,
                    //    "\" with look mode ",
                    //    lookMode,
                    //    ". This will cause bugs because null lists are not registered during loading so CrossRefResolver will break."
                    //}));
                }
                Scribe.EnterNode(label);
                if (list == null)
                {
                    Scribe.WriteAttribute("IsNull", "True");
                }
                else
                {
                    foreach (T current in list)
                    {
                        if (lookMode == LookMode.Value)
                        {
                            T t = current;
                            Scribe_Values.LookValue<T>(ref t, "li", default(T), true);
                        }
                        else if (lookMode == LookMode.Deep)
                        {
                            T t2 = current;
                            Scribe_Deep.LookDeep<T>(ref t2, "li", ctorArgs);
                        }
                    }
                }
                Scribe.ExitNode();
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (Scribe.curParent == null)
                {
                    //Log.Error("XmlHandling.curParent is null. I'm not sure why.");
                    list = null;
                    return;
                }
                XmlNode xmlNode = Scribe.curParent[label];
                if (xmlNode == null)
                {
                    list = null;
                    return;
                }
                XmlAttribute xmlAttribute = xmlNode.Attributes["IsNull"];
                if (xmlAttribute != null && xmlAttribute.Value.ToLower() == "true")
                {
                    list = null;
                    return;
                }
                if (lookMode == LookMode.Value)
                {
                    list = new List<T>(xmlNode.ChildNodes.Count);
                    foreach (XmlNode subNode in xmlNode.ChildNodes)
                    {
                        T item = ScribeExtractor.ValueFromNode<T>(subNode, default(T));
                        list.Add(item);
                    }
                }
                else if (lookMode == LookMode.Deep)
                {
                    list = new List<T>(xmlNode.ChildNodes.Count);
                    foreach (XmlNode subNode2 in xmlNode.ChildNodes)
                    {
                        T item2 = ScribeExtractor.SaveableFromNode<T>(subNode2, ctorArgs);
                        list.Add(item2);
                    }
                }
            }
        }
        
    }
}
