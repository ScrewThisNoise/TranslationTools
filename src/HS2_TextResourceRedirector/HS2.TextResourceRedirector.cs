﻿using System;
using BepInEx;

namespace IllusionMods
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextResourceRedirector
    {
        public const string PluginNameInternal = "HS2_TextResourceRedirector";

        public TextResourceRedirector()
        {
            //TextResourceExtensions.EnableTraces = true;
            TextResourceRedirectorAwake += ConfigureHandlersForHS2;
        }

        public VoiceInfoHandler VoiceInfoHandler { get; private set; }
        public BGMNameInfoHandler BGMNameInfoHandler { get; private set; }
        public EventContentInfoDataHandler EventContentInfoDataHandler { get; private set; }
        public MapInfoHandler MapInfoHandler { get; private set; }
        public ParameterNameInfoHandler ParameterNameInfoHandler { get; private set; }
        public AchievementInfoDataHandler AchievementInfoDataHandler { get; private set; }
        public object PlanNameInfoHandler { get; private set; }

        private TextResourceHelper GetTextResourceHelper()
        {
            return CreateHelper<HS2_TextResourceHelper>();
        }

        private void ConfigureHandlersForHS2(TextResourceRedirector sender, EventArgs eventArgs)
        {
            sender.VoiceInfoHandler = new VoiceInfoHandler(sender);
            sender.BGMNameInfoHandler = new BGMNameInfoHandler(sender);
            sender.EventContentInfoDataHandler = new EventContentInfoDataHandler(sender);
            sender.MapInfoHandler = new MapInfoHandler(sender);
            sender.ParameterNameInfoHandler = new ParameterNameInfoHandler(sender);
            sender.AchievementInfoDataHandler = new AchievementInfoDataHandler(sender);
            sender.ChaListDataHandler.WhiteListPaths.Add("abdata/list/characustom");

            if (TextResourceHelper is HS2_TextResourceHelper helper && helper.IsHS2DX())
            {
                // DX only
                sender.PlanNameInfoHandler = new PlanNameInfoHandler(sender);
            }
        }
    }
}
