using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

//using CommonMisc;

namespace AIPawn
{
    /// <summary>
    /// This is the pawn generator class for MAI.
    /// Here are all the settings of the new pawn created.
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>For usage of this code, please look at the license information.</permission>
    public static class AIPawnGenerator
    {
        // xml informations
        private static int passionLevel = 0;
        private static int startingSkillLevel = 6;
        private static bool enhancedAI = false;

        private static string AIPawn_BaseTraitDefName = "AIPawn_BaseTrait";
        private static int AIPawn_BaseTraitDegree = 1;
        private static string AIPawn_ApparelDefName = "AIPawn_Apparel_BaseShielding";

        private static void GetXMLData(AIPawn pawn)
        {
            ThingDef_AIPawn def2 = (ThingDef_AIPawn)pawn.def;
            passionLevel = def2.passionLevel;
            startingSkillLevel = def2.startingSkillLevel;
            enhancedAI = def2.enhancedAI;
        }

        public static AIPawn GenerateAIPawn(string kindDefName, Faction faction, Map map, Gender gender = Gender.Female)
        {
            //return GeneratePawn(PawnKindDef.Named(kindDefName), faction, map, gender);
            PawnGenerationRequest request = new PawnGenerationRequest(PawnKindDef.Named(kindDefName), faction, PawnGenerationContext.NonPlayer, -1, true, true, false, false, false, false, 0f, false, false, true, false, false, false, false, false, 0, null, 0,null,null,null,null,null,0f, 0f, gender, 0.1f, null, null, null);
            return GenerateAIPawn(ref request, map);

        }
        //public static AIPawn GeneratePawn(PawnKindDef kindDef, Faction faction, Map map, Gender gender = Gender.Female, int tries = 0)
        public static AIPawn GenerateAIPawn(ref PawnGenerationRequest request, Map map, int tries = 0)
        {
            BackstoryHelper.AddNewBackstoriesToDatabase(); // First add the new backstories to the database

            //Log.Error("0");

            AIPawn pawnAI = (AIPawn)ThingMaker.MakeThing(request.KindDef.race, null);

            //Log.Error("1");
            GetXMLData(pawnAI);

            //request.EnsureNonNullFaction();

            pawnAI.kindDef = request.KindDef;
            pawnAI.SetFactionDirect(request.Faction);

            PawnComponentsUtility.CreateInitialComponents(pawnAI);

            //Log.Error("2");

            // Needs to be set because of not flesh user
            pawnAI.relations = new Pawn_RelationsTracker(pawnAI);
            pawnAI.guest = new Pawn_GuestTracker(pawnAI);

            if (request.FixedGender.HasValue)
            {
                pawnAI.gender = request.FixedGender.Value;
            }
            else if (pawnAI.RaceProps.hasGenders)
            {
                if (Rand.Value < 0.5f)
                {
                    pawnAI.gender = Gender.Male;
                }
                else
                {
                    pawnAI.gender = Gender.Female;
                }
            }
            else
            {
                pawnAI.gender = Gender.Female;
            }

            SetBirthday(pawnAI);
            pawnAI.needs.SetInitialLevels();


            //Log.Error("3");

            AIPawnGenerator.GenerateInitialHediffs(pawnAI);


            if (pawnAI.RaceProps.Humanlike)
            {
                pawnAI.story.melanin = 0.1f;
                pawnAI.story.crownType = CrownType.Average;
                pawnAI.story.hairColor = PawnHairColors.RandomHairColor(pawnAI.story.SkinColor, pawnAI.ageTracker.AgeBiologicalYears);

                pawnAI.story.childhood = GetBackstory();
                //pawnAI.story.adulthood = GetBackstory();

                string headGraphicPath = GraphicDatabaseHeadRecords.GetHeadRandom(pawnAI.gender, pawnAI.story.SkinColor, pawnAI.story.crownType).GraphicPath;
                // With this Reflection you can access a private variable! Here: The private string "headGraphicPath" is set 
                System.Reflection.FieldInfo fi = typeof(Pawn_StoryTracker).GetField("headGraphicPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                fi.SetValue(pawnAI.story, headGraphicPath);

                pawnAI.story.hairDef = GetHair();

                pawnAI.story.bodyType = ((pawnAI.gender != Gender.Female) ? BodyTypeDefOf.Male : BodyTypeDefOf.Female);

                MakeSkillsFromBackstory(pawnAI);
                GiveTraitsTo(pawnAI);

                if (pawnAI.workSettings != null && (request.Faction == Faction.OfPlayer))
                    pawnAI.workSettings.EnableAndInitialize();
            }

            if (pawnAI.RaceProps.ToolUser)
                GenerateBaseApparel(pawnAI);

            pawnAI.Name = GetName(pawnAI.def, map);

            //Log.Error("4");
            //PawnGenerationRequest request = new PawnGenerationRequest(pawnAI.kindDef , Faction.OfPlayer, PawnGenerationContext.All, true, true, false,false, false, false, 0, false, false, false,null, 0,0, pawnAI.gender, null, null);

            //PawnInventoryGenerator.GenerateInventoryFor(pawnAI, request);

            if (!pawnAI.Dead)
                return pawnAI;

            if (tries < 10)
                return GenerateAIPawn(ref request, map, tries + 1);

            return null;
        }

        private static HairDef GetHair()
        {
            return DefDatabase<HairDef>.GetNamed("Shaved");
        }

        private static Backstory GetBackstory()
        {
            string researchProject;

            WorkTags workDisables = BackstoryHelper.GetBasicWorkDisables(); // WorkTags.Artistic <= These are never accessable for mai

            researchProject = "AIPawnAdvResearchAnimals";
            if ((DefDatabase<ResearchProjectDef>.GetNamedSilentFail(researchProject) != null) && (!ResearchProjectDef.Named(researchProject).IsFinished))
                workDisables = workDisables | WorkTags.Animals;

            researchProject = "AIPawnAdvResearchSocial";
            if ((DefDatabase<ResearchProjectDef>.GetNamedSilentFail(researchProject) != null) && (!ResearchProjectDef.Named(researchProject).IsFinished))
                workDisables = workDisables | WorkTags.Social;

            if (!enhancedAI) // do not if enhanced AI
            {
                researchProject = "AIPawnAdvResearchCombat";
                if ((DefDatabase<ResearchProjectDef>.GetNamedSilentFail(researchProject) != null) && (!ResearchProjectDef.Named(researchProject).IsFinished))
                    workDisables = workDisables | WorkTags.Violent;
            }

            researchProject = "AIPawnAdvResearchCrafting";
            if ((DefDatabase<ResearchProjectDef>.GetNamedSilentFail(researchProject) != null) && (!ResearchProjectDef.Named(researchProject).IsFinished))
                workDisables = workDisables | WorkTags.Crafting;

            if (!enhancedAI) // do not if enhanced AI
            {
                researchProject = "AIPawnAdvResearchIntellectual";
                if ((DefDatabase<ResearchProjectDef>.GetNamedSilentFail(researchProject) != null) && (!ResearchProjectDef.Named(researchProject).IsFinished))
                    workDisables = workDisables | WorkTags.Intellectual;
            }

            researchProject = "AIPawnAdvResearchCooking";
            if ((DefDatabase<ResearchProjectDef>.GetNamedSilentFail(researchProject) != null) && (!ResearchProjectDef.Named(researchProject).IsFinished))
                workDisables = workDisables | WorkTags.Cooking;


            researchProject = "AIPawnAdvResearchCaring";
            if ((DefDatabase<ResearchProjectDef>.GetNamedSilentFail(researchProject) != null) && (!ResearchProjectDef.Named(researchProject).IsFinished))
                workDisables = workDisables | WorkTags.Caring;


            researchProject = "AIPawnAdvResearchConstruction";
            if ((DefDatabase<ResearchProjectDef>.GetNamedSilentFail(researchProject) != null) && (!ResearchProjectDef.Named(researchProject).IsFinished))
                workDisables = workDisables | WorkTags.ManualSkilled;


            researchProject = "AIPawnAdvResearchMining";
            if ((DefDatabase<ResearchProjectDef>.GetNamedSilentFail(researchProject) != null) && (!ResearchProjectDef.Named(researchProject).IsFinished))
                workDisables = workDisables | WorkTags.Mining;

            string backstoryUniqueKey = BackstoryHelper.GetBackstoryUniqueKey(workDisables);

            Backstory bs;
            if (BackstoryDatabase.TryGetWithIdentifier(backstoryUniqueKey, out bs))
                return bs;
            return null;

        }

        private static Name GetName(ThingDef pawnDef, Map map)
        {
            string AIs = "";
            IEnumerable<Pawn> pawns = null;

            // Added to catch error when Common Core isn't loaded..
            try
            {
                pawns = map.mapPawns.FreeColonists;
                //pawns = Radar.FindAllPawns(map);
            }
            catch { }

            if (pawns != null)
            {
                int c = pawns.Where(p => p.def.defName == pawnDef.defName).Count();

                if (c > 0)
                    AIs = " " + (c + 1).ToString();
            }

            string first = "AIPawn_Basename_first".Translate(); // "Mobile Artificial Intelligence"
            string nick = "AIPawn_Basename_nick".Translate() + AIs; // "Mai" + AIs
            string last = "AIPawn_Basename_last".Translate(); // " "

            if (enhancedAI)
                nick = nick.ToUpper();

            NameTriple pawnName = new NameTriple(first, nick, last);
            
            return pawnName;
        }

        private static void SetBirthday(Pawn pawn)
        {
            // Need to be > 14 to activate the possibility for romance
            int age = 18;

            pawn.ageTracker.AgeBiologicalTicks = (long)(age * 3600000f);
            pawn.ageTracker.BirthAbsTicks = (long)GenTicks.TicksAbs - pawn.ageTracker.AgeBiologicalTicks;
        }


        public static void MakeSkillsFromBackstory(AIPawn pawn)
        {
            IEnumerator<SkillDef> enumerator = DefDatabase<SkillDef>.AllDefs.GetEnumerator();
            try
            {
                
                while (enumerator.MoveNext())
                {
                    SkillDef current = enumerator.Current;
                    int num = FinalLevelOfSkill(current);
                    SkillRecord skill = pawn.skills.GetSkill(current);
                    skill.levelInt = num;
                    if (skill.TotallyDisabled)
                    {
                        continue;
                    }

                    skill.xpSinceLastLevel = 0;

                    switch (passionLevel)
                    {
                        case 1:
                            skill.passion = Passion.Minor;
                            break;
                        case 2:
                            skill.passion = Passion.Major;
                            break;
                        default:
                            skill.passion = Passion.None;
                            break;
                    }

                }
            }
            finally
            {
                enumerator.Dispose();
            }
        }
        private static int FinalLevelOfSkill(SkillDef sDef)
        {
            return startingSkillLevel;//6; //3;
        }


        private static void GiveTraitsTo(Pawn pawn)
        {
            if (pawn.story == null)
            {
                return;
            }

            // Remove all other traits
            pawn.story.traits.allTraits.Clear();

            TraitDef traitDef = DefDatabase<TraitDef>.AllDefsListForReading.Find((TraitDef tr) => tr.defName == AIPawn_BaseTraitDefName);
            if (traitDef != null && !pawn.story.traits.HasTrait(traitDef))
            {
                Trait trait = new Trait(traitDef, AIPawn_BaseTraitDegree);
                pawn.story.traits.GainTrait(trait);
            }

            return;
        }

        private static void GenerateInitialHediffs(Pawn pawn)
        {
            int num = 0;
            while (true)
            {
                pawn.health.hediffSet.Clear();
                PawnTechHediffsGenerator.GenerateTechHediffsFor(pawn);
                if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving))
                    break;

                pawn.health.Reset();
                num++;
                if (num > 80)
                {
                    Log.Error(string.Concat("Could not generate old age injuries that allow pawn to move: ", pawn));
                    return;
                }
                if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving))
                    break;
            }
        }


        private static void GenerateBaseApparel(Pawn pawn)
        {
            try
            {
                if (pawn.apparel != null)
                    pawn.apparel.DestroyAll(DestroyMode.Vanish);
                else
                    pawn.apparel = new Pawn_ApparelTracker(pawn);

                ThingDef item = DefDatabase<ThingDef>.GetNamed(AIPawn_ApparelDefName);
                Apparel apparel = (Apparel)ThingMaker.MakeThing(item);
                if (ApparelUtility.HasPartsToWear(pawn, apparel.def))
                {
                    pawn.apparel.Wear(apparel, false);
                }
            }
            catch (Exception ex)
            {
                Log.Error("AIPawnGenerator: Caught an unexpected error: \n" + ex.Message);
            }
        }

        public static void GiveBaseApparelWhileInBed(Pawn pawn, bool inRechargeStation)
        {
            if (!pawn.Spawned)
                return;


            try
            {

                // Check if available, try to repair
                if (inRechargeStation)
                {
                    foreach (Apparel a in pawn.apparel.WornApparel)
                    {
                        if (a.def.defName == AIPawn_ApparelDefName && a.HitPoints < a.MaxHitPoints * 0.95)
                            a.HitPoints += 1;
                    }
                }

                if (pawn.apparel.WornApparelCount != 0 || !inRechargeStation)
                    return;


                // Inventar empty + in Bed -> rebuild shielding
                ThingDef item = DefDatabase<ThingDef>.GetNamed(AIPawn_ApparelDefName);
                Apparel apparel = (Apparel)ThingMaker.MakeThing(item);
                apparel.HitPoints = (int)(apparel.MaxHitPoints * 0.05);
                if (ApparelUtility.HasPartsToWear(pawn, apparel.def))
                {
                    pawn.apparel.Wear(apparel, false);
                }

            }
            catch (Exception ex)
            {
                Log.Error("AIPawnGenerator: Caught an unexpected error: \n" + ex.Message);
            }
        }
        public static void DestroyBaseShielding(Pawn pawn)
        {
            try
            {
                foreach (Apparel a in pawn.apparel.WornApparel)
                {
                    if (a.def.defName == AIPawn_ApparelDefName)
                    {
                        pawn.apparel.Remove(a);
                        a.Destroy();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("AIPawnGenerator: Caught an unexpected error: \n" + ex.Message);
            }
        }

    }
}
