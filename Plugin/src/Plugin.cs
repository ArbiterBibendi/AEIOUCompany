using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace AEIOU_Company
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public Harmony Harmony = null;
        protected static new ManualLogSource Logger = null;
        public static void Log(object data)
        {
            Logger?.LogInfo(data);
        }
        public static void LogError(object data)
        {
            Logger?.LogError(data);
        }

        public void Awake()
        {
            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            Harmony = harmony;
            Logger = base.Logger;

            TTS.Init();
            base.Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Harmony.PatchAll();
            base.Logger.LogInfo($"Plugin total patches appled: {Harmony.GetPatchedMethods().Count()}");
        }
        public void OnDestroy()
        {
            TweaksForTesting();
            TTS.Speak("Starting Up");
            TTS.SpeakToMemory("monday");
        }
        private void TweaksForTesting()
        {
            LCModUtils modUtils = new LCModUtils(base.Logger, Harmony);
            modUtils.DisableFullscreen();
            modUtils.BootToLANMenu();
        }
    }
}
