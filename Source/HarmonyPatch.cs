using System;
using System.Collections.Generic;
using System.Reflection;

using HarmonyLib;
using RimWorld;
using Verse;

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


            try
            {
                h.Unpatch(AccessTools.Method(typeof(Building_NutrientPasteDispenser), "HasEnoughFeedstockInHoppers"), HarmonyPatchType.Prefix);
            }
            catch { }

            try
            {
                h.Unpatch(AccessTools.Method(typeof(Building_NutrientPasteDispenser), "FindFeedInAnyHopper"), HarmonyPatchType.Prefix);
            }
            catch { }

            try
            {
                h.Unpatch(AccessTools.Method(typeof(Alert_PasteDispenserNeedsHopper), "BadDispensers"), HarmonyPatchType.Prefix);
            }
            catch { }

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
}
