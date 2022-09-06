using HarmonyLib;
using StoryScene;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using WorkParser;

namespace SeraphDLL
{
    public class PatchStorySprites
    {
        private static readonly Harmony harmony = new Harmony("LoR.Purplelite.PatchStorySprites");
        private static Dictionary<string, Sprite[]> characterFaces = new Dictionary<string, Sprite[]>();
        private static FieldInfo faceRenderListField = typeof(StoryCharacter).GetField("faceRenderList", AccessTools.all);
        public static void Patch()
        {
            GetCharacterFaces();
            MethodInfo methodLoadCharacterPrefab = typeof(StoryManager).GetMethod("LoadCharacterPrefab", AccessTools.all);
            harmony.Patch(methodLoadCharacterPrefab, postfix: new HarmonyMethod(typeof(PatchStorySprites).GetMethod("StoryManager_LoadCharacterPrefab_Postfix", AccessTools.all)));
            MethodInfo methodUpdateList = typeof(DialogLogManager).GetMethod("UpdateList", AccessTools.all);
            harmony.Patch(methodUpdateList, postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => DialogLogManager_UpdateList_Postfix(null))));
        }

        private static void GetCharacterFaces()
        {
            var characters = ModData.Sprites.Where(x => x.Key.Contains("StoryStanding_") && !x.Key.Contains("Faces_"));
            foreach (var character in characters)
            {
                var name = character.Key.Substring(character.Key.LastIndexOf('_') + 1);
                var faces = ModData.Sprites.Where(x => x.Key.Contains("Faces_" + name));
                foreach (var face in faces)
                {
                    face.Value.name = "face_" + face.Key.Substring(face.Key.LastIndexOf(name) + name.Length);
                }
                if (faces.Count() > 0)
                {
                    characterFaces[name] = faces.Select(x => x.Value).ToArray();
                }
            }
        }

        private static void StoryManager_LoadCharacterPrefab_Postfix(ref StoryCharacter __result, string name)
        {
            if (StorySerializer.isMod && characterFaces.TryGetValue(name, out Sprite[] faces))
            {
                var faceRenderList = (List <SpriteRenderer>)faceRenderListField.GetValue(__result);
                foreach (var face in faces)
                {
                    var parentRenderer = __result.GetCurBody();
                    if (parentRenderer == null)
                    {
                        __result.SetBody("nomal");
                        parentRenderer = __result.GetCurBody();
                    }
                    var newRenderer = Object.Instantiate(parentRenderer, parentRenderer.transform);
                    newRenderer.name = face.name;
                    newRenderer.sprite = face;
                    switch (name)
                    {
                        case "Johanna":
                            newRenderer.transform.localPosition = new Vector3(-0.52f, 7.08f);
                            break;
                        case "Linus": 
                            newRenderer.transform.localPosition = new Vector3(-0.51f, 6.2f);
                            break;
                        case "Sheire":
                            newRenderer.transform.localPosition = new Vector3(-.11f, 5.99f);
                            break;
                    }
                    
                    faceRenderList.Add(newRenderer);
                }
            }
        }

        private static void DialogLogManager_UpdateList_Postfix(DialogLogManager __instance)
        {
            if (StorySerializer.isMod)
            {
                var slotList = (List<CharacterDialogLog>)typeof(DialogLogManager).GetField("slotList", AccessTools.all).GetValue(__instance);
                var fieldPortraitImage = typeof(CharacterDialogLog).GetField("PortraitImage", AccessTools.all);
                var fieldDialog = typeof(CharacterDialogLog).GetField("dialog", AccessTools.all);
                foreach (var slot in slotList)
                {
                    var dialog = (Dialog)fieldDialog.GetValue(slot);
                    if (dialog != null && ModData.Sprites.TryGetValue("StoryStanding_CharacterPortraits_" + dialog.Teller, out var portrait))
                    {
                        var portraitImage = (Image)fieldPortraitImage.GetValue(slot);
                        portraitImage.enabled = true;
                        portraitImage.sprite = portrait;
                    }
                }
            }
        }
    }
}
