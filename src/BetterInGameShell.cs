using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace ZmanBase
{
    using UnityEngine;
    using UnityEngine.UI;
    using HarmonyLib;
    using TMPro;
    using InControl;

    internal static class BetterInGameShell
    {
        private struct ScrollbackPosition
        {
            public bool bottom;
            public Vector2 offset;
        }

        public static bool IsEnabled { get; private set; } = false;
        public static List<string> CommandHistory { get; private set; } = new List<string>();

        public static Int32 maxScrollbackLines = 500;

        private static int historyIndex = 0;
        private static Dictionary<int, string> partialCommands = new Dictionary<int, string>();
        private static int idealCursorPosition = 0;
        private static int historyMovedCursorPosition = 0;

        private static FieldInfo caretPosition, caretSelectPosition, stringPosition, stringSelectPosition;
        
        private static TMP_InputField inputField;

        private static GameObject shellTextObject;
        private static RectTransform shellTextRect;
        private static TextMeshProUGUI shellText;

        private static GameObject textBoxObject;
        private static RectTransform textBoxRect;
        private static ScrollRect scrollRect;

        private static KeyCombo lastFrameKeyCombo = default(KeyCombo);

        // Apply shell modifications
        public static void Start()
        {
            if (IsEnabled)
                return;

            caretPosition = AccessTools.DeclaredField(typeof(TMP_InputField), "m_CaretPosition");
            caretSelectPosition = AccessTools.DeclaredField(typeof(TMP_InputField), "m_CaretSelectPosition");
            stringPosition = AccessTools.DeclaredField(typeof(TMP_InputField), "m_StringPosition");
            stringSelectPosition = AccessTools.DeclaredField(typeof(TMP_InputField), "m_StringSelectPosition");

            inputField = GameObject.Find("Game(Clone)/Menu/Shell/Background/TextMeshPro - InputField").GetComponent<TMP_InputField>();

            Harmony.CreateAndPatchAll(typeof(BetterInGameShell), "BetterInGameShell");
            ApplyScrollbackChanges();
            IsEnabled = true;
        }

        // Reset shell to default state
        public static void Stop()
        {
            if (!IsEnabled)
                return;

            Harmony.UnpatchID("BetterInGameShell");
            RevertScrollbackChanges();
            IsEnabled = false;
        }

        private static void ApplyScrollbackChanges()
        {
            shellTextObject = GameObject.Find("Game(Clone)/Menu/Shell/Background/TextMeshPro Text");
            shellTextRect = shellTextObject.transform as RectTransform;

            textBoxObject = new GameObject("TextBox", typeof(RectTransform),
                                                      typeof(CanvasRenderer),
                                                      typeof(RectMask2D),
                                                      typeof(ScrollRect));
            textBoxObject.layer = LayerMask.NameToLayer("UI");

            textBoxRect = textBoxObject.transform as RectTransform;
            textBoxRect.SetParent(shellTextRect.parent);
            HelperFunctions.ResetRectTransform(textBoxRect);
            textBoxRect.pivot = new Vector2(0.5f, 1.0f);
            textBoxRect.offsetMax = new Vector2(-10.0f, 0.0f);
            textBoxRect.offsetMin = new Vector2(10.0f, 50.0f);

            scrollRect = textBoxObject.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.content = shellTextRect;
            scrollRect.decelerationRate = 0.01f;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 10;
            scrollRect.elasticity = 0.03f;
            scrollRect.viewport = textBoxRect;

            shellTextRect.SetParent(textBoxRect);
            shellTextRect.pivot = new Vector2(0.5f, 0.0f);
            shellTextRect.offsetMax = new Vector2(0.0f, 0.0f);
            shellTextRect.offsetMin = new Vector2(0.0f, 0.0f);
            shellTextObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            shellText = shellTextObject.GetComponent<TextMeshProUGUI>();
            shellText.alignment = TextAlignmentOptions.TopLeft;
        }

        private static void RevertScrollbackChanges()
        {
            shellTextRect.SetParent(textBoxRect.parent);
            shellTextRect.pivot = new Vector2(0.5f, 0.5f);
            shellTextRect.offsetMax = new Vector2(-10.0f, -10.0f);
            shellTextRect.offsetMin = new Vector2(10.0f, 50.0f);
            Component.Destroy(shellTextObject.GetComponent<ContentSizeFitter>());
            shellText.alignment = TextAlignmentOptions.BottomLeft;

            GameObject.Destroy(textBoxObject);
            textBoxObject = null;
            textBoxRect = null;
            scrollRect = null;
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

        // Get keyboard shortcuts
        [HarmonyPatch(typeof(Shell), "Update")]
        [HarmonyPrefix]
        private static void ShellUpdatePrefix()
        {
            KeyCombo keyCombo = KeyCombo.Detect(false);

            if (keyCombo == lastFrameKeyCombo)
            {
                lastFrameKeyCombo = keyCombo;
                return;
            }
            lastFrameKeyCombo = keyCombo;

            // Ctrl + [key]
            if (keyCombo.IncludeCount == 2 && keyCombo.GetInclude(0) == Key.Control)
            {
                Key key = keyCombo.GetInclude(1);

                if (key == Key.U)
                {
                    // Scroll halfpage up
                    Vector3 pos = shellTextRect.localPosition;
                    pos.y -= (textBoxRect.rect.height / 2.0f);
                    shellTextRect.localPosition = pos;
                }
                else if (key == Key.D)
                {
                    // Scroll halfpage down
                    Vector3 pos = shellTextRect.localPosition;
                    pos.y += (textBoxRect.rect.height / 2.0f);
                    shellTextRect.localPosition = pos;
                }
                else if (key == Key.P)
                {
                    // Previous in history
                    if (historyIndex > 0)
                    {
                        moveInHistory(inputField, -1);
                    }
                }
                else if (key == Key.N)
                {
                    // Next in history
                    if (historyIndex < CommandHistory.Count)
                    {
                        moveInHistory(inputField, 1);
                    }
                }
            }
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
                ).SetAndAdvance(OpCodes.Ldarg_0, null) // For labeling purposes
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

        // Before printing new text, take note of where we are in the scrollback, and whether we should scroll to accomidate this new text
        [HarmonyPatch(typeof(Shell), "Print", new Type[] { typeof(string) })]
        [HarmonyPrefix]
        private static void ShellPrintPrefix(string str, ref ScrollbackPosition __state)
        {
            __state.bottom = (scrollRect.verticalNormalizedPosition <= 0.025f);
            __state.offset = shellTextRect.offsetMax;

            if (__state.bottom)
                shellTextRect.pivot = new Vector2(0.5f, 0.0f);
            else
                shellTextRect.pivot = new Vector2(0.5f, 1.0f);
        }

        // After printing the text, adjust to keep the position the same if we're not at the bottom of the scrollback
        [HarmonyPatch(typeof(Shell), "Print", new Type[] { typeof(string) })]
        [HarmonyPostfix]
        private static void ShellPrintPostfix(string str, ref ScrollbackPosition __state)
        {
            if (!__state.bottom)
                shellTextRect.anchoredPosition = __state.offset;
        }

        // Change how many lines the shell saves in its text buffer for scrollback purposes
        [HarmonyPatch(typeof(Shell), "Print", new Type[] { typeof(string) })]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> ShellPrintTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            // Default is 40 lines, so match that and replace with the field from this class
            CodeMatcher codeMatcher = new CodeMatcher(instructions);
            codeMatcher.MatchStartForward(
                    new CodeMatch(OpCodes.Ldc_I4_S, (SByte) 40)
                ).SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldsfld, typeof(BetterInGameShell).GetField("maxScrollbackLines")));

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
