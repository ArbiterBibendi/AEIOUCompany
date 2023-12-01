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

namespace AEIOU_Company;
public class LCModUtils
{
    private static Harmony _harmony = null;
    public LCModUtils(Harmony harmony)
    {
        _harmony = harmony;
    }
    public void DisableFullscreen()
    {
        MethodInfo SetFullscreenMode = AccessTools.Method(typeof(IngamePlayerSettings), "SetFullscreenMode");
        if (SetFullscreenMode == null)
        {
            Plugin.LogError($"Couldn't find method {nameof(SetFullscreenMode)}");
        }
        MethodInfo SetFullScreenModePrefixInfo = SymbolExtensions.GetMethodInfo(() => SetFullScreenModePrefix());
        _harmony.Patch(SetFullscreenMode, prefix: new HarmonyMethod(SetFullScreenModePrefixInfo));
        Plugin.Log("Disabled Fullscreen");
    }
    public void BootToLANMenu()
    {
        Plugin.Log("Attempting to load lan scene");
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
            Plugin.Log("MainMenuLoaded");
            GameObject LANWarning = GameObject.FindAnyObjectByType<MenuManager>().lanWarningContainer;
            if (LANWarning)
            {
                Plugin.Log("Destroy LAN Warning");
                LANWarning.SetActive(false);
            }
            else
            {
                Plugin.Log("LANWarning Null");
            }
        }
    }
}