using LOR_XML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using UnityEngine;

namespace CustomDLLs
{
    internal static class ModData
    {
        internal const string WorkshopId = "SeraphOffice";
        internal static string Language = "en";
        internal static DirectoryInfo AssembliesPath;
        internal static Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
    }

    public class ModInitializer_seraph : ModInitializer
    {
        public override void OnInitializeMod()
        {
            ModData.AssembliesPath = new DirectoryInfo(Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path)));
            GetSprites(new DirectoryInfo((ModData.AssembliesPath?.ToString()) + "/Sprites"));
            AddEffectText();
        }

        private static void GetSprites(DirectoryInfo parentDir)
        {
            if (parentDir.GetDirectories().Length != 0)
            {
                DirectoryInfo[] childDirs = parentDir.GetDirectories();
                for (int i = 0; i < childDirs.Length; i++)
                {
                    GetSprites(childDirs[i]);
                }
            }
            foreach (FileInfo fileInfo in parentDir.GetFiles())
            {
                Texture2D texture2D = new Texture2D(2, 2);
                texture2D.LoadImage(File.ReadAllBytes(fileInfo.FullName));
                Sprite value = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0f, 0f));
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileInfo.FullName);
                ModData.Sprites[fileNameWithoutExtension] = value;
            }
        }

        private static void AddEffectText()
        {
            Dictionary<string, BattleEffectText> dictionary =
                typeof(BattleEffectTextsXmlList).GetField("_dictionary", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(Singleton<BattleEffectTextsXmlList>.Instance) as Dictionary<string, BattleEffectText>;
            FileInfo[] files = new DirectoryInfo((ModData.AssembliesPath?.ToString()) + "/Localize/" + ModData.Language + "/EffectTexts").GetFiles();
            for (int i = 0; i < files.Length; i++)
            {
                using (StringReader stringReader = new StringReader(File.ReadAllText(files[i].FullName)))
                {
                    BattleEffectTextRoot battleEffectTextRoot = (BattleEffectTextRoot)new XmlSerializer(typeof(BattleEffectTextRoot)).Deserialize(stringReader);
                    for (int j = 0; j < battleEffectTextRoot.effectTextList.Count; j++)
                    {
                        BattleEffectText battleEffectText = battleEffectTextRoot.effectTextList[j];
                        dictionary.Add(battleEffectText.ID, battleEffectText);
                    }
                }
            }
        }
    }


}
