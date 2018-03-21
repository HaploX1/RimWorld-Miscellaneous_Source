using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace Blueprint2MapGenConverter
{
    public static class ScribeMetaUtility
    {

        public static void WriteMetaHeader()
        {
            Scribe.EnterNode("meta");
            string currentVersionStringWithRev = "0.18.1722"; //VersionControl.CurrentVersionStringWithRev;
            Scribe_Values.LookValue<string>(ref currentVersionStringWithRev, "gameVersion", null, false);
            //List<string> list = (from mod in LoadedModManager.RunningMods
            //                     select mod.Identifier).ToList<string>();
            //Scribe_Collections.LookList<string>(ref list, "modIds", LookMode.Undefined, new object[0]);
            //List<string> list2 = (from mod in LoadedModManager.RunningMods
            //                      select mod.Name).ToList<string>();
            //Scribe_Collections.LookList<string>(ref list2, "modNames", LookMode.Undefined, new object[0]);
            Scribe.ExitNode();
        }

        public static bool ReadToMetaElement(XmlTextReader textReader)
        {
            return ScribeMetaUtility.ReadToNextElement(textReader) && ScribeMetaUtility.ReadToNextElement(textReader) && !(textReader.Name != "meta");
        }

        private static bool ReadToNextElement(XmlTextReader textReader)
        {
            while (textReader.Read())
            {
                if (textReader.NodeType == XmlNodeType.Element)
                {
                    return true;
                }
            }
            return false;
        }

    }
}
