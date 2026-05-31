using System;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace ZmanBase
{
    using HarmonyLib;

    internal static class BetterInGameShell
    {
        public static bool IsEnabled { get; private set; } = false;

        // Apply shell modifications
        public static void Start()
        {
            if (IsEnabled)
                return;

            Harmony.CreateAndPatchAll(typeof(BetterInGameShell), "BetterInGameShell");
            IsEnabled = true;
        }

        // Reset shell to default state
        public static void Stop()
        {
            Harmony.UnpatchID("BetterInGameShell");
            IsEnabled = false;
        }

        // Patch the Shell such that it doesn't force the input to be lowercase or trim off the whitespace
        [HarmonyPatch(typeof(Shell), "Update")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> ShellUpdateTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions);

            codeMatcher.MatchStartForward(
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(String), "Trim"))
                ).RemoveInstruction();

            codeMatcher.MatchStartForward(
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(String), "ToLowerInvariant"))
                ).RemoveInstruction();

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
    }
}
