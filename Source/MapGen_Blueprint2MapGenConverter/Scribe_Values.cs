using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace Blueprint2MapGenConverter
{
    public static class Scribe_Values
    {
        public static void LookValue<T>(ref T value, string label, T defaultValue = default(T), bool forceSave = false)
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                //if (typeof(T) == typeof(TargetInfo))
                //{
                //    Log.Error("Saving a TargetInfo " + label + " with Scribe_Values. TargetInfos must be saved with Scribe_TargetInfo.");
                //    return;
                //}
                //if (typeof(Thing).IsAssignableFrom(typeof(T)))
                //{
                //    Log.Error("Using Scribe_Values with a Thing reference " + label + ". Use Scribe_References or Scribe_Deep instead.");
                //    return;
                //}
                //if (typeof(IExposable).IsAssignableFrom(typeof(T)))
                //{
                //    Log.Error("Using Scribe_Values with a IExposable reference " + label + ". Use Scribe_References or Scribe_Deep instead.");
                //    return;
                //}
                if (forceSave || (value == null && defaultValue != null) || (value != null && !value.Equals(defaultValue)))
                {
                    if (value != null)
                        Scribe.WriteElement(label, value.ToString());
                    else
                        Scribe.WriteElement(label, "");
                }
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                value = ScribeExtractor.ValueFromNode<T>(Scribe.curParent[label], defaultValue);
            }
        }

    }
}
