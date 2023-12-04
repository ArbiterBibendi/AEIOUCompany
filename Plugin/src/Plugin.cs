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
        public static Harmony Harmony = null;
        protected static new ManualLogSource Logger = null;
        public static bool PlayStartingUpMessage = false;
        public static float TTSVolume = 0f;
        public static float TTSDopperLevel;

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

            PlayStartingUpMessage = Config.Bind<bool>("General", "StartingUpMessage", true, "Enables \"starting up\" sound effect.").Value;
            TTSVolume = Config.Bind<float>("General", "Volume", 1f, "Volume scale of text-to-speech-voice. Values range from 0 to 1").Value;
            TTSDopperLevel = Config.Bind<float>("General", "Doppler Effect Level", 1f, "Values range from 0 to 1").Value;

            TTS.Init();
            base.Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Harmony.PatchAll();
            base.Logger.LogInfo($"Plugin total patches appled: {Harmony.GetPatchedMethods().Count()}");
        }
        public void OnDestroy()
        {
            EnableTestMode();
            if (PlayStartingUpMessage)
            {
                TTS.Speak("Starting Up");
            }
        }
        private void EnableTestMode()
        {
            LCModUtils modUtils = new LCModUtils(Harmony);
            modUtils.DisableFullscreen();
            modUtils.BootToLANMenu();
        }
    }
}
