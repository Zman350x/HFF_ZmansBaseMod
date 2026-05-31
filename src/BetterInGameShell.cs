using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace ZmanBase
{
    using UnityEngine;
    using HarmonyLib;
    using TMPro;

    internal static class BetterInGameShell
    {
        public static bool IsEnabled { get; private set; } = false;
        public static List<string> CommandHistory { get; private set; } = new List<string>();

        private static int historyIndex = 0;
        private static Dictionary<int, string> partialCommands = new Dictionary<int, string>();
        private static int idealCursorPosition = 0;
        private static int historyMovedCursorPosition = 0;

        private static FieldInfo caretPosition, caretSelectPosition, stringPosition, stringSelectPosition;

        // Apply shell modifications
        public static void Start()
        {
            if (IsEnabled)
                return;

            caretPosition = AccessTools.DeclaredField(typeof(TMP_InputField), "m_CaretPosition");
            caretSelectPosition = AccessTools.DeclaredField(typeof(TMP_InputField), "m_CaretSelectPosition");
            stringPosition = AccessTools.DeclaredField(typeof(TMP_InputField), "m_StringPosition");
            stringSelectPosition = AccessTools.DeclaredField(typeof(TMP_InputField), "m_StringSelectPosition");

            Harmony.CreateAndPatchAll(typeof(BetterInGameShell), "BetterInGameShell");
            IsEnabled = true;
        }

        // Reset shell to default state
        public static void Stop()
        {
            Harmony.UnpatchID("BetterInGameShell");
            IsEnabled = false;
        }

        private static string SaveCommand(string command)
        {
            historyIndex = CommandHistory.Count;
            partialCommands.Clear();
            idealCursorPosition = int.MaxValue;

            if (string.IsNullOrEmpty(command))
                return command;

            if (char.IsWhiteSpace(command, 0))
                return command.TrimStart();

            if (CommandHistory.Count == 0 || command != CommandHistory[CommandHistory.Count - 1])
            {
                CommandHistory.Add(command);
                ++historyIndex;
            }
            return command;
        }

        private static void ProcessKeystrokes(TMP_InputField input)
        {
            if (input.selectionAnchorPosition != historyMovedCursorPosition)
                idealCursorPosition = input.selectionAnchorPosition;

            historyMovedCursorPosition = input.caretPosition;

            if (Game.GetKeyDown(KeyCode.UpArrow))
            {
                if (historyIndex > 0)
                {
                    moveInHistory(input, -1);
                }
            }
            if (Game.GetKeyDown(KeyCode.DownArrow))
            {
                if (historyIndex < CommandHistory.Count)
                {
                    moveInHistory(input, 1);
                }
            }
        }

        private static void moveInHistory(TMP_InputField input, int incAmt)
        {
                if (historyIndex == CommandHistory.Count || input.text != CommandHistory[historyIndex])
                    partialCommands[historyIndex] = input.text;

                historyIndex += incAmt;

                string partialCommand;
                if (partialCommands.TryGetValue(historyIndex, out partialCommand))
                    input.text = partialCommand;
                else
                    input.text = CommandHistory[historyIndex];

                historyMovedCursorPosition = idealCursorPosition.Clamp(0, input.text.Length);
                caretPosition.SetValue(input, historyMovedCursorPosition);
                caretSelectPosition.SetValue(input, historyMovedCursorPosition);
                stringPosition.SetValue(input, historyMovedCursorPosition);
                stringSelectPosition.SetValue(input, historyMovedCursorPosition);
        }

        // Patch the Shell such that it doesn't force the input to be lowercase or trim off the whitespace
        // Add support for command history
        [HarmonyPatch(typeof(Shell), "Update")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> ShellUpdateTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions);

            codeMatcher.MatchStartForward(
                    new CodeMatch(OpCodes.Ldc_I4_S, (SByte) KeyCode.Return),
                    new CodeMatch(OpCodes.Call, typeof(Game).GetMethod("GetKeyDown", new Type[] { typeof(KeyCode) }))
                )
                .SetAndAdvance(OpCodes.Ldarg_0, null) // For labeling purposes
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldfld, typeof(Shell).GetField("input")),
                    CodeInstruction.Call(typeof(BetterInGameShell), "ProcessKeystrokes", new Type[] { typeof(TMP_InputField) }),
                    new CodeInstruction(OpCodes.Ldc_I4_S, (SByte) KeyCode.Return) // What we replaced earlier for labeling purposes
                );

            codeMatcher.MatchStartForward(
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(String), "Trim"))
                ).RemoveInstruction();

            codeMatcher.MatchStartForward(
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(String), "ToLowerInvariant"))
                ).SetInstructionAndAdvance(
                    CodeInstruction.Call(typeof(BetterInGameShell), "SaveCommand", new Type[] { typeof(string) })
                );

            return codeMatcher.Instructions();
        }

        // Patch the Shell such that directly invoking commands doesn't force them to be lowercase
        // To the best of my knowledge, nothing in the game actually calls RawInvoke, however it doesn't hurt patch it
        [HarmonyPatch(typeof(Shell), "RawInvoke")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> ShellRawInvokeTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions);

            codeMatcher.MatchStartForward(
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(String), "ToLowerInvariant"))
                ).RemoveInstruction();

            return codeMatcher.Instructions();
        }

        // Even if the Shell doesn't alter the commands, the CommandRegistry still tries to impose the same restrictions. It's pretty redundant
        // However, that means we have to apply the same patches to this method as well
        [HarmonyPatch(typeof(CommandRegistry), "Execute")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> CommandRegistryExecuteTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions);
            codeMatcher.MatchStartForward(
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(String), "ToLowerInvariant"))
                ).Repeat(matchAction: cm => { cm.RemoveInstruction(); });

            codeMatcher.Start();

            codeMatcher.MatchStartForward(
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(String), "Trim"))
                ).RemoveInstruction();

            return codeMatcher.Instructions();
        }

        [HarmonyPatch(typeof(TMP_InputField), "MoveUp", new Type[] { typeof(bool) })]
        [HarmonyPatch(typeof(TMP_InputField), "MoveDown", new Type[] { typeof(bool) })]
        [HarmonyPrefix]
        private static void MoveUpDown(ref bool __runOriginal)
        {
            // Disable the move up/down functions (specifically the variant that only takes one bool) to disable the up & down arrow default behavior
            __runOriginal = false;
        }
    }
}
