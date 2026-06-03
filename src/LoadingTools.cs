using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;

namespace ZmanBase
{
    using HarmonyLib;
    using HumanAPI;
    using UnityEngine.SceneManagement;

    public static class LoadingTools
    {
        public static event Action StartupEvent;
        public const string emptySceneName = "Assets/Scenes/Empty.unity";
        public static ulong loadingLevelNumber { get; private set; }

        private static Dictionary<ulong, Action> runtimeLevels = new Dictionary<ulong, Action>();

        public static void LoadLevel(WorkshopLevelMetadata levelData)
        {
            if (Game.currentLevel != null)
                Game.currentLevel.gameObject.SetActive(false);
            Multiplayer.App.instance.LaunchSinglePlayer(levelData.workshopId, levelData.levelType, 0, 0);
        }

        // Returns the level number on success or 0 on failure
        public static ulong RegisterRuntimeLevel(Action levelCallback)
        {
            for (ulong i = ulong.MaxValue - 1; i > unchecked ((ulong) long.MinValue); --i)
            {
                if (!runtimeLevels.ContainsKey(i))
                {
                    runtimeLevels.Add(i, levelCallback);
                    return i;
                }
            }

            return 0;
        }

        public static void UnregisterRuntimeLevel(ulong levelNumber)
        {
            runtimeLevels.Remove(levelNumber);
        }

        internal static void Enable()
        {
            Harmony.CreateAndPatchAll(typeof(LoadingTools), "LoadingTools");
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        internal static void Disable()
        {
            Harmony.UnpatchID("LoadingTools");
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.path == "Assets/Scenes/Startup.unity")
            {
                StartupEvent?.Invoke();
                return;
            }

            if (Game.instance.currentLevelType == WorkshopItemSource.BuiltInLobbies ||
                Game.instance.currentLevelType == WorkshopItemSource.SubscriptionLobbies)
            {
                Multiplayer.MultiplayerLobbyController.instance.HideUI();
            }

            if (scene.path == LoadingTools.emptySceneName &&
                Game.instance.currentLevelType == WorkshopItemSource.NotSpecified &&
                Multiplayer.App.state != Multiplayer.AppSate.Menu)
            {
                Action levelCallback;
                if (runtimeLevels.TryGetValue(loadingLevelNumber, out levelCallback))
                {
                    levelCallback?.Invoke();
                }
            }
        }

        [HarmonyPatch(typeof(Game), "LoadLevel")]
        [HarmonyPrefix]
        private static void GameLoadLevel(string levelId, ulong levelNumber, int checkpointNumber, int checkpointSubObjectives, Action onComplete, WorkshopItemSource levelType)
        {
            loadingLevelNumber = levelNumber;
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

            // Replace "!= ulong.MaxValue" with "<= 0x8000000000000000UL" (negative if viewed as signed)
            // Normally the code adds a bundle exclusion for the main menu, which has a level number of `-1` (MaxValue when unsigned)
            // This just extends that exclusion so that custom runtime levels can have level numbers of other negative values
            codeMatcher.MatchEndForward(
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, levelNumberField),
                    new CodeMatch(OpCodes.Ldc_I4_M1)
                ).SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldc_I8, long.MinValue))
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
    }
}
