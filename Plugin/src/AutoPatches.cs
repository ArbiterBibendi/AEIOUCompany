using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using GameNetcodeStuff;
using System.Linq;
using TMPro;
using UnityEngine;
using Dissonance.Integrations.Unity_NFGO;
using System.Collections;



namespace AEIOU_Company;

[HarmonyPatch]
public class AutoPatches
{
    private static readonly int NEW_CHAT_SIZE = 1024;
    private static TMP_InputField chatTextField = null;
    private static string lastChatMessage = "";
    private static readonly float[] emptySamples = new float[TTS.IN_BUFFER_SIZE];
    [HarmonyPatch(typeof(HUDManager), "AddPlayerChatMessageClientRpc")]
    [HarmonyPostfix]
    public static void AddPlayerChatMessageClientRpcPostfix(HUDManager __instance, string chatMessage, int playerId)
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
            AEIOUSpeakObject.AddComponent<AudioHighPassFilter>();
            AEIOUSpeakObject.AddComponent<AudioLowPassFilter>();
        }
        AudioSource audioSource = AEIOUSpeakObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Plugin.LogError($"Couldn't speak, AudioSource was null");
            return;
        }
        float[] samples = TTS.SpeakToMemory(chatMessage, 7.5f);
        if (audioSource.clip == null)
        {
            audioSource.clip = AudioClip.Create("AEIOUCLIP", TTS.IN_BUFFER_SIZE, 1, 11025, false);
        }

        audioSource.clip.SetData(emptySamples, 0);
        audioSource.clip.SetData(samples, 0);

        audioSource.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[player.playerClientId];
        audioSource.volume = Plugin.TTSVolume;
        if (Vector3.Distance(player.transform.position, StartOfRound.Instance.localPlayerController.transform.position) > 50f)
        {
            audioSource.volume = 0f;
        }
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.dopplerLevel = Plugin.TTSDopperLevel;
        audioSource.pitch = 1f;
        audioSource.spatialize = true;
        audioSource.spatialBlend = 1f;

        AudioHighPassFilter highPassFilter = AEIOUSpeakObject.GetComponent<AudioHighPassFilter>();
        if (highPassFilter != null)
        {
            highPassFilter.enabled = false;
        }

        AudioLowPassFilter lowPassFilter = AEIOUSpeakObject.GetComponent<AudioLowPassFilter>();
        if (lowPassFilter != null)
        {
            Plugin.Log("AudioLowPassFilter not null!");
            lowPassFilter.lowpassResonanceQ = 1;
        }

        if (audioSource.isPlaying)
        {
            audioSource.Stop(true);
        }

        Plugin.Log
        (
            $"Playing audio: {audioSource.ToString()}\n" +
            $"Playing audio: {audioSource.volume.ToString()}\n" +
            $"Playing audio: {audioSource.ignoreListenerVolume.ToString()}\n" +
            $"Playing audio: {audioSource.clip.ToString()}\n"
        );
        if (player.holdingWalkieTalkie)
        {
            Plugin.Log("WalkieTalkie");
            WalkieTalkie walkieTalkie = (WalkieTalkie)player.currentlyHeldObjectServer;
            if (walkieTalkie == null || !walkieTalkie.isBeingUsed)
            {
                return;
            }
            audioSource.volume = Plugin.TTSVolume;
            if (player == StartOfRound.Instance.localPlayerController)
            {
                Plugin.Log("Pushing walkie button");
                player.playerBodyAnimator.SetBool("walkieTalkie", true);
                walkieTalkie.StartCoroutine(WaitAndStopUsingWalkieTalkie(audioSource.clip, player));
            }
            else
            {
                highPassFilter.enabled = true;
                lowPassFilter.lowpassResonanceQ = 3000;
                audioSource.spatialBlend = 0f;
            }
        }
        audioSource.PlayOneShot(audioSource.clip, 1f);
    }
    public static IEnumerator WaitAndStopUsingWalkieTalkie(AudioClip clip, PlayerControllerB player)
    {
        Plugin.Log($"WalkieButton Length {TTS.CurrentAudioLengthInSeconds}");
        yield return new WaitForSeconds(TTS.CurrentAudioLengthInSeconds);
        Plugin.Log($"WalkieButton end");
        player.playerBodyAnimator.SetBool("walkieTalkie", false);
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
}