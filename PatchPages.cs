using HarmonyLib;
using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SeraphDLL
{
    public class PatchPages
    {
        private static readonly Harmony harmony = new Harmony("LoR.Purplelite.PatchPages");
        public static void Patch()
        {
            MethodInfo methodSetXmlInfo = typeof(BookModel).GetMethod("SetXmlInfo", AccessTools.all);
            harmony.Patch(methodSetXmlInfo, postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => BookModel_SetXmlInfo_Postfix(null))));
        }
        private static void BookModel_SetXmlInfo_Postfix(BookModel __instance)
        {
            if (__instance.ClassInfo.id.packageId == ModData.WorkshopId)
            {
                List<DiceCardXmlInfo> onlyCards = __instance.GetOnlyCards();
                onlyCards.Clear();
                onlyCards.AddRange(from x in __instance.ClassInfo.EquipEffect.OnlyCard
                                   select ItemXmlDataList.instance.GetCardItem(new LorId(ModData.WorkshopId, x), false) into x
                                   where x != null
                                   select x);
            }
        }
    }
}
