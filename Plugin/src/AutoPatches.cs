using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using GameNetcodeStuff;
using System.Linq;
using TMPro;
using UnityEngine;
using Dissonance.Integrations.Unity_NFGO;



namespace AEIOU_Company;

[HarmonyPatch]
public class AutoPatches
{
    private static readonly int NEW_CHAT_SIZE = 8000;
    private static TMP_InputField chatTextField = null;
    private static AudioClip audioClip = null;
    private static string lastChatMessage = "";
    [HarmonyPatch(typeof(HUDManager), "AddPlayerChatMessageClientRpc")]
    [HarmonyPostfix]
    public static async void AddPlayerChatMessageClientRpcPostfix(HUDManager __instance, string chatMessage, int playerId)
    {
        if (lastChatMessage == chatMessage)
        {
            return;
        }
        lastChatMessage = chatMessage;
        Plugin.Log($"AddTextToChatOnServer: {chatMessage} {playerId}");
        PlayerControllerB player = null;
        for (int i = 0; i < __instance.playersManager.allPlayerScripts.Length; i++)
        {
            if (__instance.playersManager.allPlayerScripts[playerId])
            {
                player = __instance.playersManager.allPlayerScripts[playerId];
            }
        }
        if (player == null)
        {
            Plugin.Log("couldnt find player");
            return;
        }

        GameObject AEIOUSpeakObject = player.gameObject.transform.Find("AEIOUSpeakObject")?.gameObject;
        if (AEIOUSpeakObject == null)
        {
            AEIOUSpeakObject = new GameObject("AEIOUSpeakObject");
            AEIOUSpeakObject.transform.parent = player.transform;
            AEIOUSpeakObject.transform.localPosition = Vector3.zero;
            AEIOUSpeakObject.AddComponent<AudioSource>();
        }
        AudioSource audioSource = AEIOUSpeakObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Plugin.LogError($"Couldn't speak, AudioSource was null");
            return;
        }

        float[] samples = await TTS.SpeakToMemory(chatMessage);
        if (audioClip == null)
        {
            audioClip = AudioClip.Create("AEIOUCLIP", samples.Length, 1, 11025, false);
        }
        //audioSource.clip = audioClip;
        audioClip.SetData(samples, 0);

        audioSource.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[player.playerClientId];
        audioSource.volume = 1f;
        audioSource.dopplerLevel = 0f;
        audioSource.pitch = 1f;
        audioSource.spatialize = true;
        audioSource.spatialBlend = 1f;
        if (audioSource.isPlaying)
        {
            audioSource.Stop(true);
        }

        audioSource.PlayOneShot(audioClip, 1f);
        Plugin.Log
        (
            $"Playing audio: {audioSource.ToString()}\n" +
            $"Playing audio: {audioSource.volume.ToString()}\n" +
            $"Playing audio: {audioSource.ignoreListenerVolume.ToString()}\n"
        );
    }

    [HarmonyPatch(typeof(HUDManager), "EnableChat_performed")]
    [HarmonyPostfix]
    public static void EnableChat_performedPostfix(ref TMP_InputField ___chatTextField)
    {
        ___chatTextField.characterLimit = NEW_CHAT_SIZE;
        chatTextField = ___chatTextField;
    }

    [HarmonyPatch(typeof(HUDManager), "SubmitChat_performed")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> SubmitChat_performedTranspiler(IEnumerable<CodeInstruction> oldInstructions)
    {
        List<CodeInstruction> newInstructions = new List<CodeInstruction>(oldInstructions);
        patchMaxChatSize(newInstructions);

        static void patchMaxChatSize(List<CodeInstruction> newInstructions)
        {
            CodeInstruction instructionToChange = null;
            bool foundFirstInstruction = false;
            foreach (CodeInstruction instruction in newInstructions)
            {
                if (instruction.Is(OpCodes.Ldc_I4_S, 50))
                {
                    foundFirstInstruction = true;
                    instructionToChange = instruction;
                    continue;
                }
                if (instruction.opcode == OpCodes.Bge && foundFirstInstruction)
                {
                    instructionToChange.opcode = OpCodes.Ldc_I4;
                    instructionToChange.operand = NEW_CHAT_SIZE; // new max chat length
                    Plugin.Log("Patched max chat length");
                    break;
                }
                else if (foundFirstInstruction) // if current instruction is not what we expected, reset
                {
                    foundFirstInstruction = false;
                    instructionToChange = null;
                }
            }
        }
        return newInstructions.AsEnumerable();
    }

    [HarmonyPatch(typeof(HUDManager), "AddPlayerChatMessageServerRpc")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> AddPlayerChatMessageServerRpcTranspiler(IEnumerable<CodeInstruction> oldInstructions)
    {
        List<CodeInstruction> newInstructions = new List<CodeInstruction>(oldInstructions);
        foreach (CodeInstruction instruction in newInstructions)
        {
            if (instruction.Is(OpCodes.Ldc_I4_S, 0x32))
            {
                instruction.opcode = OpCodes.Ldc_I4;
                instruction.operand = NEW_CHAT_SIZE;
                Plugin.Log("Patched server max chat length");
                break;
            }
        }
        return newInstructions.AsEnumerable();
    }
    [HarmonyPatch(typeof(HUDManager), "EnableChat_performed")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> EnableChat_performedTranspiler(IEnumerable<CodeInstruction> oldInstructions)
    {
        List<CodeInstruction> newInstructions = new List<CodeInstruction>(oldInstructions);
        for (int i = 0; i < newInstructions.Count - 3; i++)
        {
            if (!(newInstructions[i].opcode == OpCodes.Ldarg_0))
            {
                continue;
            }
            if (!newInstructions[i + 1].Is(OpCodes.Ldfld, AccessTools.Field(typeof(HUDManager), "localPlayer")))
            {
                continue;
            }
            if (!newInstructions[i + 2].Is(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerControllerB), "isPlayerDead")))
            {
                continue;
            }
            if (!(newInstructions[i + 3].opcode == OpCodes.Brfalse))
            {
                Plugin.Log($"{newInstructions[i + 3].opcode} {newInstructions[i + 3].operand}");
                continue;
            }
            Plugin.Log("Patching dead chat");
            newInstructions[i].opcode = OpCodes.Br;
            newInstructions[i].operand = newInstructions[i + 3].operand;
            newInstructions.RemoveRange(i + 1, 3);
        }
        return newInstructions.AsEnumerable();
    }
}