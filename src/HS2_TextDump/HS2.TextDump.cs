﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using IllusionMods.Shared;
using Manager;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.SceneManagement;
using static IllusionMods.TextDump.Helpers;

namespace IllusionMods
{
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextDump : BaseUnityPlugin
    {
        public const string PluginNameInternal = "HS2_TextDump";

        private static bool _LobbyLoaded = false;

        private static readonly string[] AssetPathsToWaitOn = new[]
        {
            "list/h/sound/voice",
            "list/characustom",
            "adv/scenario"
        };

        private int _stableCount = 0;
        private TranslationCount _lastTotal = new TranslationCount();
        private TranslationCount _lastDelta = new TranslationCount();
        private bool _waitOnRetry = false;

        static TextDump()
        {
            CurrentExecutionMode = ExecutionMode.BeforeFirstLoad;
            DumpLevelMax = 3;
        }

        public TextDump()
        {
            TextResourceHelper = CreateHelper<HS2_TextResourceHelper>();
            AssetDumpHelper = CreatePluginHelper<HS2_AssetDumpHelper>();
            LocalizationDumpHelper = CreatePluginHelper<HS2_LocalizationDumpHelper>();

            CheckReadyToDumpChecker = HS2_CheckReadyToDump;

            TextDumpAwake += HS2_TextDumpAwake;
            TextDumpLevelComplete += TextDump_TextDumpLevelComplete;

        }

        private void TextDump_TextDumpLevelComplete(TextDump sender, EventArgs eventArgs)
        {
            var delta = _total - _lastTotal;

            if (DumpLevelCompleted >= DumpLevelMax)
            {
                NotificationMessage = string.Empty;
                

                if (_total == _lastTotal)
                {
                    _stableCount++;
                }
                else
                {
                    _lastTotal = _total;
                    if (_stableCount != 0) _lastDelta = delta;
                    _stableCount = 0;
                }

                if (_stableCount < 3)
                {
                    StartCoroutine(RetryDelay(10));
                    if (_stableCount == 0)
                    {
                        NotificationMessage = $"Number of translations found is continuing to change ({delta})";
                    }
                    else
                    {
                        NotificationMessage = $"Number of translations unchanged";

                    }


                    NotificationMessage += $", will keep re-dumping until it's stable for {3 - _stableCount} more cycle(s)";
                    DumpLevelCompleted--;
                    DumpLevelReady = DumpLevelCompleted;
                }
            }
            else if (DumpLevelCompleted > 0)
            {
                NotificationMessage =
                    "Multiple brute-force dump attempts are required, please wait until you see a message saying files are available";
            }
        }

        private void HS2_TextDumpAwake(TextDump sender, EventArgs eventArgs)
        {
            SceneManager.sceneLoaded += HS2_sceneLoaded;
        }

        private void HS2_sceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode loadSceneMode)
        {
            if ((IsStudio && scene.name == "Studio" && loadSceneMode == LoadSceneMode.Single) ||
                (!IsStudio && scene.name == "Lobby"))
            {
                _LobbyLoaded = true;
            }
        }


        private IEnumerator RetryDelay(float seconds)
        {
            _waitOnRetry = true;
            yield return new WaitForSeconds(seconds);
            _waitOnRetry = false;
        }

        private IEnumerator HS2_CheckReadyToDump()
        {
            Logger.LogDebug("CheckReadyToDump: waiting until dump 1 completes");
            while (DumpLevelCompleted < 1) yield return new WaitForSeconds(1);

            Logger.LogDebug("CheckReadyToDump: waiting for Lobby to load");

            while (!_LobbyLoaded) yield return new WaitForSeconds(1);

            SceneManager.sceneLoaded -= HS2_sceneLoaded;
           

            Logger.LogDebug("CheckReadyToDump: waiting for lobby to finish loading");
            
            while (Singleton<Manager.Scene>.Instance == null) yield return new WaitForSeconds(1);
            while (Manager.Scene.IsNowLoadingFade) yield return new WaitForSeconds(1);

            Logger.LogDebug("CheckReadyToDump: waiting for Manager.Voice");
           
            while (Voice.infoTable == null || Voice.infoTable.Count == 0) yield return new WaitForSeconds(1);

            Logger.LogDebug($"CheckReadyToDump: waiting for Manager.GameSystem");
            while (Singleton<Manager.GameSystem>.Instance == null) yield return new WaitForSeconds(1);

            Logger.LogFatal($"Language = {Singleton<Manager.GameSystem>.Instance.language}");

            
            foreach (var pth in AssetPathsToWaitOn)
            {
                Logger.LogDebug($"CheckReadyToDump: waiting until we can list asset bundles for {pth}");
                
                while (true)
                {
                    var count = 0;
                    try
                    {
                        count = GetAssetBundleNameListFromPath(pth).Count;
                    }
                    catch
                    {
                        count = 0;
                    }
                    if (count != 0) break;

                    try
                    {
                        count = GetAssetBundleNameListFromPath(pth, true).Count;
                    }
                    catch
                    {
                        count = 0;
                    }
                    if (count != 0) break;

                    yield return new WaitForSeconds(1);
                }
            }

            Logger.LogDebug("CheckReadyToDump: waiting for remaining dumps");
            while (DumpLevelReady < DumpLevelMax)
            {
                if (DumpLevelReady <= DumpLevelCompleted)
                {
                    if (_waitOnRetry) Logger.LogDebug("CheckReadyToDump: waiting for retry delay");
                    while (_waitOnRetry) yield return new WaitForSeconds(1);
                    DumpLevelReady++;
                    Logger.LogDebug($"CheckReadyToDump: level {DumpLevelReady} ready!");
                }

                yield return new WaitForSeconds(1);
            }


        }

    }
}
