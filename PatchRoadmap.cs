using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UI;
using UnityEngine;
using static UI.UIIconManager;

namespace SeraphDLL
{
    public static class PatchRoadmap
    {
        private static readonly Harmony harmony = new Harmony("LoR.Purplelite.PatchRoadmap");
        private static Dictionary<List<StageClassInfo>, UIStoryProgressIconSlot> _storySlots;
        private static bool _centerPanel_Init = false;
        private static bool _battleStoryPanel_Init = false;

        public static void Patch()
		{
			MethodInfo methodSetStoryLine = typeof(UIStoryProgressPanel).GetMethod("SetStoryLine", AccessTools.all);
			harmony.Patch(methodSetStoryLine, postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => UIStoryProgressPanel_SetStoryLine_Postfix(null))));
            MethodInfo methodSetStoryIconDictionary = typeof(UISpriteDataManager).GetMethod("SetStoryIconDictionary", AccessTools.all);
            harmony.Patch(methodSetStoryIconDictionary, postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => UISpriteDataManager_SetStoryIconDictionary_Postfix(null))));
        }

        private static void UIStoryProgressPanel_SetStoryLine_Postfix(UIStoryProgressPanel __instance)
        {
            if (__instance.gameObject.transform.parent.gameObject.name == "[Rect]CenterPanel" && !_centerPanel_Init)
            {
                if (_storySlots == null)
                    _storySlots = new Dictionary<List<StageClassInfo>, UIStoryProgressIconSlot>();
                CreateStoryLine(__instance);
                _centerPanel_Init = true;
            }
            if (__instance.gameObject.transform.parent.gameObject.name == "BattleStoryPanel" && !_battleStoryPanel_Init)
            {
                if (_storySlots == null)
                    _storySlots = new Dictionary<List<StageClassInfo>, UIStoryProgressIconSlot>();
                CreateStoryLine(__instance);
                _battleStoryPanel_Init = true;
            }
            foreach (List<StageClassInfo> key in _storySlots.Keys)
            {
                _storySlots[key].SetSlotData(key);
                if (key[0].currentState != StoryState.Close)
                    _storySlots[key].SetActiveStory(true);
                else
                    _storySlots[key].SetActiveStory(false);
            }
        }

        public static void CreateStoryLine(UIStoryProgressPanel __instance)
        {
            var previousStory = UIStoryLine.TheThumb;
            var offset = new Vector3(-400f, 0f);
            var previousStoryIconSlot = ((List<UIStoryProgressIconSlot>)typeof(UIStoryProgressPanel).GetField("iconList", AccessTools.all).GetValue(__instance)).Find(x => x.currentStory == previousStory);

            var data = Singleton<StageClassInfoList>.Instance.GetData(new LorId(ModData.WorkshopId, 1));
            //Get rid of the workshop id automatically added to the book LorIds, we want to use a vanilla book here
            var needsBooksCopy = data.invitationInfo.needsBooks.ToArray();
            data.invitationInfo.needsBooks.Clear();
            data.invitationInfo.needsBooks.AddRange(needsBooksCopy.Select(x => new LorId(x.id)));

            var newIconSlot = Object.Instantiate(previousStoryIconSlot, previousStoryIconSlot.transform.parent);
            //newIconSlot.currentStory = UIStoryLine.Rats;
            newIconSlot.Initialized(__instance);
            newIconSlot.transform.localPosition += offset;

            var connectLineListField = typeof(UIStoryProgressIconSlot).GetField("connectLineList", AccessTools.all);
            var previousStoryIconConnectLine = ((List<GameObject>)connectLineListField.GetValue(newIconSlot)).FirstOrDefault();
            var newConnectLine = Object.Instantiate(previousStoryIconConnectLine, previousStoryIconConnectLine.transform.parent);
            newConnectLine.transform.localScale = new Vector3(1.9f, 1f);
            newConnectLine.transform.localPosition += offset/2 + new Vector3(0, 150);
            newConnectLine.transform.localRotation = new Quaternion(90f, 0f, 0f, 0f);
            connectLineListField.SetValue(newIconSlot, new List<GameObject> { newConnectLine });

            _storySlots[new List<StageClassInfo>() { data }] = newIconSlot;
        }

        private static void UISpriteDataManager_SetStoryIconDictionary_Postfix(UISpriteDataManager __instance)
        {
            var storyIconDic = (Dictionary<string, IconSet>)typeof(UISpriteDataManager).GetField("StoryIconDic", AccessTools.all).GetValue(__instance);
            if (!storyIconDic.ContainsKey(ModData.WorkshopId))
            {
                var iconSet = new IconSet
                {
                    type = ModData.WorkshopId,
                    icon = ModData.Sprites["Sprites_StoryIcon"],
                    color = storyIconDic.Values.First().color,
                    iconGlow = ModData.Sprites["Sprites_StoryIconGlow"],
                    colorGlow = storyIconDic.Values.First().colorGlow
                };
                storyIconDic.Add(iconSet.type, iconSet);
            }
        }
    }
}
