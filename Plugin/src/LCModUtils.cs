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
    private static bool _shouldHost = false;
    private static bool _shouldJoin = false;
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
    public void StartLANHost()
    {
        BootToLANMenu();
        _shouldHost = true;
    }
    public void StartLANClient()
    {
        BootToLANMenu();
        _shouldJoin = true;
    }



    private static bool SetFullScreenModePrefix()
    {
        return false;
    }
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        MenuManager menuManager = UnityEngine.GameObject.FindObjectOfType<MenuManager>();
        if (scene.name == "MainMenu")
        {
            Plugin.Log("MainMenuLoaded");
            GameObject LANWarning = menuManager.lanWarningContainer;
            if (LANWarning)
            {
                Plugin.Log("Destroy LAN Warning");
                UnityEngine.GameObject.Destroy(LANWarning);
            }
            else
            {
                Plugin.Log("LANWarning Null");
            }

            if (_shouldHost || _shouldJoin)
            {
                MethodInfo startMethodInfo = AccessTools.Method(typeof(MenuManager), "Start");
                if (startMethodInfo == null)
                {
                    Plugin.LogError("Couldn't find method \"Start\" in MenuManager");
                }
                MethodInfo postfix = SymbolExtensions.GetMethodInfo(() => HostOrJoin());
                _harmony.Patch(startMethodInfo, postfix: new HarmonyMethod(postfix));
            }
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
    private static void HostOrJoin()
    {
        MenuManager menuManager = UnityEngine.GameObject.FindObjectOfType<MenuManager>();
        if (_shouldHost)
        {
            menuManager?.ClickHostButton();
            menuManager?.ConfirmHostButton();
        }
        else if (_shouldJoin)
        {
            MethodInfo ClickJoinButton = AccessTools.Method(typeof(MenuManager), "ClickJoinButton");
            ClickJoinButton?.Invoke(menuManager, null);
        }
    }
}