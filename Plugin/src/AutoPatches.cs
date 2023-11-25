using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using GameNetcodeStuff;
using System.Linq;
using TMPro;



namespace AEIOU_Company;

[HarmonyPatch]
public class AutoPatches
{
    private static readonly int NEW_CHAT_SIZE = 8000;
    private static TMP_InputField chatTextField = null;
    [HarmonyPatch(typeof(HUDManager), "AddTextToChatOnServer")]
    [HarmonyPrefix]
    public static void AddTextToChatOnServerPostfix(string chatMessage, int playerId)
    {
        Plugin.Log($"AddTextToChatOnServer: {chatMessage} {playerId}");
        TTS.Speak(chatMessage);
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
                    Plugin.Log("instructionToChange = instruction");
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