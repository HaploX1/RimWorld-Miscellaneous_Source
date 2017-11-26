using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using UnityEngine; // Always needed

namespace Miscellaneous
{
    public class RecipeInjector : ITab
    {
        protected GameObject gameObject;

        public RecipeInjector()
        {
            gameObject = new GameObject("Miscellaneous_Injector");
            gameObject.AddComponent<GameObject_RecipeInjector>();
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
        }

        protected override void FillTab() { }

    }


    public class GameObject_RecipeInjector : MonoBehaviour
    {

        #region MonoBehavior

        public void Start()
        {
            DoRecipeInjection();
            base.enabled = true;
        }

        public void OnLevelWasLoaded() { }

        public void Update() { }

        #endregion




        #region Add Recipes

        private List<RecipeDef> addRecipes;
        private List<string> addRecipeNames = new List<string>()
        {
            "InstallElectronicTacticalImplant",
            "InstallBionicTacticalImplant"
        };
        private string pawnDefNameToAddRecipes = "Human";


        private void DoRecipeInjection()
        {

            if (addRecipes == null || addRecipes.Count == 0)
            {
                addRecipes = new List<RecipeDef>();
                // fill addRecipes (with error catching if not found!)
                for (int i = 0; i < addRecipeNames.Count; i++)
                {
                    RecipeDef rd = DefDatabase<RecipeDef>.GetNamedSilentFail(addRecipeNames[i]);
                    if (rd != null)
                        addRecipes.Add(rd);
                }
            }

            //Log.Error("Adding Recipes: " + addRecipes.Count.ToString());

            ThingDef pawnDef = DefDatabase<ThingDef>.GetNamed(pawnDefNameToAddRecipes, true);
            // Add recipes to pawn defs
            if (pawnDef != null)
            {
                bool updated = false;
                for (int i = 0; i < addRecipes.Count; i++)
                {
                    if (!(pawnDef.recipes.Contains(addRecipes[i])))
                    {
                        pawnDef.recipes.Add(addRecipes[i]);
                        updated = true;
                        //Log.Error("Added Recipe: " + addRecipes[i].defName);
                    }
                }

                RecipeDef recipeDefLastEntry = DefDatabase<RecipeDef>.GetNamedSilentFail("Euthanize");
                if (updated && recipeDefLastEntry != null)
                {
                    pawnDef.recipes.Remove(recipeDefLastEntry);
                    pawnDef.recipes.Add(recipeDefLastEntry);
                }
            }


        }


        #endregion

    }




}
