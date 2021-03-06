﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AIProject;
using HarmonyLib;
using Localize.Translate;
using UnityEngine;
using UnityEngine.UI;
using UnityEx;
using static IllusionMods.TextResourceHelper.Helpers;
using Resources = Manager.Resources;

namespace IllusionMods
{
    public partial class AI_INT_LocalizationDumpHelper
    {
        private static readonly string[] AssetBundleRequired = {"/"};
        private static readonly string[] AssetBundleForbidden = {"<", ">", ":"};
        private static readonly string[] AssetForbidden = {"/"};

        protected IEnumerable<KeyValuePair<GameObject, Text>> EnumerateTexts(GameObject gameObject,
            MonoBehaviour component, HashSet<object> handled = null, List<UIBinder> binders = null)
        {
            var _ = binders;
            if (handled is null)
            {
                handled = new HashSet<object>();
            }

            if (!handled.Contains(component))
            {
                handled.Add(component);
                if (component is Text text)
                {
                    //Logger.LogInfo($"EnumerateTexts: {gameObject} yield {text}");
                    yield return new KeyValuePair<GameObject, Text>(gameObject, text);
                }
                else
                {
                    var trav = Traverse.Create(component);
                    foreach (var fieldName in trav.Fields())
                    {
                        var field = trav.Field(fieldName);
                        var fieldType = field.GetValueType();
                        if (fieldType == typeof(Text))
                        {
                            var fieldValue = field.GetValue<Text>();
                            if (fieldValue != null && !handled.Contains(fieldValue))
                            {
                                //Logger.LogInfo($"EnumerateTexts: {gameObject} field {fieldName} text {fieldValue}");
                                yield return new KeyValuePair<GameObject, Text>(gameObject, fieldValue);
                            }
                        }
                        else if (typeof(MonoBehaviour).IsAssignableFrom(fieldType))
                        {
                            var subBehaviour = field.GetValue<MonoBehaviour>();
                            if (subBehaviour != null && !handled.Contains(subBehaviour))
                            {
                                foreach (var subValue in EnumerateTexts(gameObject, subBehaviour, handled))
                                {
                                    yield return subValue;
                                }
                            }
                        }

                        /*
                        else if (typeof(GameObject).IsAssignableFrom(fieldType))
                        {
                            var subObject = field.GetValue<GameObject>();
                            if (subObject != null && !handled.Contains(subObject))
                            {
                                handled.Add(subObject);
                                Logger.LogInfo($"EnumerateTexts: {gameObject} field {fieldName} GameObject {subObject}");
                                foreach (var subValue in EnumerateTexts(subObject, handled))
                                {
                                    yield return subValue;
                                }
                            }
                        }
                        */
                    }
                }
            }
        }

        private IEnumerable<KeyValuePair<GameObject, Text>> EnumerateTexts(GameObject gameObject,
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

            foreach (var text in gameObject.GetComponents<Text>())
            {
                //Logger.LogInfo($"EnumerateTexts: {gameObject} GetComponents (text) {text}");
                yield return new KeyValuePair<GameObject, Text>(gameObject, text);
            }

            foreach (var component in gameObject.GetComponents<MonoBehaviour>())
            {
                foreach (var result in EnumerateTexts(gameObject, component, handled, binders))
                {
                    yield return result;
                }
            }

            foreach (var childText in GetChildrenFromGameObject(gameObject)
                .SelectMany(child => EnumerateTexts(child, handled, binders)))
            {
                yield return childText;
            }
        }

        private bool GetAssetInfo(List<string> address, ref int idx, out AssetBundleInfo assetBundleInfo)
        {
            var startIdx = idx;
            var result = new AssetBundleInfo(
                address.GetElement(idx++) ?? string.Empty,
                address.GetElement(idx++) ?? string.Empty,
                (address.GetElement(idx++) ?? string.Empty).Replace(".asset", string.Empty),
                address.GetElement(idx++) ?? string.Empty);
            assetBundleInfo = result;
            if (!IsAssetBundleInfoValid(result))
            {
                //Logger.LogError($"GetAssetInfo: BAD INFO: name={result.name}, assetbundle={result.assetbundle}, asset={result.asset}, manifest={result.manifest}");
                idx = startIdx;
                return false;
            }

            //Logger.LogWarning($"GetAssetInfo: name={result.name}, assetbundle={result.assetbundle}, asset={result.asset}, manifest={result.manifest}");
            return true;
        }

        private bool GetAssetInfo(string name, List<string> address, ref int idx, out AssetBundleInfo assetBundleInfo)
        {
            var startIdx = idx;
            var result = new AssetBundleInfo(
                name,
                address.GetElement(idx++) ?? string.Empty,
                (address.GetElement(idx++) ?? string.Empty).Replace(".asset", string.Empty),
                address.GetElement(idx++) ?? string.Empty);
            assetBundleInfo = result;
            if (!IsAssetBundleInfoValid(result))
            {
                //Logger.LogError($"GetAssetInfo: BAD INFO: name={result.name}, assetbundle={result.assetbundle}, asset={result.asset}, manifest={result.manifest}");
                idx = startIdx;
                return false;
            }

            //Logger.LogWarning($"GetAssetInfo: name={result.name}, assetbundle={result.assetbundle}, asset={result.asset}, manifest={result.manifest}");
            return true;
        }

        public static bool IsAssetBundleInfoValid(AssetBundleInfo assetBundleInfo)
        {
            return
                !string.IsNullOrEmpty(assetBundleInfo.assetbundle) &&
                !AssetBundleForbidden.Any(s => assetBundleInfo.assetbundle.Contains(s)) &&
                !string.IsNullOrEmpty(assetBundleInfo.asset) &&
                !AssetForbidden.Any(s => assetBundleInfo.asset.Contains(s)) &&
                AssetBundleRequired.All(s => assetBundleInfo.assetbundle.Contains(s));
        }

        private Dictionary<int, KeyValuePair<string, List<AssetBundleInfo>>> LoadAssetBundleInfos(
            AssetBundleInfo assetBundleInfo)
        {
            var results = new Dictionary<int, KeyValuePair<string, List<AssetBundleInfo>>>();
            ExcelData excelData;
            try
            {
                excelData = ManualLoadAsset<ExcelData>(assetBundleInfo);
            }
            catch
            {
                excelData = null;
            }

            if (excelData != null) // && excelData.GetRow(0).Contains("AssetBundleName"))
            {
                foreach (var param in excelData.list)
                {
                    var row = param.list;
                    if (int.TryParse(row.GetElement(0) ?? string.Empty, out var startIdx))
                    {
                        var idx = startIdx;
                        var name = row.GetElement(idx++) ?? string.Empty;
                        var infos = new List<AssetBundleInfo>();
                        while (idx < row.Count)
                        {
                            if (!GetAssetInfo(string.Empty, row, ref idx, out var info))
                            {
                                break;
                            }

                            info.ClearManifest();
                            infos.Add(info);
                        }

                        if (infos.Count == 0) continue;

                        var entry = new KeyValuePair<string, List<AssetBundleInfo>>(name, infos);
                        results[startIdx] = entry;
                    }
                }
            }

            return results;
        }

        private GameObject[] LoadGameObjects(IEnumerable<AssetBundleInfo> assetBundleInfos)
        {
            return LoadGameObjects(assetBundleInfos.ToArray());
        }

        private GameObject[] LoadGameObjects(params AssetBundleInfo[] assetBundleInfos)
        {
            var results = new GameObject[0];
            var gameObjects = ListPool<GameObject>.Get();
            foreach (var abi in assetBundleInfos)
            {
                GameObject gameObject;

                //Logger.LogError($"LoadGameObjects: assetbundle={abi.assetbundle}, asset={abi.asset}, manifest={abi.manifest}");
                try
                {
                    gameObject = ManualLoadAsset<GameObject>(abi);
                }
                catch
                {
                    gameObject = null;
                }

                if (gameObject != null)
                {
                    //Singleton<Manager.Resources>.Instance.AddLoadAssetBundle(abi.assetbundle, abi.manifest);
                    //Logger.LogFatal($"LoadGameObjects: {gameObject.name}");
                    gameObjects.Add(gameObject);
                }
            }

            if (!gameObjects.IsNullOrEmpty())
            {
                results = new GameObject[gameObjects.Count];
                for (var l = 0; l < gameObjects.Count; l++)
                {
                    results[l] = gameObjects[l];
                }
            }

            ListPool<GameObject>.Release(gameObjects);

            return results;
        }

        private bool IsRedirectTable(AssetBundleInfo assetBundleInfo, out ExcelData excelData)
        {
            try
            {
                excelData = ManualLoadAsset<ExcelData>(assetBundleInfo);
            }
            catch
            {
                excelData = null;
            }

            if (excelData != null && IsRedirectTable(excelData))
            {
                return true;
            }

            excelData = null;
            return false;
        }

        private bool IsRedirectTable(ExcelData excelData)
        {
            if (excelData.MaxCell > 1)
            {
                var row = excelData.GetRow(1);
                if (int.TryParse(row.GetElement(0), out _))
                {
                    var idx = 1;
                    if (GetAssetInfo(row, ref idx, out _))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsAssetTable(AssetBundleInfo assetBundleInfo, out ExcelData excelData, out int startIdx)
        {
            try
            {
                excelData = ManualLoadAsset<ExcelData>(assetBundleInfo);
            }
            catch
            {
                excelData = null;
            }

            if (excelData != null && IsAssetTable(excelData, out startIdx))
            {
                return true;
            }

            excelData = null;
            startIdx = -1;
            return false;
        }

        private bool IsAssetTable(ExcelData excelData, out int startIdx)
        {
            if (excelData.MaxCell > 1)
            {
                var headers = ResourceHelper.GetExcelHeaderRow(excelData, out var firstRow);
                startIdx = headers.IndexOf("AssetBundleName");
                //Logger.LogFatal($"IsAssetTable: {startIdx}");
                if (startIdx != -1)
                {
                    var row = excelData.GetRow(firstRow);
                    //Logger.LogFatal($"IsAssetTable: {startIdx}: '{string.Join("', '", row.ToArray())}'");
                    if (GetAssetInfo(string.Empty, row, ref startIdx, out _))
                    {
                        return true;
                    }
                }
            }

            startIdx = -1;
            return false;
        }

        private IEnumerable<AssetBundleInfo> GetRedirectTables(AssetBundleInfo assetBundleInfo)
        {
            if (IsRedirectTable(assetBundleInfo, out var excelData))
            {
                yield return assetBundleInfo;

                foreach (var entry in excelData.list)
                {
                    var row = entry.list;
                    var idx = 0;
                    if (int.TryParse(row.GetElement(idx++) ?? string.Empty, out _))
                    {
                        if (GetAssetInfo(row, ref idx, out var nestedAssetBundleInfo))
                        {
                            nestedAssetBundleInfo.ClearManifest();
                            foreach (var subentry in GetRedirectTables(nestedAssetBundleInfo))
                            {
                                yield return subentry;
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<AssetBundleInfo> GetRedirectTables(string assetBundlePath)
        {
            foreach (var abi in GetAssetBundleInfos(assetBundlePath))
            {
                foreach (var redirectTable in GetRedirectTables(abi))
                {
                    yield return redirectTable;
                }
            }
        }

        private IEnumerable<AssetBundleInfo> GetAssetTables(AssetBundleInfo assetBundleInfo)
        {
            
            if (IsAssetTable(assetBundleInfo, out _, out _))
            {
                yield return assetBundleInfo;
            }
            else if (IsRedirectTable(assetBundleInfo, out var excelData))
            {
                foreach (var entry in excelData.list)
                {
                    var row = entry.list;
                    var idx = 0;
                    if (!int.TryParse(row.GetElement(idx++) ?? string.Empty, out _)) continue;
                    if (!GetAssetInfo(row, ref idx, out var nestedAssetBundleInfo)) continue;

                    nestedAssetBundleInfo.ClearManifest();
                    foreach (var subEntry in GetAssetTables(nestedAssetBundleInfo))
                    {
                        yield return subEntry;
                    }
                }
            }
        }

        private IEnumerable<AssetBundleInfo> GetAssetBundleInfos(string assetPath)
        {
            var assetBundleNames = GetAssetBundleNameListFromPath(assetPath);
            assetBundleNames.Sort();
            foreach (var assetBundleName in assetBundleNames)
            {
                string[] assetNames = null;
                try
                {
                    assetNames = GetAssetNamesFromBundle(assetBundleName);
                }
                catch
                {
                    assetNames = null;
                }

                if (assetNames is null) continue;
                foreach (var assetName in assetNames)
                {
                    yield return new AssetBundleInfo(string.Empty, assetBundleName, assetName, null);
                }
            }
        }

        private GameObject[] GetAssetTableObjects(AssetBundleInfo assetTable)
        {
            var infos = new List<AssetBundleInfo>();
            if (IsAssetTable(assetTable, out var excelData, out var startIdx))
            {
                var first = true;
                foreach (var entry in excelData.list)
                {
                    if (first)
                    {
                        first = false;
                        continue;
                    }

                    var row = entry.list;
                    var idx = startIdx;
                    while (idx < row.Count)
                    {
                        if (GetAssetInfo(string.Empty, row, ref idx, out var nestedAssetBundleInfo))
                        {
                            nestedAssetBundleInfo.ClearManifest();
                            infos.Add(nestedAssetBundleInfo);
                        }
                    }
                }
            }

            return LoadGameObjects(infos);
        }

        private IEnumerable<ITranslationDumper> GetBindLocalizers(string assetPath)
        {
            var handled = new HashSet<object>();
            foreach (var entry in GetAssetBundleInfos(assetPath))
            {
                var path = CombinePaths(
                    Path.GetDirectoryName(entry.assetbundle),
                    Path.GetFileNameWithoutExtension(entry.asset));
                foreach (var gameObject in LoadGameObjects(entry))
                {
                    var outputName = $"Bind/{path}/{gameObject.name}";

                    Dictionary<string, string> Localizer()
                    {
                        var binders = new List<UIBinder>();
                        var textList = EnumerateTexts(gameObject, handled, binders).Select(t => t.Value).ToArray();
                        var before = textList.Select(t => t.text).ToArray();

                        foreach (var binder in binders)
                        {
                            var binderLoad = Traverse.Create(binder).Method("Load");
                            if (binderLoad?.MethodExists() == true)
                            {
                                binderLoad.GetValue();
                            }
                        }

                        var results = new Dictionary<string, string>();
                        var after = textList.Select(t => t.text).ToArray();
                        for (var i = 1; i < before.Length; i++)
                        {
                            AddLocalizationToResults(results, before[i], after[i]);
                        }

                        return results;
                    }

                    yield return new StringTranslationDumper(outputName, Localizer);
                }
            }
        }

        private TranslationDumper<IDictionary<string, string>>.TranslationCollector MakePopupLocalizer<TValue>(
            IEnumerable<KeyValuePair<int, TValue>> dict,
            Func<TValue, IEnumerable<string[]>> converter)
        {
            //Logger.LogError(dict);
            Dictionary<string, string> Collector()
            {
                var results = new Dictionary<string, string>();
                if (dict is null)
                {
                    //Logger.LogError("dict is null!");
                }
                else
                {
                    foreach (var entry in dict)
                    {
                        foreach (var item in converter(entry.Value))
                        {
                            if (item.Length > 0 && !string.IsNullOrEmpty(item[0]))
                            {
                                AddLocalizationToResults(results, item[0], item.Length > 1 ? item[1] : string.Empty);
                            }
                        }
                    }
                }

                return results;
            }

            return Collector;
        }

        private TranslationDumper<IDictionary<string, string>>.TranslationCollector MakeRecipeLocalizer(
            IEnumerable<KeyValuePair<int, RecipeDataInfo[]>> table)
        {
            Dictionary<string, string> Collector()
            {
                var results = new Dictionary<string, string>();
                foreach (var entry in table)
                {
                    foreach (var item in entry.Value)
                    {
                        var itemInfo = Singleton<Resources>.Instance.GameInfo.FindItemInfo(item.nameHash);
                        var name = string.Empty;
                        var explanation = string.Empty;

                        Singleton<Resources>.Instance.Localize.ConvertTranslateItem(itemInfo.CategoryID, itemInfo.ID,
                            ref name, ref explanation);

                        AddLocalizationToResults(results, itemInfo.Name, name);
                        AddLocalizationToResults(results, itemInfo.Explanation, explanation);
                    }
                }

                return results;
            }

            return Collector;
        }

        private StringTranslationDumper MapLabelPostProcessor(StringTranslationDumper localizer)
        {
            IDictionary<string, string> PostLocalizer()
            {
                var results = localizer.Collector();
                var keys = results.Keys.ToArray();
                foreach (var key in keys)
                {
                    var value = results[key];
                    if (!string.IsNullOrEmpty(value))
                    {
                        value = $"：{value}";
                    }

                    AddLocalizationToResults(results, $"：{key}", value);
                }

                return results;
            }

            return new StringTranslationDumper(localizer.Path, PostLocalizer);
        }

        #region extracted data

#if false
        protected readonly Dictionary<int, Dictionary<string, string>> OtherDataByTag =
            new Dictionary<int, Dictionary<string, string>>
            {
                {
                    100,
                    new Dictionary<string, string>
                    {
                        {"DeleteScene", "データを消去しますか？"},
                        {
                            "DeleteWarning",
                            string.Concat("本当に削除しますか？\n", "このキャラにはパラメータが含まれています。".Coloring("#DE4529FF").Size(24))
                        },
                        {"Delete", "本当に削除しますか？"},
                        {"EndHousing", "ハウジングを終了しますか？"},
                        {"EndHScene", "Hシーンを終了しますか"},
                        {
                            "LoadScene",
                            string.Concat("データを読込みますか？\n", "セットされたアイテムは削除されます。".Coloring("#DE4529FF").Size(24))
                        },
                        {"Migration", "{0}に移動しますか？"},
                        {
                            "OverwriteWarn",
                            string.Concat("本当に上書きしますか？\n", "上書きするとパラメータは初期化されます。".Coloring("#DE4529FF").Size(24))
                        },
                        {"Overwrite", "本当に上書きしますか？"},
                        {"ReleaseHousingItem", "作成しますか"},
                        {
                            "RestoreScene",
                            string.Concat("初期化しますか？\n", "セットされたアイテムは削除されます。".Coloring("#DE4529FF").Size(24))
                        },
                        {"Save", "セーブしますか？"},
                        {"SleepTogether", "一緒に寝た場合2人で行動状態が解除されます。"},
                        {"Sleep", "一日を終了しますか？"},
                        {"Teleport", "このポイントに移動しますか"},
                        {"Warp", "移動しますか"},
                        {
                            "ReleasePet",
                            string.Concat("{0}を逃しますか？\n", "逃がすとアイテムとして戻ってきません。".Coloring("#DE4529FF").Size(24))
                        }
                    }
                }
            };
#endif

        private readonly Dictionary<string, Dictionary<int, string>> _managerResources =
            new Dictionary<string, Dictionary<int, string>>
            {
                //{"ActionName", new Dictionary<int, string> { } }, // handled
                {
                    "AgentCommandLabel",
                    new Dictionary<int, string>
                    {
                        {0, "トーク"},
                        {1, "アドバイスする"},
                        {2, "アイテムを渡す"},
                        {3, "頼まれもの"},
                        {4, "薬をあげる"},
                        {5, "ついてきて"},
                        {6, "エッチがしたい"},
                        {7, "エッチなことをする"},
                        {8, "立ち去る"},
                        {9, "印象を聞く"},
                        {10, "調子どう？"},
                        {11, "誰と仲良し？"},
                        {12, "お気に入りの場所は？"},
                        {13, "おだてる"},
                        {14, "エッチな会話"},
                        {15, "戻る"},
                        {16, "容姿についてほめる"},
                        {17, "内面についてほめる"},
                        {18, "寝に行ったら？"},
                        {19, "少し休んだら？"},
                        {20, "採取手伝って"},
                        {21, "なにか食べたら？"},
                        {22, "なにか飲んだら？"},
                        {23, "料理作って"},
                        {24, "たまには気分転換を"},
                        {25, "エッチな行動を要求"},
                        {26, "解散する"},
                        {27, "3人でエッチがしたい"},
                        {28, "起こす"},
                        {29, "トイレ行ったら？"},
                        {30, "風呂入ったら？"}
                    }
                },
                //{"BaseName", new Dictionary<int, string> { } },
                {
                    "CommandTitle",
                    new Dictionary<int, string>
                    {
                        {0, "トーク"},
                        {1, "アドバイス"},
                        {2, "おだてる"},
                        {3, "拠点"},
                        {4, "データ端末"},
                        {5, "移動先"},
                        {6, "睡眠"},
                        {7, "料理"},
                        {8, "食事"},
                        {9, "特殊エッチ"},
                        {10, "鶏小屋"},
                        {11, "どう始める？"},
                        {12, "ワープ装置"}
                    }
                },

                {
                    "ConfigTag",
                    new Dictionary<int, string>
                    {
                        {0, "カメラ設定"},
                        {1, "描画レベル"},
                        {2, "オーディオ設定"},
                        {3, "エッチシーン設定"},
                        {4, "ゲーム設定"},
                        {5, "エッチシーン設定"}
                    }
                },
                //{"DateActionName", new Dictionary<int, string> { } }, //handled
                {
                    "GuideText",
                    new Dictionary<int, string>
                    {
                        {0, "決定"},
                        {1, "もどる"},
                        {2, "項目選択"},
                        {3, "拡大・縮小"},
                        {4, "決定"},
                        {5, "マップを閉じる"}
                    }
                },
                {
                    "HCommandLabel",
                    new Dictionary<int, string>
                    {
                        {0, "リードする"},
                        {1, "してほしい"},
                        {2, "いきなり挿入する"},
                        {3, "奉仕させる"},
                        {4, "挿入する"}
                    }
                },
                //{"HName", new Dictionary<int, string> { } }, // handled
                //{"MapName", new Dictionary<int, string> { } }, // handled
                {
                    "MerchantCommandLabel",
                    new Dictionary<int, string>
                    {
                        {0, "話す"},
                        {1, "ショップ"},
                        {2, "エッチさせて"},
                        {3, "立ち去る"}
                    }
                },
                //{"MinimapIcon", new Dictionary<int, string> { } }, // handled
                {
                    "PointCommandLabel",
                    new Dictionary<int, string>
                    {
                        {0, "そのまま寝転ぶ"},
                        {1, "寝る"},
                        {2, "起きる"},
                        {3, "料理をする"},
                        {4, "貯蔵庫を見る"},
                        {5, "立ち去る"},
                        {6, "ハウジング"},
                        {7, "女の子を登場"},
                        {8, "女の子を変更"},
                        {9, "女の子の容姿変更"},
                        {10, "女の子の住む島を変更"},
                        {11, "主人公を変更"},
                        {12, "主人公の容姿変更"},
                        {13, "タマゴ箱を確認"},
                        {14, "ニワトリを追加"},
                        {15, "一緒にご飯を食べる"},
                        {16, "分娩台"},
                        {17, "木馬"},
                        {18, "拘束台鞍馬"},
                        {19, "ギロチン"},
                        {20, "拘束デンマ台"},
                        {21, "拘束機械姦"},
                        {22, "吊るし挿入"},
                        {23, "移動する"}
                    }
                }
                //{"SickName", new Dictionary<int, string> { } } // handled
            };

        #endregion extracted data
    }
}
