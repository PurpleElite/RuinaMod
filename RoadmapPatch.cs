using CustomInvitation;
using HarmonyLib;
using Mod;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UI;
using UnityEngine;
using Workshop;

namespace CustomDLLs
{
    public static class RoadmapPatch
    {
        private static readonly Harmony harmony = new Harmony("LoR.Purplelite.RoadmapPatch");
        private static Dictionary<List<StageClassInfo>, UIStoryProgressIconSlot> _storyslots;
        private static bool _centerPanel_Init = false;
        private static bool _battleStoryPanel_Init = false;

        public static void Patch()
		{
			MethodInfo methodSetStoryLine = typeof(UIStoryProgressPanel).GetMethod("SetStoryLine", AccessTools.all);
			harmony.Patch(methodSetStoryLine, postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => UIStoryProgressPanel_SetStoryLine_Postfix(null))));
		}

        private static void UIStoryProgressPanel_SetStoryLine_Postfix(UIStoryProgressPanel __instance)
        {
            if (__instance.gameObject.transform.parent.gameObject.name == "[Rect]CenterPanel" && !_centerPanel_Init)
            {
                if (_storyslots == null)
                    _storyslots = new Dictionary<List<StageClassInfo>, UIStoryProgressIconSlot>();
                CreateStoryLine(__instance, UIStoryLine.TheThumb, new Vector3(-400f, 0f));
                _centerPanel_Init = true;
            }
            if (__instance.gameObject.transform.parent.gameObject.name == "BattleStoryPanel" && !_battleStoryPanel_Init)
            {
                if (_storyslots == null)
                    _storyslots = new Dictionary<List<StageClassInfo>, UIStoryProgressIconSlot>();
                CreateStoryLine(__instance, UIStoryLine.TheThumb, new Vector3(-400f, 0f));
                _battleStoryPanel_Init = true;
            }
            foreach (List<StageClassInfo> key in _storyslots.Keys)
            {
                _storyslots[key].SetSlotData(key);
                if (key[0].currentState != StoryState.Close)
                    _storyslots[key].SetActiveStory(true);
                else
                    _storyslots[key].SetActiveStory(false);
            }
        }

        public static void CreateStoryLine(UIStoryProgressPanel __instance, UIStoryLine reference, Vector3 vector)
        {
            StageClassInfo data = Singleton<StageClassInfoList>.Instance.GetData(new LorId(ModData.WorkshopId, 1));
            UIStoryProgressIconSlot progressIconSlot = ((List<UIStoryProgressIconSlot>)typeof(UIStoryProgressPanel).GetField("iconList", AccessTools.all).GetValue(__instance)).Find(x => x.currentStory == reference);
            UIStoryProgressIconSlot newslot = UnityEngine.Object.Instantiate(progressIconSlot, progressIconSlot.transform.parent);
            newslot.currentStory = UIStoryLine.Rats;
            newslot.Initialized(__instance);
            newslot.transform.localPosition += vector;
            typeof(UIStoryProgressIconSlot).GetField("connectLineList", AccessTools.all).SetValue(newslot, new List<GameObject>());
            _storyslots[new List<StageClassInfo>() { data }] = newslot;
        }
    }
}
