﻿using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace IllusionMods
{
    public partial class BaseDumpHelper
    {
        private ManualLogSource _logger;

        public BaseDumpHelper(TextDump plugin)
        {
            Plugin = plugin;
        }

        protected ManualLogSource Logger => _logger = _logger ?? BepInEx.Logging.Logger.CreateLogSource(GetType().Name);

        protected TextDump Plugin { get; }

        protected TextResourceHelper ResourceHelper => Plugin?.TextResourceHelper;
        protected TextAssetTableHelper TableHelper => Plugin?.TextAssetTableHelper;

        public void AddLocalizationToResults(Dictionary<string, string> results, string origText, string transText)
        {
            ResourceHelper.AddLocalizationToResults(results, origText, transText);
        }

        public void AddLocalizationToResults(Dictionary<string, string> results, KeyValuePair<string, string> mapping)
        {
            ResourceHelper.AddLocalizationToResults(results, mapping);
        }

        public bool IsValidLocalization(string original, string localization)
        {
            return ResourceHelper.IsValidLocalization(original, localization);
        }

        public static TranslationGenerator WrapTranslationCollector(string path,
            TranslationCollector translationCollector)
        {
            IEnumerable<TranslationDumper> Generator()
            {
                yield return new TranslationDumper(path, translationCollector);
            }

            return Generator;
        }

        public static List<string> GetAllAssetBundles()
        {
            return TextDump.Helpers.GetAllAssetBundleNames();
        }

        public static List<string> GetAssetBundleNameListFromPath(string path, bool subdirCheck = false)
        {
            return TextDump.Helpers.GetAssetBundleNameListFromPath(path, subdirCheck);
        }

        public static void UnloadBundles()
        {
            TextDump.Helpers.UnloadBundles();
        }

        public static string[] GetAllAssetName(string assetBundleName)
        {
            return TextDump.Helpers.GetAllAssetName(assetBundleName);
        }

        public static T ManualLoadAsset<T>(string bundle, string asset, string manifest) where T : Object
        {
            return TextDump.Helpers.ManualLoadAsset<T>(bundle, asset, manifest);
        }
    }
}