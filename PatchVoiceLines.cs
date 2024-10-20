using HarmonyLib;
using StoryScene;
using System.Reflection;
using UnityEngine;
using WorkParser;

namespace SeraphDLL
{
    public class PatchVoiceLines
    {
        private static readonly Harmony harmony = new Harmony("LoR.Purplelite.PatchVoiceLines");

        public static void Patch()
        {
            MethodInfo methodPlayVoice = typeof(StoryManager).GetMethod("PlayVoice", AccessTools.all);
            harmony.Patch(methodPlayVoice, postfix: new HarmonyMethod(typeof(PatchVoiceLines).GetMethod("StoryManager_PlayVoice_Postfix", AccessTools.all)));
        }

        private static void StoryManager_PlayVoice_Postfix(StoryManager __instance, Dialog d, ref Coroutine ____autoClipCoroutine, DialogLogManager ___dialogLogManager)
        {
            if (StorySerializer.isMod && StorySerializer.curEpisode.episodeName == ModData.WorkshopId)
            {
                __instance.StopCoroutine(____autoClipCoroutine);

                if (d.Voice != null && ModData.Sounds.TryGetValue(d.Voice, out AudioClip voiceFile))
                {
                    __instance.voice.clip = voiceFile;
                    __instance.voice.Play();
                    ____autoClipCoroutine = __instance.StartCoroutine("AudioClipOver", __instance.voice.clip.length);
                    ___dialogLogManager.OnVoice(d);
                    return;
                }
            }
        }
    }
}
