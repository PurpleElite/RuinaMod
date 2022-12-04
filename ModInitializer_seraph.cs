using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace SeraphDLL
{
    public static class ModData
    {
        internal const string WorkshopId = "SeraphRunaways";
        internal static string Language = "en";
        internal static DirectoryInfo ModPath { get; set; }
        static public Dictionary<string, Sprite> Sprites { get; } = new Dictionary<string, Sprite>();
        static public Dictionary<string, AudioClip> Sounds { get; } = new Dictionary<string, AudioClip>();
    }

    public class ModInitializer_seraph : ModInitializer
    {
        public static char Sep => Path.DirectorySeparatorChar;
        public override void OnInitializeMod()
        {
            var assemblyPath = new DirectoryInfo(Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path)));
            ModData.ModPath = assemblyPath.Parent;
            GetResources(new DirectoryInfo((ModData.ModPath?.ToString()) + Sep + "Resource"));
            InitStageClassInfo();
            AddAppearanceProjections();
            PatchRoadmap.Patch();
            PatchBookThumbnail.Patch();
            PatchPages.Patch();
            PatchStorySprites.Patch();
            PatchVoiceLines.Patch();
        }

        private static void GetResources(DirectoryInfo parentDir)
        {
            if (parentDir.GetDirectories().Length != 0)
            {
                DirectoryInfo[] childDirs = parentDir.GetDirectories();
                for (int i = 0; i < childDirs.Length; i++)
                {
                    GetResources(childDirs[i]);
                }
            }

            var audioDownloads = new Queue<(DownloadHandlerAudioClip Download, string Name)>();

            foreach (FileInfo fileInfo in parentDir.GetFiles())
            {
                //Sprites
                if (fileInfo.Extension == ".png")
                {
                    Texture2D texture2D = new Texture2D(2, 2);
                    texture2D.LoadImage(File.ReadAllBytes(fileInfo.FullName));
                    Sprite value = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0f, 0f));
                    value.texture.wrapMode = TextureWrapMode.Clamp;
                    ModData.Sprites[GetImageName(fileInfo)] = value;
                }
                else
                {
                    //Sounds
                    AudioType audioType = AudioType.OGGVORBIS;
                    if (fileInfo.Extension == ".wav")
                    {
                        audioType = AudioType.WAV;
                    }
                    if (File.Exists(fileInfo.FullName))
                    {
                        var webRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + fileInfo.FullName, audioType);
                        webRequest.SendWebRequest();
                        if (webRequest.isNetworkError)
                        {
                            Debug.Log(webRequest.error);
                        }
                        else
                        {
                            audioDownloads.Enqueue((webRequest.downloadHandler as DownloadHandlerAudioClip, fileInfo.Name));
                        }
                    }
                }
            }

            foreach ((var download, var name) in audioDownloads)
            {
                while (!download.isDone)
                {
                    Thread.Sleep(1);
                }
                AudioClip audioClip = download.audioClip;
                audioClip.name = name;
                ModData.Sounds[name] = audioClip;
            }

            string GetImageName(FileInfo fileInfo)
            {
                var dirArray = fileInfo.FullName.Split(Sep);
                var rootIndex = Array.IndexOf(dirArray, "Resource");
                var subDirArray = dirArray.Skip(rootIndex + 1);
                if (subDirArray.Count() > 2)
                {
                    subDirArray = subDirArray.Take(2).Concat(new[]{ subDirArray.Last()});
                }
                var name = string.Join("_", subDirArray);
                return Path.GetFileNameWithoutExtension(name);
            }
        }

        //private static void AddEffectText()
        //{
        //    Dictionary<string, BattleEffectText> dictionary =
        //        typeof(BattleEffectTextsXmlList).GetField("_dictionary", BindingFlags.NonPublic | BindingFlags.Instance)
        //        .GetValue(Singleton<BattleEffectTextsXmlList>.Instance) as Dictionary<string, BattleEffectText>;
        //    FileInfo[] files = new DirectoryInfo((ModData.ModPath?.ToString()) + Sep + "Data" + Sep + "Localize" + Sep + ModData.Language + Sep + "EffectTexts").GetFiles();
        //    for (int i = 0; i < files.Length; i++)
        //    {
        //        using (StringReader stringReader = new StringReader(File.ReadAllText(files[i].FullName)))
        //        {
        //            BattleEffectTextRoot battleEffectTextRoot = (BattleEffectTextRoot)new XmlSerializer(typeof(BattleEffectTextRoot)).Deserialize(stringReader);
        //            for (int j = 0; j < battleEffectTextRoot.effectTextList.Count; j++)
        //            {
        //                BattleEffectText battleEffectText = battleEffectTextRoot.effectTextList[j];
        //                dictionary.Add(battleEffectText.ID, battleEffectText);
        //            }
        //        }
        //    }
        //}

        private static void InitStageClassInfo()
        {
            var data = Singleton<StageClassInfoList>.Instance.GetData(new LorId(ModData.WorkshopId, 1));
            Singleton<StageClassInfoList>.Instance.recipeCondList.Add(data);
        }

        private static void AddAppearanceProjections()
        {

            var skinDataField = typeof(CustomizingResourceLoader).GetField("_skinData", BindingFlags.NonPublic | BindingFlags.Instance);
            var _skinData = (Dictionary<string, Workshop.WorkshopSkinData>)skinDataField.GetValue(Singleton<CustomizingResourceLoader>.Instance);
            foreach (var charName in new[]{ "Johanna", "Linus", "Sheire"})
            {
                var appearanceInfo = Workshop.WorkshopAppearanceItemLoader.LoadCustomAppearance(ModData.ModPath.FullName + Sep + "Resource" + Sep + "CharacterSkin" + Sep + $"{charName}KeyPage");
                appearanceInfo.uniqueId = appearanceInfo.uniqueId ?? charName;
                if (!_skinData.ContainsKey(appearanceInfo.uniqueId))
                {
                    var workshopSkinData = new Workshop.WorkshopSkinData
                    { 
                        dic = appearanceInfo.clothCustomInfo,
                        dataName = appearanceInfo.bookName,
                        contentFolderIdx = appearanceInfo.uniqueId,
                        id = _skinData.Count + 1,
                    };
                    _skinData.Add(appearanceInfo.uniqueId, workshopSkinData);
                }
            }
        }
    }
}
