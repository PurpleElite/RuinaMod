using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SeraphDLL
{
    public class PatchBookThumbnail
    {
        private static readonly Harmony harmony = new Harmony("LoR.Purplelite.PatchBookThumbnail");
        public static void Patch()
        {
            MethodInfo methodGetThumbSpriteModel = typeof(BookModel).GetMethod("GetThumbSprite", AccessTools.all);
            harmony.Patch(methodGetThumbSpriteModel, postfix: new HarmonyMethod(typeof(PatchBookThumbnail).GetMethod("BookModel_GetThumbSprite_Postfix", AccessTools.all)));
            MethodInfo methodGetThumbSpriteXml = typeof(BookXmlInfo).GetMethod("GetThumbSprite", AccessTools.all);
            harmony.Patch(methodGetThumbSpriteXml, postfix: new HarmonyMethod(typeof(PatchBookThumbnail).GetMethod("BookXmlInfo_GetThumbSprite_Postfix", AccessTools.all)));
        }
        private static void BookModel_GetThumbSprite_Postfix(ref Sprite __result, BookModel __instance)
        {
            if (__instance.BookId.packageId == ModData.WorkshopId)
            {
                __result = ModData.Sprites["CharacterSkin_" + __instance.ClassInfo.GetCharacterSkin() + "_Icon"];
            }
        }

        private static void BookXmlInfo_GetThumbSprite_Postfix(ref Sprite __result, BookXmlInfo __instance)
        {
            if (__instance.id.packageId == ModData.WorkshopId)
            {
                __result = ModData.Sprites["CharacterSkin_" + __instance.GetCharacterSkin() + "_Icon"];
            }
        }
    }
}
