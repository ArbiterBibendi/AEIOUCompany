using HarmonyLib;
using GameNetcodeStuff;



namespace AEIOU_Company;

[HarmonyPatch]
public class AutoPatches
{
    [HarmonyPatch(typeof(HUDManager), "AddTextToChatOnServer")]
    [HarmonyPrefix]
    public static void AddTextToChatOnServerPostfix(string chatMessage, int playerId)
    {
        Plugin.Log($"AddTextToChatOnServer: {chatMessage} {playerId}");
        TTS.Speak(chatMessage);
    }
    [HarmonyPatch(typeof(HUDManager), "AddChatMessage")]
    [HarmonyPrefix]
    public static void AddChatMessagePostfix(string chatMessage, string nameOfUserWhoTyped)
    {
        Plugin.Log($"AddChatMessage: {chatMessage} {nameOfUserWhoTyped}");
    }
    [HarmonyPatch(typeof(HUDManager), "AddPlayerChatMessageClientRpc")]
    [HarmonyPrefix]
    public static void AddPlayerChatMessageClientRpcPostfix(string chatMessage, int playerId)
    {
        Plugin.Log($"AddPlayerChatMessageClientRpc: {chatMessage} {playerId}");
    }
    [HarmonyPatch(typeof(HUDManager), "AddPlayerChatMessageServerRpc")]
    [HarmonyPrefix]
    public static void AddPlayerChatMessageServerRpcPostfix(string chatMessage, int playerId)
    {
        Plugin.Log($"AddPlayerChatMessageServerRpc: {chatMessage} {playerId}");
    }
}