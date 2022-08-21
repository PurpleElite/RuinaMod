using HarmonyLib;
using StoryScene;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

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
                        case "Linus": 
                            newRenderer.transform.localPosition = new Vector3(-0.51f, 6.2f);
                            break;
                    }
                    
                    faceRenderList.Add(newRenderer);
                }
            }
        }
    }
}
