using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blueprint2MapGenConverter
{
    public class Scribe_Deep
    {

        public static void LookDeep<T>(ref T target, string label, params object[] ctorArgs)
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {

                IExposable exposable = target as IExposable;
                if (target != null && exposable == null)
                {
                    //Log.Error(string.Concat(new object[]
                    //{
                    //"Cannot use LookDeep to save non-IExposable non-null ",
                    //label,
                    //" of type ",
                    //typeof(T)
                    //}));
                    return;
                }
                if (target == null)
                {
                    Scribe.EnterNode(label);
                    Scribe.WriteAttribute("IsNull", "True");
                    Scribe.ExitNode();
                }
                else
                {
                    Scribe.EnterNode(label);
                    if (target.GetType() != typeof(T))
                    {
                        Scribe.WriteAttribute("Class", target.GetType().ToString());
                    }
                    exposable.ExposeData();
                    Scribe.ExitNode();
                }
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                target = ScribeExtractor.SaveableFromNode<T>(Scribe.curParent[label], ctorArgs);
            }
        }
    }
}
