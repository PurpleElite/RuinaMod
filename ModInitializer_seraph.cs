using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using Workshop;

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
        private static readonly string FaceCustomDir = "FaceCustom";
        public override void OnInitializeMod()
        {
            var assemblyPath = new DirectoryInfo(Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path)));
            ModData.ModPath = assemblyPath.Parent;
            GetResources(new DirectoryInfo((ModData.ModPath?.ToString()) + Sep + "Resource"));
            InitStageClassInfo();
            AddAppearanceProjections();
            AddCustomHairFace();
            PatchRoadmap.Patch();
            PatchBookThumbnail.Patch();
            PatchPages.Patch();
            PatchStorySprites.Patch();
            PatchVoiceLines.Patch();
            PatchAchievements.Patch();
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

            foreach (System.IO.FileInfo fileInfo in parentDir.GetFiles())
            {
                //Sprites
                if (fileInfo.Extension == ".png")
                {
                    Texture2D texture2D = new Texture2D(2, 2);
                    texture2D.LoadImage(File.ReadAllBytes(fileInfo.FullName));
                    var pivot = new Vector2(0, 0);
                    if (fileInfo.DirectoryName.Contains(FaceCustomDir))
                    {
                        pivot.x = 0.5f;
                        pivot.y = 0.5f;
                    }
                    Sprite value = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), pivot);
                    value.texture.wrapMode = TextureWrapMode.Clamp;
                    ModData.Sprites[GetImageName(fileInfo)] = value;
                }
                else
                {
                    //Sounds
                    var audioType = AudioType.UNKNOWN;
                    switch (fileInfo.Extension)
                    {
                        case ".wav":
                            audioType = AudioType.WAV; break;
                        case ".ogg":
                            audioType = AudioType.OGGVORBIS; break;
                    }
                    if (audioType != AudioType.UNKNOWN && File.Exists(fileInfo.FullName))
                    {
                        var webRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + fileInfo.FullName, audioType);
                        webRequest.SendWebRequest();
                        if (webRequest.isNetworkError)
                        {
                            Debug.Log(webRequest.error);
                        }
                        else
                        {
                            audioDownloads.Enqueue((webRequest.downloadHandler as DownloadHandlerAudioClip, Path.GetFileNameWithoutExtension(fileInfo.Name)));
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

            string GetImageName(System.IO.FileInfo fileInfo)
            {
                var dirArray = fileInfo.FullName.Split(Sep);
                var rootIndex = Array.IndexOf(dirArray, "Resource");
                var subDirArray = dirArray.Skip(rootIndex + 1);
                if (subDirArray.Count() > 2)
                {
                    subDirArray = subDirArray.Take(2).Concat(new[] { subDirArray.Last() });
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
            var _skinData = (Dictionary<string, WorkshopSkinData>)skinDataField.GetValue(Singleton<CustomizingResourceLoader>.Instance);
            foreach (var charName in new[] { "Johanna", "Linus", "Sheire" })
            {
                var appearanceInfo = WorkshopAppearanceItemLoader.LoadCustomAppearance(ModData.ModPath.FullName + Sep + "Resource" + Sep + "CharacterSkin" + Sep + $"{charName}KeyPage");
                appearanceInfo.uniqueId = appearanceInfo.uniqueId ?? charName;
                if (!_skinData.ContainsKey(appearanceInfo.uniqueId))
                {
                    var workshopSkinData = new WorkshopSkinData
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

        private static void AddCustomHairFace()
        {
            Dictionary<string, List<(Workshop.FaceCustomType Type, Sprite Sprite)>> appearanceSprites = new Dictionary<string, List<(Workshop.FaceCustomType, Sprite)>>();

            var sprites = ModData.Sprites.Where(x => x.Key.Contains(FaceCustomDir));
            var rx = new Regex($@"{FaceCustomDir}_(?<name>[a-zA-Z]+)_(?<type>.+)");
            foreach (var sprite in sprites)
            {
                MatchCollection matches = rx.Matches(sprite.Key);
                GroupCollection groups = matches[0].Groups;
                var character = groups["name"].Value;
                var typeString = groups["type"].Value;
                if (Enum.TryParse(typeString, out FaceCustomType type))
                {
                    if (!appearanceSprites.ContainsKey(character))
                    {
                        appearanceSprites[character] = new List<(Workshop.FaceCustomType, Sprite)>();
                    }
                    appearanceSprites[character].Add(((Workshop.FaceCustomType, Sprite))(type, sprite.Value));
                }
            }

            var eyeResourcesField = typeof(CustomizingResourceLoader).GetField("_eyeResources", BindingFlags.NonPublic | BindingFlags.Instance);
            var _eyeResources = (List<FaceResourceSet>)eyeResourcesField.GetValue(Singleton<CustomizingResourceLoader>.Instance);
            var browResourcesField = typeof(CustomizingResourceLoader).GetField("_browResources", BindingFlags.NonPublic | BindingFlags.Instance);
            var _browResources = (List<FaceResourceSet>)browResourcesField.GetValue(Singleton<CustomizingResourceLoader>.Instance);
            var mouthResourcesField = typeof(CustomizingResourceLoader).GetField("_mouthResources", BindingFlags.NonPublic | BindingFlags.Instance);
            var _mouthResources = (List<FaceResourceSet>)mouthResourcesField.GetValue(Singleton<CustomizingResourceLoader>.Instance);
            var frontHairResourcesField = typeof(CustomizingResourceLoader).GetField("_frontHairResources", BindingFlags.NonPublic | BindingFlags.Instance);
            var _frontHairResources = (List<HairResourceSet>)frontHairResourcesField.GetValue(Singleton<CustomizingResourceLoader>.Instance);
            var rearHairResourcesField = typeof(CustomizingResourceLoader).GetField("_rearHairResources", BindingFlags.NonPublic | BindingFlags.Instance);
            var _rearHairResources = (List<HairResourceSet>)rearHairResourcesField.GetValue(Singleton<CustomizingResourceLoader>.Instance);

            foreach (var character in appearanceSprites)
            {
                var eyeResourceSet = new FaceResourceSet();
                var browResourceSet = new FaceResourceSet();
                var mouthResourceSet = new FaceResourceSet();
                var frontHairResourceSet = new HairResourceSet();
                var rearHairResourceSet = new HairResourceSet();
                foreach (var sprite in character.Value)
                {
                    var value = sprite.Sprite;
                    switch (sprite.Type)
                    {
                        case Workshop.FaceCustomType.Front_RearHair:
                            rearHairResourceSet.Default = value;
                            break;
                        case Workshop.FaceCustomType.Front_FrontHair:
                            frontHairResourceSet.Default = value;
                            break;
                        case Workshop.FaceCustomType.Front_Eye:
                            eyeResourceSet.normal = value;
                            break;
                        case Workshop.FaceCustomType.Front_Brow_Normal:
                            browResourceSet.normal = value;
                            break;
                        case Workshop.FaceCustomType.Front_Brow_Attack:
                            browResourceSet.atk = value;
                            break;
                        case Workshop.FaceCustomType.Front_Brow_Hit:
                            browResourceSet.hit = value;
                            break;
                        case Workshop.FaceCustomType.Front_Mouth_Normal:
                            mouthResourceSet.normal = value;
                            break;
                        case Workshop.FaceCustomType.Front_Mouth_Attack:
                            mouthResourceSet.atk = value;
                            break;
                        case Workshop.FaceCustomType.Front_Mouth_Hit:
                            mouthResourceSet.hit = value;
                            break;
                        case Workshop.FaceCustomType.Side_RearHair_Rear:
                            rearHairResourceSet.Side_Back = value;
                            break;
                        case Workshop.FaceCustomType.Side_FrontHair:
                            frontHairResourceSet.Side_Front = value;
                            break;
                        case Workshop.FaceCustomType.Side_RearHair_Front:
                            rearHairResourceSet.Side_Front = value;
                            break;
                        case Workshop.FaceCustomType.Side_Mouth:
                            mouthResourceSet.atk_side = value;
                            break;
                        case Workshop.FaceCustomType.Side_Brow:
                            browResourceSet.atk_side = value;
                            break;
                        case Workshop.FaceCustomType.Side_Eye:
                            eyeResourceSet.atk_side = value;
                            break;
                    }
                }
                eyeResourceSet.FillSprite();
                browResourceSet.FillSprite();
                mouthResourceSet.FillSprite();

                _eyeResources.Add(eyeResourceSet);
                _browResources.Add(browResourceSet);
                _mouthResources.Add(mouthResourceSet);
                _frontHairResources.Add(frontHairResourceSet);
                _rearHairResources.Add(rearHairResourceSet);
            }
        }
    }
}
