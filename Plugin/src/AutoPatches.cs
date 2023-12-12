using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using GameNetcodeStuff;
using System.Linq;
using TMPro;
using UnityEngine;
using Dissonance.Integrations.Unity_NFGO;
using System.Collections;
using System.Threading.Tasks;
using System;
using System.Reflection;



namespace AEIOU_Company;

[HarmonyPatch]
public class Patches
{
    private static int NEW_CHAT_SIZE = Plugin.ChatSize;
    private static TMP_InputField chatTextField = null;
    private static string lastChatMessage = "";
    private static readonly float[] emptySamples = new float[TTS.IN_BUFFER_SIZE];
    private static List<Speak> _speaks = new List<Speak>();
    private static bool _isProcessing = false;

    [HarmonyPatch(typeof(HUDManager), "AddPlayerChatMessageClientRpc")]
    [HarmonyPostfix]
    public static void AddPlayerChatMessageClientRpcPostfix(HUDManager __instance, string chatMessage, int playerId)
    {
        if (lastChatMessage == chatMessage)
        {
            return;
        }
        lastChatMessage = chatMessage;
        bool walkieTalkieTextChat = GameNetworkManager.Instance.localPlayerController.holdingWalkieTalkie && StartOfRound.Instance.allPlayerScripts[playerId].holdingWalkieTalkie;
        float distanceToPlayer = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, HUDManager.Instance.playersManager.allPlayerScripts[playerId].transform.position);
		if (distanceToPlayer > 25f && !walkieTalkieTextChat && GameNetworkManager.Instance.localPlayerController.isPlayerDead)
		{
			MethodInfo AddChatMessage = (MethodInfo)AccessTools.Method(typeof(HUDManager), "AddChatMessage");
            AddChatMessage.Invoke(HUDManager.Instance, new object[]{chatMessage, HUDManager.Instance.playersManager.allPlayerScripts[playerId].playerUsername});
		}

        Plugin.Log($"AddTextToChatOnServer: {chatMessage} {playerId}");
        QueueSpeak(__instance, chatMessage, playerId);

        if (_isProcessing)
        {
            return;
        }
        _isProcessing = true;
        Task.Run(() =>
        {
            while (_speaks.Count > 0)
            {
                Speak(__instance, _speaks[0].ChatMessage, _speaks[0].PlayerId);
                _speaks.RemoveAt(0);
            }
            _isProcessing = false;
        });
    }
    public static void Speak(HUDManager __instance, string chatMessage, int playerId)
    {
        Plugin.Log("Speak");
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
        string filteredChatMessage = chatMessage.Replace("\r", "").Replace("\n", "");
        float[] samples = TTS.SpeakToMemory(filteredChatMessage, 7.5f);
        if (audioSource.clip == null)
        {
            audioSource.clip = AudioClip.Create("AEIOUCLIP", TTS.IN_BUFFER_SIZE, 1, 11025, false);
        }

        audioSource.clip.SetData(emptySamples, 0);
        audioSource.clip.SetData(samples, 0);

        audioSource.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[player.playerClientId];

        audioSource.rolloffMode = AudioRolloffMode.Custom;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 40f;
        audioSource.dopplerLevel = Plugin.TTSDopperLevel;
        audioSource.pitch = 1f;
        audioSource.spatialize = true;
        audioSource.spatialBlend = player.isPlayerDead ? 0f : 1f;
        bool playerHasDeathPermissions = !player.isPlayerDead || StartOfRound.Instance.localPlayerController.isPlayerDead;
        audioSource.volume = playerHasDeathPermissions ? Plugin.TTSVolume : 0;

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
            lowPassFilter.cutoffFrequency = 5000;
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
        if (player.holdingWalkieTalkie && player.currentlyHeldObjectServer is WalkieTalkie walkieTalkie)
        {
            Plugin.Log("WalkieTalkie");
            bool localPlayerIsUsingWalkieTalkie = false;
            for (int i = 0; i < WalkieTalkie.allWalkieTalkies.Count; i++)
            {
                if
                (
                    WalkieTalkie.allWalkieTalkies[i].playerHeldBy == StartOfRound.Instance.localPlayerController
                    && WalkieTalkie.allWalkieTalkies[i].isBeingUsed
                )
                {
                    localPlayerIsUsingWalkieTalkie = true;
                }
            }
            if
            (
                walkieTalkie != null
                && walkieTalkie.isBeingUsed
                && localPlayerIsUsingWalkieTalkie
            )
            {
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
                    lowPassFilter.lowpassResonanceQ = 3f;
                    lowPassFilter.cutoffFrequency = 4000;
                    audioSource.spatialBlend = 0f;
                }
            }
        }
        audioSource.PlayOneShot(audioSource.clip, 1f);
        RoundManager.Instance.PlayAudibleNoise(AEIOUSpeakObject.transform.position, 25f, 0.7f);
    }
    public static void QueueSpeak(HUDManager __instance, string chatMessage, int playerId)
    {
        Plugin.Log("Queueing speak");
        _speaks.Add(new Speak(chatMessage, playerId));
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
    public static void EnableChat_performedPostfix(ref TMP_InputField ___chatTextField, HUDManager __instance)
    {
        ___chatTextField.characterLimit = NEW_CHAT_SIZE;
        chatTextField = ___chatTextField;
        Plugin.Log("Enable Chat");
    }
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer))]
    [HarmonyPostfix]
    public static void KillPlayerPostfix(PlayerControllerB __instance)
    {
        HUDManager.Instance.HideHUD(false);
        HUDManager.Instance.UpdateHealthUI(100, false);
        Plugin.Log("Player died, re-enabling UI");
    }

    [HarmonyPatch(typeof(HUDManager), "EnableChat_performed")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> EnableChat_performedTranspiler(IEnumerable<CodeInstruction> oldInstructions)
    {
        List<CodeInstruction> newInstructions = new List<CodeInstruction>(oldInstructions);
        for (int i = 0; i < newInstructions.Count - 3; i++)
        {
            if
            (
                newInstructions[i].opcode == OpCodes.Ldarg_0 &&
                newInstructions[i + 1].Is(OpCodes.Ldfld, AccessTools.Field(typeof(HUDManager), "localPlayer")) &&
                newInstructions[i + 2].Is(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerControllerB), "isPlayerDead")) &&
                newInstructions[i + 3].opcode == OpCodes.Brfalse
            )
            {
                Plugin.Log("Patching dead chat in EnableChat_performed");
                newInstructions[i].opcode = OpCodes.Br;
                newInstructions[i].operand = newInstructions[i + 3].operand;
                break;
            }
        }
        return newInstructions.AsEnumerable();
    }

    [HarmonyPatch(typeof(HUDManager), "SubmitChat_performed")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> SubmitChat_performedTranspiler(IEnumerable<CodeInstruction> oldInstructions)
    {
        List<CodeInstruction> newInstructions = new List<CodeInstruction>(oldInstructions);
        patchMaxChatSize(newInstructions);
        patchDeadChat(newInstructions);

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
                    instructionToChange.operand = NEW_CHAT_SIZE + 1; // new max chat length
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
        static void patchDeadChat(List<CodeInstruction> newInstructions)
        {
            for (int i = 0; i < newInstructions.Count - 3; i++)
            {
                if
                (
                    newInstructions[i].opcode == OpCodes.Ldarg_0 &&
                    newInstructions[i + 1].Is(OpCodes.Ldfld, AccessTools.Field(typeof(HUDManager), "localPlayer")) &&
                    newInstructions[i + 2].Is(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerControllerB), "isPlayerDead")) &&
                    newInstructions[i + 3].opcode == OpCodes.Brfalse
                )
                {
                    Plugin.Log("Patching dead chat in SubmitChat_performed");
                    newInstructions[i].opcode = OpCodes.Br;
                    newInstructions[i].operand = newInstructions[i + 3].operand;
                    break;
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