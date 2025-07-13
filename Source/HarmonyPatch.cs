using System;
using System.Collections.Generic;
using System.Reflection;

using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FridgesAreHoppers
{

    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        public static Dictionary<string, bool> hopperCache;


        static HarmonyPatches()
        {
            hopperCache = new Dictionary<string, bool>();

            var h = new Harmony("com.fridgesarehoppers.rimworld.mod");
            h.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static List<Thing> findFeedsInNearbyHoppers(Building_NutrientPasteDispenser __instance)
        {
            List<Thing> foundFeeds = new List<Thing>();
            for (int i = 0; i < __instance.AdjCellsCardinalInBounds.Count; i++)
            {
                bool foodFound = false;
                bool foundHopper = false;
                IntVec3 c = __instance.AdjCellsCardinalInBounds[i];

                List<Thing> thingsInPosition = c.GetThingList(__instance.Map);
                List<Thing> feedsInPosition = new List<Thing>();
                for (int j = 0; j < thingsInPosition.Count; j++)
                {
                    Thing thingToCheck = thingsInPosition[j];
                    if (Building_NutrientPasteDispenser.IsAcceptableFeedstock(thingToCheck.def))
                    {

                        feedsInPosition.Add(thingToCheck);
                        foodFound= true;
                    }
                    if (thingToCheck.IsHopper())
                    {
                        foundHopper = true;
                    }
                }

                if (foodFound && foundHopper)
                {
                    foundFeeds.AddRange(feedsInPosition);
                }
            }
            return foundFeeds;
        }
    }

    [HarmonyPatch(typeof(StorageGroupUtility), "IsHopper")]
    public static class Patch_StorageGroupUtility_IsHopper
    {

        static void Postfix(Thing thing, ref bool __result)
        {
            if (__result)
            {
                return;
            }


            if (thing == null)
            {
                return;
            }


            if (!HarmonyPatches.hopperCache.ContainsKey(thing.def.defName))
            {
                HarmonyPatches.hopperCache[thing.def.defName] = thing.def.defName.ToLower().Contains("refrigerator") || thing.def.defName.ToLower().Contains("fridge");
            }

            __result = HarmonyPatches.hopperCache[thing.def.defName];
        }

    }

    [HarmonyPatch(typeof(Building_NutrientPasteDispenser), "HasEnoughFeedstockInHoppers")]
    public static class Patch_Building_NutrientPasteDispenser_HasEnoughFeedstockInHoppers
    {
        static bool Prefix(Building_NutrientPasteDispenser __instance, ref bool __result)
        {
            __result = false;
            float nutritionRequired = __instance.def.building.nutritionCostPerDispense - 0.0001f;
            List<Thing> feedSources = HarmonyPatches.findFeedsInNearbyHoppers(__instance);
            float currentFeedValue = 0f;
            for (int i = 0; i < feedSources.Count; i++)
            {
                Thing feedSource = feedSources[i];
                currentFeedValue += (float)feedSource.stackCount * feedSource.GetStatValue(StatDefOf.Nutrition);
                if (currentFeedValue > nutritionRequired)
                {
                    __result = true;
                    return false;
                }

            }
            return false;
        }
    }


    [HarmonyPatch(typeof(Building_NutrientPasteDispenser))]
    [HarmonyPatch("TryDispenseFood", MethodType.Normal)]
    public static class Building_NutrientPasteDispenser_TryDispenseFood
    {
        public static bool Prefix(Building_NutrientPasteDispenser __instance, ref Thing __result)
        {

            if (!__instance.CanDispenseNow)
            {
                __result = null;
                return false;
            }
            float nutritionRequired = __instance.def.building.nutritionCostPerDispense - 0.0001f;
            List<Thing> feedSources = HarmonyPatches.findFeedsInNearbyHoppers(__instance);
            List<ThingDef> ingredientList = new List<ThingDef>();
            for (int i = 0 ; i < feedSources.Count; i++)
            {
                Thing feedSource = feedSources[i];
                if ( feedSource == null )
                {
                    Log.Error("Invalid feed source returned");
                    return true;
                }
                int amountOfThisFeedTypeToFillNeed = Mathf.CeilToInt(nutritionRequired / feedSource.GetStatValue(StatDefOf.Nutrition));
                int nutritionToRemoveFromFeedSource = Mathf.Min(feedSource.stackCount, amountOfThisFeedTypeToFillNeed);

                nutritionRequired -= (float)nutritionToRemoveFromFeedSource * feedSource.GetStatValue(StatDefOf.Nutrition);
                ingredientList.Add(feedSource.def);
                feedSource.SplitOff(nutritionToRemoveFromFeedSource);
                if (nutritionRequired < 0 )
                {
                    break;
                }

            }

            if (nutritionRequired > 0 )
            {
                __result = null;
                Log.Error("Something went wrong, didn't have enough nutrition at the end");
            }

            __instance.def.building.soundDispense.PlayOneShot(new TargetInfo(__instance.Position, __instance.Map));
            Thing createdNutrientPaste = ThingMaker.MakeThing(ThingDefOf.MealNutrientPaste);
            CompIngredients compIngredients = createdNutrientPaste.TryGetComp<CompIngredients>();
            for (int i = 0; i < ingredientList.Count; i++)
            {
                compIngredients.RegisterIngredient(ingredientList[i]);
            }
            __result = createdNutrientPaste;

            return false;
        }
    }
}
