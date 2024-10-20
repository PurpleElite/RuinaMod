using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UI;
using UnityEngine;

namespace SeraphDLL
{
    public static class PatchAchievements
    {
        private static readonly Harmony harmony = new Harmony("LoR.Purplelite.PatchAchievements");

        public static void Patch()
		{
			MethodInfo methodUnlockAchievement = typeof(PlatformManager).GetMethod("UnlockAchievement", AccessTools.all);
			harmony.Patch(methodUnlockAchievement, prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => PlatformManager_UnlockAchievement_Prefix())), postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => PlatformManager_UnlockAchievement_Postfix())));
        }

        private static void PlatformManager_UnlockAchievement_Prefix()
        {
            GlobalGameManager.WithMods = false;
        }

        private static void PlatformManager_UnlockAchievement_Postfix()
        {
            GlobalGameManager.WithMods = true;
        }
    }
}
