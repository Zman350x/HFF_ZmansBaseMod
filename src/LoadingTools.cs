using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;

namespace HffArchipelagoClient
{
    using HarmonyLib;
    using HumanAPI;

    public static class LoadingTools
    {
        public const string emptySceneName = "Assets/Scenes/Empty.unity";

        public static void LoadLevel(WorkshopLevelMetadata levelData)
        {
            if (Game.currentLevel != null)
                Game.currentLevel.gameObject.SetActive(false);
            Multiplayer.App.instance.LaunchSinglePlayer(levelData.workshopId, levelData.levelType, 0, 0);
        }

        public static void Patch()
        {
            Harmony.CreateAndPatchAll(typeof(LoadingTools), "LoadingTools");
        }

        public static void Unpatch()
        {
            Harmony.UnpatchID("LoadingTools");
        }

        [HarmonyPatch(typeof(Game), "LoadLevel", MethodType.Enumerator)]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> GameLoadLevelMoveNext(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions);

            // Get access to the sceneName local variable
            FieldInfo sceneNameField = AccessTools.GetDeclaredFields(originalMethod.DeclaringType).Single(field => field.Name.Contains("<sceneName>"));
            FieldInfo levelNumberField = AccessTools.GetDeclaredFields(originalMethod.DeclaringType).Single(field => field.Name.Contains("levelNumber"));
            FieldInfo levelTypeField = AccessTools.GetDeclaredFields(originalMethod.DeclaringType).Single(field => field.Name.Contains("levelType"));

            // Replace "!= ulong.MaxValue" with "<= 0xF000000000000000UL"
            // Normally the code adds a bundle exclusion for the main menu, which has a level number of `-1` (MaxValue when unsigned)
            // This just extends that exclusion so the hub world can have a level number of `-2`, and reserves room to add more runtime levels if needed
            codeMatcher.MatchEndForward(
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, levelNumberField),
                    new CodeMatch(OpCodes.Ldc_I4_M1)
                ).SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldc_I8, -(1L<<60)))
                .RemoveInstruction() // There is an unneeded type conversion
                .SetOpcodeAndAdvance(OpCodes.Cgt_Un);

            // Change the != to == to check when the level type is unspecified
            codeMatcher.Start().MatchEndForward(
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, levelTypeField),
                    new CodeMatch(OpCodes.Ldc_I4_S, (SByte) WorkshopItemSource.NotSpecified),
                    new CodeMatch(OpCodes.Beq)
                ).SetOpcodeAndAdvance(OpCodes.Bne_Un_S);

            // Set the scene name to empty if unspecified level type
            codeMatcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldstr, emptySceneName),
                    new CodeInstruction(OpCodes.Stfld, sceneNameField)
                );

            // Find the level type switch statement
            codeMatcher.Start().MatchEndForward(
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, levelTypeField),
                    new CodeMatch(OpCodes.Switch)
                );

            // Advance to the label
            Label lobbySwitchLabel = (codeMatcher.Operand as Label[])[(int) WorkshopItemSource.BuiltInLobbies];
            while (codeMatcher.Remaining > 0)
            {
                if (codeMatcher.Labels.Contains(lobbySwitchLabel))
                    break;

                codeMatcher.Advance(1);
            }

            // Add code to load lobbies
            codeMatcher.SetAndAdvance(OpCodes.Ldarg_0, null) // Set instead of insert to keep the labels aligned
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, levelNumberField),
                    new CodeInstruction(OpCodes.Call, typeof(WorkshopRepository).GetMethod("GetLobbyFilename", new Type[] { typeof(ulong) })),
                    new CodeInstruction(OpCodes.Stfld, sceneNameField)
                );

            return codeMatcher.Instructions();
        }

        [HarmonyPatch(typeof(Game), "Fall")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> GameFall(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);

            // Match the "currentLevelType == EditorPick" if statement
            codeMatcher.MatchStartForward(
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, typeof(Game).GetField("currentLevelType")),
                    new CodeMatch(OpCodes.Ldc_I4_1),
                    new CodeMatch(OpCodes.Bne_Un),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldc_I4_0),
                    new CodeMatch(OpCodes.Stfld, typeof(Game).GetField("passedLevel"))
                );

            // Add the additional condition that the ArchipelagoClient must be inactive
            Label label = (Label) codeMatcher.InstructionAt(3).operand;
            codeMatcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Call, typeof(ArchipelagoClient).GetMethod("get_IsActive", new Type[] {})),
                    new CodeInstruction(OpCodes.Brtrue, label)
                );

            // Find where `StatsAndAchievements.PassLevel()` is called
            codeMatcher.MatchStartForward(
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, typeof(Game).GetField("levels")),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, typeof(Game).GetField("currentLevelNumber")),
                    new CodeMatch(OpCodes.Ldelem_Ref),
                    new CodeMatch(OpCodes.Ldloc_0),
                    new CodeMatch(OpCodes.Call, typeof(StatsAndAchievements).GetMethod("PassLevel", new Type[] { typeof(string), typeof(Human) }))
                );

            // Only run on built-in level
            // Advancing past a `Ldarg_0` and adding one at the end so the labels line up
            Label label2;
            codeMatcher.CreateLabelAt(codeMatcher.Pos + 7, out label2);
            codeMatcher.Advance(1).InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldfld, typeof(Game).GetField("currentLevelType")),
                    new CodeInstruction(OpCodes.Ldc_I4_0),
                    new CodeInstruction(OpCodes.Bne_Un, label2),
                    new CodeInstruction(OpCodes.Ldarg_0)
                );

            return codeMatcher.Instructions();
        }

        [HarmonyPatch(typeof(Game), "PassLevel", MethodType.Enumerator)]
        [HarmonyPrefix]
        private static void GamePassLevel(ref bool __runOriginal)
        {
            // Return to hub after beating level if active
            if (ArchipelagoClient.IsActive)
            {
                // Except for Ice -> Reprise
                if (Game.instance.currentLevelType == WorkshopItemSource.BuiltIn &&
                    Game.instance.currentLevelNumber == 11)
                    return;

                Game.currentLevel.CompleteLevel();
                HubWorld.LoadHubWorld();
                StatsAndAchievements.Save();
                __runOriginal = false;
            }
        }

        [HarmonyPatch(typeof(GameSave), "PassCheckpointCampaign")]
        [HarmonyPatch(typeof(GameSave), "PassCheckpointEditorPick")]
        [HarmonyPatch(typeof(GameSave), "PassCheckpointWorkshop")]
        [HarmonyPrefix]
        private static void GameSavePassCheckpoint(ref bool __runOriginal)
        {
            if (ArchipelagoClient.IsActive)
                __runOriginal = false;
        }
    }
}
