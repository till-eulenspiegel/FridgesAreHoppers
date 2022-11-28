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
        static HarmonyPatches()
        {
            var h = new Harmony("com.fridgesarehoppers.rimworld.mod");
            h.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    public class HopperValidator
    {
        public static bool IsValidHopper(Thing t)
        {
            if (t == null)
            {
                return false;
            }
            return (t.def == ThingDefOf.Hopper || t.def.defName.ToLower().Contains("refrigerator") || t.def.defName.ToLower().Contains("fridge"));
        }

    }


    [HarmonyPatch(typeof(Building_NutrientPasteDispenser), "FindFeedInAnyHopper")]
    public static class Patch_Building_NutrientPasteDispenser_FindFeedInAnyHopper
    {
        static bool Prefix(Building_NutrientPasteDispenser __instance, ref Thing __result)
        {
            foreach (IntVec3 cell in __instance.AdjCellsCardinalInBounds)
            {

                Thing thing = null;
                Thing holder = null;
                foreach (Thing t in cell.GetThingList(__instance.Map))
                {
                    if (Building_NutrientPasteDispenser.IsAcceptableFeedstock(t.def))
                    {
                        thing = t;
                    }

                    if (HopperValidator.IsValidHopper(t))
                    {
                        holder = t;
                    }
                }
                if (thing != null && holder != null)
                {
                    __result = thing;
                    return false;
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(Building_NutrientPasteDispenser), "HasEnoughFeedstockInHoppers")]
    public static class Patch_Building_NutrientPasteDispenser_HasEnoughFeedstockInHoppers
    {
        static bool Prefix(Building_NutrientPasteDispenser __instance, ref bool __result)
        {
            float num = 0f;
            for (int i = 0; i < __instance.AdjCellsCardinalInBounds.Count; i++)
            {
                IntVec3 c = __instance.AdjCellsCardinalInBounds[i];
                Thing acceptableFeedSrc = null;
                Thing hopperSource = null;

                List<Thing> thingList = c.GetThingList(__instance.Map);
                for (int j = 0; j < thingList.Count; j++)
                {
                    Thing curThing = thingList[j];
                    if (Building_NutrientPasteDispenser.IsAcceptableFeedstock(curThing.def))
                    {
                        acceptableFeedSrc = curThing;
                    }
                    if (HopperValidator.IsValidHopper(curThing))
                    {
                        hopperSource = curThing;
                    }
                }
                if (acceptableFeedSrc != null && hopperSource != null)
                {
                    num += (float)acceptableFeedSrc.stackCount * acceptableFeedSrc.GetStatValue(StatDefOf.Nutrition);
                }
                if (num >= __instance.def.building.nutritionCostPerDispense)
                {
                    __result = true;
                    return false;
                }
            }
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Alert_PasteDispenserNeedsHopper), "BadDispensers", MethodType.Getter)]
    public static class Patch_Alert_PasteDispenserNeedsHopper_BadDispensers_Getter
    {
        static bool Prefix(Alert_PasteDispenserNeedsHopper __instance, ref List<Thing> __result)
        {
            __result = (List<Thing>)__instance.GetType().GetField("badDispensersResult", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            __result.Clear();
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                foreach (Thing item in maps[i].listerThings.ThingsInGroup(ThingRequestGroup.FoodDispenser))
                {
                    bool flag = false;
                    ThingDef hopper = ThingDefOf.Hopper;
                    foreach (IntVec3 adjCellsCardinalInBound in ((Building_NutrientPasteDispenser)item).AdjCellsCardinalInBounds)
                    {
                        Thing edifice = adjCellsCardinalInBound.GetEdifice(item.Map);
                        if (HopperValidator.IsValidHopper(edifice))
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (!flag)
                    {
                        __result.Add(item);
                    }
                }
            }
            return false;
        }
    }

}
