using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Verse;

namespace Patches_Misc_CE
{

    //Extracted from Combat Extended
    public class PatchOperationIsModActive : PatchOperation
    {
        private string modName = null;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            return !this.modName.NullOrEmpty() && ModsConfig.ActiveModsInLoadOrder.Any((ModMetaData m) => m.Name == this.modName);
        }
    }
}
