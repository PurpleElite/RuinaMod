using HarmonyLib;
using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UI;

namespace SeraphDLL
{
    public class PatchAppearanceProjection
    {
        private static readonly Harmony harmony = new Harmony("LoR.Purplelite.PatchAppearanceProjection");
        private static readonly int[] IDs = { 16167788, 16167789, 16167790 };
        public static void Patch()
        {
            

            MethodInfo methodInit = typeof(CustomCoreBookInventoryModel).GetMethod("Init", AccessTools.all);
            harmony.Patch(methodInit, postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => CustomCoreBookInventoryModel_Init_Postfix())));

            MethodInfo methodGetData = typeof(BookXmlList).GetMethod("GetData", AccessTools.all, Type.DefaultBinder, new[] { typeof(LorId), typeof(bool) }, null);
            harmony.Patch(methodGetData, prefix: new HarmonyMethod(typeof(PatchAppearanceProjection).GetMethod("BookXmlInfo_GetData_Prefix", AccessTools.all)));
        }

        private static void CustomCoreBookInventoryModel_Init_Postfix()
        {
            foreach (var id in IDs)
            {
                Singleton<CustomCoreBookInventoryModel>.Instance.AddBook(id);
            }
        }

        private static void BookXmlInfo_GetData_Prefix(ref LorId id, bool errNull)
        {
            if (!id.IsWorkshop() && IDs.Contains(id.id))
            {
                id = new LorId(ModData.WorkshopId, id.id);
            }
        }
    }
}
