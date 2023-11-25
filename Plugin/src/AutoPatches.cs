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
    [HarmonyPatch(typeof(HUDManager), "AddTextToChatOnServer")]
    [HarmonyPrefix]
    public static void AddTextToChatOnServerPostfix(HUDManager __instance, string chatMessage, int playerId)
    {
        Plugin.Log($"AddTextToChatOnServer: {chatMessage} {playerId}");
        TTS.Speak(chatMessage);
        PlayerControllerB player = null;
        for (int i = 0; i < __instance.playersManager.allPlayerScripts.Length; i++)
        {
            if ((int)__instance.playersManager.allPlayerScripts[i].playerClientId == playerId)
            {
                player = __instance.playersManager.allPlayerScripts[i];
                if (player.currentVoiceChatAudioSource)
                {
                    Plugin.Log("audio source null");
                    return;
                }
            }
        }
        //if (player == null)
        //{
        //    Plugin.Log("couldnt find player");
        //    return;
        //}
        //AudioSource audioSource = player.gameObject.GetComponentInChildren<AudioSource>();
        //if (audioSource == null)
        //{
        //    return;
        //}
        //audioSource.clip = AudioClip.Create("AEIOUCLIP", 11025, 1, 1000, false);
        //audioSource.clip.SetData(TTS.SpeakToMemory(chatMessage), 0);
        //audioSource.clip.LoadAudioData();
        //audioSource.Play();
        //Plugin.Log($"Playing audio: {audioSource.ToString()}");
    }

    [HarmonyPatch(typeof(HUDManager), "EnableChat_performed")]
    [HarmonyPostfix]
    public static void EnableChat_performedPostfix(ref TMP_InputField ___chatTextField)
    {
        ___chatTextField.characterLimit = NEW_CHAT_SIZE;
        chatTextField = ___chatTextField;
    }
    [HarmonyPatch(typeof(HUDManager), "SubmitChat_performed")]
    [HarmonyPrefix]
    public static void SubmitChat_performedPrefix()
    {
        Plugin.Log($"SubmitChat_performed: {chatTextField.text.Length}");
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
                    Plugin.Log("Patched max chat size");
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
}