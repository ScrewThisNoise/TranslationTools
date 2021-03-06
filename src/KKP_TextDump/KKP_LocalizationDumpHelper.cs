﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ActionGame.Point;
using HarmonyLib;
using IllusionMods.Shared;
using Localize.Translate;
using Manager;
using UnityEngine;
using UnityEngine.UI;
using static IllusionMods.TextResourceHelper.Helpers;

namespace IllusionMods
{
    public class KKP_LocalizationDumpHelper : LocalizationDumpHelper
    {
        private static readonly string[] SupportedEnumerationTypeNames =
        {
            "UnityEngine.UI.Text",
            "UnityEngine.TextMesh",
            "TMPro.TMP_Text",
            "TMPro.TextMeshProUGUI",
            "TMPro.TextMeshPro",
            "UILabel",
            "FairyGUI.TextField"
        };

        private static readonly Type[] SupportedEnumerationTypes;

        private static readonly Dictionary<string, Type> SupportedEnumerationTypeMap;

        static KKP_LocalizationDumpHelper()
        {
            FormatStringRegex = new Regex(@"(\{[0-9]\}|\[[PH][^\]]*\])");

            var typeMap = SupportedEnumerationTypeMap = new Dictionary<string, Type>();
            foreach (var typeName in SupportedEnumerationTypeNames)
            {
                Type type = null;
                try
                {
                    type = AccessTools.TypeByName(typeName);
                }
                catch (Exception)
                {
                    type = TextDump.Helpers.FindType(typeName);
                }

                if (type == null)
                {
                    TextDump.Logger.LogDebug(
                        $"SupportedEnumerationTypes: Unable to find type {typeName} {type}, skipping.");
                }

                typeMap[typeName] = type;
            }

            SupportedEnumerationTypes = typeMap.Values.Where(o => o != null).ToArray();
        }

        protected KKP_LocalizationDumpHelper(TextDump plugin) : base(plugin)
        {
            OtherDataByTag[0] = new Dictionary<string, string>
            {
                {"InitCheck", "容姿を初期化しますか？"},
                {"Check", "選択した場所に移動しますか？"},
                {"ServerCheck", "サーバーをチェックしています"},
                {"ServerAccessField", "サーバーへのアクセスに失敗しました。"},
                {"Other", "特殊"}
            };

            // translateCharaInfo
            OtherDataByTag[1] = new Dictionary<string, string>
            {
                {"Month", "月"},
                {"Day", "日"},
                {"SampleColor", "左クリックで適用"}
                //{"LoadCheck", ""},
                //{"SaveCheck", ""},
                //{"SaveCheckOverride", ""},
                //{"Saved", ""},
            };


            // translateQuestionTitle
            OtherDataByTag[3] = new Dictionary<string, string>
            {
                {"Remove", "転校させますか？"},
                {"FailedData", "データの取得に失敗しました"}
                //{"RemoveAll", ""},
                //{"RemoveClass", ""}
            };

            // translateMessageTitle
            OtherDataByTag[4] = new Dictionary<string, string>
            {
                {"Members", "{0} 人"},
                {"Times", "{0} 回"}
            };

            // translateOther
            // OtherDataByTag[5] = new Dictionary<string, string> {};

            OtherDataByTag[100] = new Dictionary<string, string>
            {
                {"ReturnHome", "家に帰りますか？"},
                {"RestoreDefault", "設定を初期化しますか？"}
            };

            // translateQuestionTitle
            OtherDataByTag[995] = new Dictionary<string, string>
            {
                {"Restore", "キャラを編集前に戻しますか？"},
                {"Change", "キャラ画像を変更しますか？"},
                {"Erase", "本当に削除しますか？"},
                {"CoordinateInput", "コーディネート名を入力して下さい"},
                {"Overwrite", "本当に上書きしますか？"}
            };

            // translateSlotTitle
            //OtherDataByTag[996] = new Dictionary<string, string> {};

            Harmony.CreateAndPatchAll(typeof(KKP_LocalizationDumpHelper));
        }

        public static List<SaveData.Heroine> LoadHeroines(string assetBundlePath)
        {
            var heroines = new Dictionary<int, TextAsset>();
            foreach (var assetBundleName in GetAssetBundleNameListFromPath(assetBundlePath))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName).Where(a => a.EndsWith(".bytes")))
                {
                    var textAsset = ManualLoadAsset<TextAsset>(assetBundleName, assetName, null);
                    if (textAsset == null) continue;

                    if (int.TryParse(textAsset.name.Replace("c", string.Empty), out var id))
                    {
                        heroines[id] = textAsset;
                    }
                }
            }

            return heroines.Select(h =>
            {
                var heroine = new SaveData.Heroine(false);
                Game.LoadFromTextAsset(h.Key, heroine, h.Value);
                return heroine;
            }).ToList();
        }

        protected static IEnumerable<KeyValuePair<GameObject, Component>> EnumerateTextComponents(GameObject gameObject,
            Component component, HashSet<object> handled = null, List<UIBinder> binders = null)
        {
            var _ = binders;
            if (handled is null)
            {
                handled = new HashSet<object>();
            }

            if (!handled.Contains(component))
            {
                handled.Add(component);
                if (IsSupportedForEnumeration(component))
                {
                    //Logger.LogInfo($"EnumerateTextComponents: {gameObject} yield {text}");
                    yield return new KeyValuePair<GameObject, Component>(gameObject, component);
                }
                else
                {
                    var trav = Traverse.Create(component);
                    foreach (var fieldName in trav.Fields())
                    {
                        var field = trav.Field(fieldName);
                        var fieldType = field.GetValueType();
                        if (IsSupportedForEnumeration(fieldType))
                        {
                            var fieldValue = field.GetValue<Component>();
                            if (fieldValue != null && !handled.Contains(fieldValue))
                            {
                                //Logger.LogInfo($"EnumerateTextComponents: {gameObject} field {fieldName} text {fieldValue}");
                                yield return new KeyValuePair<GameObject, Component>(gameObject, fieldValue);
                            }
                        }
                        else if (typeof(Component).IsAssignableFrom(fieldType))
                        {
                            var subComponent = field.GetValue<Component>();
                            if (subComponent != null && !handled.Contains(subComponent))
                            {
                                foreach (var subValue in EnumerateTextComponents(gameObject, subComponent, handled))
                                {
                                    yield return subValue;
                                }
                            }
                        }
                    }
                }
            }
        }

        protected static XuaResizerResult GetTextResizerFromComponent(Component component)
        {
            var result = new XuaResizerResult();
            var componentType = component.GetType();
            Type matchType;

            if (component is Text textComponent)
            {
                result.AutoResize = textComponent.resizeTextForBestFit;
                result.FontSize = textComponent.fontSize;
                result.LineSpacing = (decimal) textComponent.lineSpacing;
                result.HorizontalOverflow = textComponent.horizontalOverflow == HorizontalWrapMode.Overflow
                    ? XuaResizerResult.HorizontalOverflowValue.Overflow
                    : XuaResizerResult.HorizontalOverflowValue.Wrap;
                result.VerticalOverflow = textComponent.verticalOverflow == VerticalWrapMode.Overflow
                    ? XuaResizerResult.VerticalOverflowValue.Overflow
                    : XuaResizerResult.VerticalOverflowValue.Truncate;
            }

            // UILabel
            /*
            if (SupportedEnumerationTypeMap.TryGetValue("UILabel", out var uiLabelType) &&
                uiLabelType.IsAssignableFrom(componentType))
            {
                // nothing to track here?
                
            }
            */

            else if (SupportedEnumerationTypeMap.TryGetValue("TMPro.TextMeshPro", out matchType) &&
                     matchType.IsAssignableFrom(componentType) ||
                     SupportedEnumerationTypeMap.TryGetValue("TMPro.TextMeshProUGUI", out matchType) &&
                     matchType.IsAssignableFrom(componentType))
            {
                var nullArgs = new object[0];

                T GetComponentPropertyValue<T>(string name)
                {
                    var propInfo = AccessTools.Property(matchType, name);
                    if (propInfo != null)
                    {
                        return (T) propInfo.GetValue(component, nullArgs);
                    }

                    var fieldInfo = AccessTools.Field(matchType, name);
                    if (fieldInfo != null)
                    {
                        return (T) fieldInfo.GetValue(component);
                    }

                    return default;
                }

                result.AutoResize = GetComponentPropertyValue<bool?>("enableAutoSizing");
                result.FontSize = (decimal?) GetComponentPropertyValue<float?>("fontSize");
                result.LineSpacing = (decimal?) GetComponentPropertyValue<float?>("lineSpacing");

                /*
                yield return new KeyValuePair<string, string>(path,
                    $"UGUI_HorizontalOverflow({textComponent.horizontalOverflow.ToString()})");
                yield return new KeyValuePair<string, string>(path,
                    $"UGUI_VerticalOverflow({textComponent.verticalOverflow.ToString()})");
                */
            }
            return result;
        }

        protected override IEnumerable<TranslationGenerator> GetLocalizationGenerators()
        {
            foreach (var generator in base.GetLocalizationGenerators())
            {
                yield return generator;
            }

            var readyToDump = TextDump.IsReadyForFinalDump();


            yield return WrapTranslationCollector("Names/ScenarioChars", CollectScenarioCharsLocalizations);
            yield return WrapTranslationCollector("Names/Clubs", CollectClubNameLocalizations);
            yield return WrapTranslationCollector("Names/Heroines", CollectHeroineLocalizations);
            yield return WrapTranslationCollector("WakeUp", CollectCycleLocalizaitons);
        }

        // TODO: public static string GetClubName(int clubActivities, bool check)
        // TODO: public static string GetPersonalityName(int personality, bool check)

        /*
        private IEnumerable<ITranslationDumper> MakeManagerResourceLocalizers()
        {
            foreach (var resource in _managerResources)
            {
                var localize = Singleton<Manager.Resources>.Instance.Localize;
                //Logger.LogWarning(localize);
                var func = AccessTools.Method(localize.GetType(), $"Get{resource.Key}");
                //Logger.LogWarning(func);

                if (func is null)
                {
                    continue;
                }

                var getter = (Func<int, string>)Delegate.CreateDelegate(typeof(Func<int, string>), localize, func);

                //Logger.LogWarning(getter);
                Dictionary<string, string> Localizer()
                {
                    var results = new Dictionary<string, string>();
                    foreach (var entry in resource.Value)
                    {
                        AddLocalizationToResults(results, entry.Value, getter(entry.Key));
                    }

                    return results;
                }

                yield return new StringTranslationDumper($"Manager/Resources/{resource.Key}", Localizer);
            }
        }
        */

        protected override string GetPersonalityName(VoiceInfo.Param voiceInfo)
        {
            var assetBundleNames = GetAssetBundleNameListFromPath("etcetra/list/config/", true);
            foreach (var voice in from assetBundleName in assetBundleNames
                from assetName in GetAssetNamesFromBundle(assetBundleName)
                select ManualLoadAsset<VoiceInfo>(assetBundleName, assetName, null)
                into asset
                where !(asset is null)
                from voice in asset.param.Where(voice =>
                    voice.No == voiceInfo.No && !voice.Personality.IsNullOrWhiteSpace())
                select voice)
            {
                return voice.Personality;
            }

            return voiceInfo.Personality;
        }

        protected override string GetPersonalityNameLocalization(VoiceInfo.Param voiceInfo)
        {
            return Localize.Translate.Manager.GetPersonalityName(voiceInfo.No, false);
        }

        private static bool IsSupportedForEnumeration(Type type)
        {
            return type != null &&
                   SupportedEnumerationTypes.Any(supported => type == supported || type.IsSubclassOf(supported));
        }

        private static bool IsSupportedForEnumeration(Component component)
        {
            return IsSupportedForEnumeration(component.GetType());
        }

        private static string GetTextFromSupportedComponent(Component component)
        {
            if (component == null) return string.Empty;

            var componentType = component.GetType();
            var fieldInfo = AccessTools.Property(componentType, "text");
            if (fieldInfo != null)
            {
                return fieldInfo.GetValue(component, new object[0]) as string;
            }

            var propInfo = AccessTools.Property(componentType, "text");
            if (propInfo != null)
            {
                return propInfo.GetValue(component, new object[0]) as string;
            }

            TextDump.Logger.LogWarning($"Unable to access 'text' property for {component}");
            return string.Empty;
        }

        private static IEnumerable<KeyValuePair<GameObject, Component>> EnumerateTextComponents(GameObject gameObject,
            HashSet<object> handled = null, List<UIBinder> binders = null)
        {
            handled = handled ?? new HashSet<object>();

            if (handled.Contains(gameObject)) yield break;
            handled.Add(gameObject);

            if (binders != null)
            {
                foreach (var binder in gameObject.GetComponents<UIBinder>())
                {
                    if (!binders.Contains(binder))
                    {
                        binders.Add(binder);
                    }
                }
            }

            foreach (var component in gameObject.GetComponents<Component>())
            {
                //Logger.LogInfo($"EnumerateTextComponents: {gameObject} GetComponents (text) {text}");
                if (IsSupportedForEnumeration(component))
                {
                    yield return new KeyValuePair<GameObject, Component>(gameObject, component);
                }

                foreach (var result in EnumerateTextComponents(gameObject, component, handled, binders))
                {
                    yield return result;
                }
            }

            foreach (var childText in GetChildrenFromGameObject(gameObject)
                .SelectMany(child => EnumerateTextComponents(child, handled, binders)))
            {
                yield return childText;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIBinder), "Load")]
        private static void UIBinderLoadPrefix(UIBinder __instance, out TranslationHookState __state)
        {
            var gameObject = __instance.gameObject;
            var path = CombinePaths(gameObject.scene.path.Replace(".unity", ""), gameObject.name);
            TextDump.Logger.LogInfo($"[TextDump] Collecting UI info for {path}");
            var items = EnumerateTextComponents(gameObject).ToList();
            var components = items.Select(t => t.Value).ToList();
            var scopes = items.Select(t =>
            {
                try
                {
                    return t.Key.scene.buildIndex;
                }
                catch
                {
                    return -1;
                }
            }).ToList();


            __state = new TranslationHookState(path);

            __state.Context.Add(components);
            __state.Context.Add(scopes);
            var origValues = components.Select(GetTextFromSupportedComponent).ToList();
            __state.Context.Add(origValues);
            var origResizers = components.Select(GetTextResizerFromComponent).ToList();
            __state.Context.Add(origResizers);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIBinder), "Load")]
        private static void UIBinderLoadPostfix(UIBinder __instance, TranslationHookState __state)
        {
            var gameObject = __instance.gameObject;
            var path = __state.Path;

            var components = (List<Component>) __state.Context[0];
            var scopes = (List<int>) __state.Context[1];
            var origValues = (List<string>) __state.Context[2];
            var origResizers = (List<XuaResizerResult>) __state.Context[3];

            var items = EnumerateTextComponents(gameObject).ToList();
            if (items.Count != components.Count)
            {
                TextDump.Logger.LogWarning(
                    $"UIBinder {gameObject}: Component count has changed, may not be able to get all translations");
            }
            else
            {
                components = items.Select(t => t.Value).ToList();
            }

            var results = new TranslationDictionary();
            var resizers = new ResizerCollection();

            for (var i = 0; i < components.Count; i++)
            {
                var key = origValues[i];
                var val = GetTextFromSupportedComponent(components[i]);

                var scope = scopes[i];
                _instance.AddLocalizationToResults(results.GetScope(scope), key, val);


                var currentResizer = GetTextResizerFromComponent(components[i]);

                var resizePath = components[i].GetXuaResizerPath();
                if (!string.IsNullOrEmpty(resizePath))
                {
                    var delta = currentResizer.Delta(origResizers[i]);
                    var scopedResizers = resizers.GetScope(scope);
                    scopedResizers[resizePath] = delta.GetDirectives().ToList();
                }
            }

            var outputName = CombinePaths("Bind/UI", path);
            HookedTextLocalizationGenerators.Add(new StringTranslationDumper(outputName, () => results));
            HookedTextLocalizationGenerators.Add(new ResizerDumper(outputName, () => resizers));
        }

        private Dictionary<string, string> CollectCycleLocalizaitons()
        {
            var results = new Dictionary<string, string>();
            if (!Localize.Translate.Manager.initialized) return results;
            if (!Singleton<Game>.IsInstance()) return results;

            var cycle = Singleton<Game>.Instance?.actScene?.Cycle;

            if (cycle == null) return results;

            var getWords = AccessTools.Method(cycle.GetType(), "GetWords");
            if (getWords == null) return results;

            var lookupTable = new Dictionary<string, string>
            {
                {"Date", "_wakeUpDateWords"},
                {"Holiday", "_wakeUpHolidayWords"},
                {"Saturday", "_wakeUpSaturdayWords"},
                {"WakeUp", "_wakeUpWeekdayWords"}
            };

            foreach (var entry in lookupTable)
            {
                var prop = AccessTools.Field(cycle.GetType(), entry.Value);

                var orig = (string[]) prop?.GetValue(cycle);
                if (orig == null) continue;

                var trans = (string[]) getWords.Invoke(cycle, new object[] {entry.Key});
                for (var i = 0; i < orig.Length; i++)
                {
                    AddLocalizationToResults(results, orig[i],
                        trans != null && trans.Length > i ? trans[i] : string.Empty);
                }
            }

            // stray one in another location
            var wakeUpWordsProp = AccessTools.Field(typeof(ActionPoint), "_wakeUpWords");

            var wakeUpWords = (string[]) wakeUpWordsProp?.GetValue(cycle);

            if (wakeUpWords == null) return results;

            var wakeUpTranslations =
                Singleton<Game>.Instance.actScene.uiTranslater.Get(6).Values.ToArray("Sleep");

            for (var i = 0; i < wakeUpWords.Length; i++)
            {
                AddLocalizationToResults(results, wakeUpWords[i],
                    wakeUpTranslations != null && wakeUpTranslations.Length > i ? wakeUpTranslations[i] : string.Empty);
            }

            return results;
        }

        private IDictionary<string, string> CollectHeroineLocalizations()
        {
            var results = new OrderedDictionary<string, string>();
            var baseHeroines = LoadHeroines("action/fixchara");
            if (baseHeroines == null) return results;

            var translatedHeroines = LoadHeroines("localize/translate/1/defdata/action/fixchara");

            for (var i = 0; i < baseHeroines.Count; i++)
            {
                var baseHeroine = baseHeroines[i];
                var translatedHeroine = translatedHeroines != null && translatedHeroines.Count > i
                    ? translatedHeroines[i]
                    : null;

                var translatedFirst = translatedHeroine?.param.chara.firstname ?? string.Empty;
                var translatedLast = translatedHeroine?.param.chara.lastname ?? string.Empty;
                AddLocalizationToResults(results, baseHeroine.param.chara.firstname,
                    translatedFirst);
                AddLocalizationToResults(results, baseHeroine.param.chara.lastname,
                    translatedLast);
                AddLocalizationToResults(results, baseHeroine.param.chara.nickname,
                    translatedHeroine?.param.chara.nickname ?? string.Empty);

                // Add names in both orders
                AddLocalizationToResults(results,
                    JoinStrings(" ", baseHeroine.param.chara.firstname, baseHeroine.param.chara.lastname).Trim(),
                    JoinStrings(" ", translatedFirst, translatedLast).Trim());
                AddLocalizationToResults(results,
                    JoinStrings(" ", baseHeroine.param.chara.lastname, baseHeroine.param.chara.firstname).Trim(),
                    JoinStrings(" ", translatedLast, translatedFirst).Trim());
            }

            return results;
        }

        private Dictionary<string, string> CollectClubNameLocalizations()
        {
            var results = new Dictionary<string, string>();
            if (!Localize.Translate.Manager.initialized) return results;
            if (!Singleton<Game>.IsInstance()) return results;

            var clubInfos = Game.ClubInfos ?? new Dictionary<int, ClubInfo.Param>();


            var assetBundleNames = GetAssetBundleNameListFromPath("action/list/clubinfo/", true);
            foreach (var assetBundleName in assetBundleNames)
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var asset = ManualLoadAsset<ClubInfo>(assetBundleName, assetName, null);
                    if (asset is null) continue;
                    foreach (var club in asset.param.Where(club => !club.Name.IsNullOrEmpty()))
                    {
                        var localization = string.Empty;

                        if (clubInfos.TryGetValue(club.ID, out var clubParam))
                        {
                            localization = clubParam.Name;
                        }

                        AddLocalizationToResults(results, club.Name, localization);
                        AddLocalizationToResults(Plugin.TextResourceHelper.GlobalMappings, club.Name, localization);
                    }
                }
            }

            return results;
        }

        private Dictionary<string, string> CollectScenarioCharsLocalizations()
        {
            var results = new Dictionary<string, string>();
            if (!Localize.Translate.Manager.initialized) return results;

            var propInfo = AccessTools.Property(typeof(Localize.Translate.Manager), "ScenarioReplaceNameData");


            if (!(propInfo?.GetValue(null, new object[0]) is Dictionary<string, List<ScenarioCharaName.Param>>
                scenarioReplaceNameData))
            {
                return results;
            }

            foreach (var name in scenarioReplaceNameData.Values.SelectMany(nameList => nameList))
            {
                AddLocalizationToResults(results, name.Target, name.Replace);
                if (SpeakerLocalizations != null)
                {
                    AddLocalizationToResults(SpeakerLocalizations, name.Target, name.Replace);
                }

                AddLocalizationToResults(Plugin.TextResourceHelper.GlobalMappings, name.Target, name.Replace);
            }

            return results;
        }
    }
}
