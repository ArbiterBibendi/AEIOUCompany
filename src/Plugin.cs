﻿using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace LCMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public Harmony Harmony = null;
        protected static new ManualLogSource Logger = null;
        public static void Log(object data)
        {
            Logger.LogInfo(data);
        }
        public static void LogError(object data)
        {
            Logger.LogError(data);
        }

        private void Awake()
        {
            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            Harmony = harmony;
            Logger = base.Logger;

            base.Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Harmony.PatchAll();
            base.Logger.LogInfo($"Plugin total patches appled: {Harmony.GetPatchedMethods().Count()}");
        }
        public void OnDestroy()
        {
            tweaksForTesting();
        }
        private void tweaksForTesting()
        {
            LCModUtils modUtils = new LCModUtils(base.Logger, Harmony);
            modUtils.DisableFullscreen();
            modUtils.BootToLANMenu();
        }
    }
}