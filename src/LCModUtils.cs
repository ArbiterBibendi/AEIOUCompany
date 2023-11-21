using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace LCMod;
public class LCModUtils
{
    private static ManualLogSource _logger = null;
    private static Harmony _harmony = null;
    public LCModUtils(ManualLogSource logger, Harmony harmony)
    {
        _logger = logger;
        _harmony = harmony;
    }
    public void DisableFullscreen()
    {
        MethodInfo SetFullscreenMode = AccessTools.Method(typeof(IngamePlayerSettings), "SetFullscreenMode");
        if (SetFullscreenMode == null)
        {
            _logger.LogError($"Couldn't find method {nameof(SetFullscreenMode)}");
        }
        MethodInfo SetFullScreenModePrefixInfo = SymbolExtensions.GetMethodInfo(() => SetFullScreenModePrefix());
        _harmony.Patch(SetFullscreenMode, prefix: new HarmonyMethod(SetFullScreenModePrefixInfo));
        _logger.LogInfo("Disabled Fullscreen");
    }
    public void BootToLANMenu()
    {
        _logger.LogInfo("Attempting to load lan scene");
        SceneManager.sceneLoaded += OnSceneLoaded; // listen for main menu load, destroy lan warning
        UnityEngine.SceneManagement.SceneManager.LoadScene("InitSceneLANMode");
    }

    
    private static bool SetFullScreenModePrefix()
    {
        return false;
    }
    
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            _logger.LogInfo("MainMenuLoaded");
            GameObject LANWarning = GameObject.FindAnyObjectByType<MenuManager>().lanWarningContainer;
            if (LANWarning)
            {
                _logger.LogInfo("Destroy LAN Warning");
                GameObject.Destroy(LANWarning);
            }
            else
            {
                _logger.LogInfo("LANWarning Null");
            }
        }
    }
}