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

    public enum Levels
    {
        // Main levels
        MANSION = 0,
        TRAIN = 1,
        CARRY = 2,
        MOUNTAIN = 3,
        DEMOLITION = 4,
        CASTLE = 5,
        WATER = 6,
        POWER_PLANT = 7,
        AZTEC = 8,
        DARK = 9,
        STEAM = 10,
        ICE = 11,
        REPRISE = 12,
        CREDITS = 13,

        // Extra dreams
        THERMAL = 16,
        FACTORY = 17,
        GOLF = 18,
        CITY = 19,
        FOREST = 20,
        LABORATORY = 21,
        LUMBER = 22,
        RED_ROCK = 23,
        TOWER = 24,
        MINIATURE = 25,
        COPPER_WORLD = 26,
        PORT = 27,
        // Port has two scenes, one unused
        // Skipping 28 to keep in line with level numbers
        UNDERWATER = 29,
        DOCKYARD = 30,
        MUSEUM = 31,
        HIKE = 32,
        CANDYLAND = 33,
        TEST_CHAMBER = 34,
        STEAMPUNK_PARTY = 35,
        VIKING = 36,
        ANNIVERSARY = 37,

        // Lobbies
        WORKSHOP_LOBBY = 64,
        BOWLING_LOBBY = 65,
        CHRISTMAS_LOBBY = 66,
        LUNAR_LOBBY = 67,

        // Useful non-level scenes
        STARTUP = 128,
        EMPTY = 129,
        CUSTOMIZATION = 130
    }

    public static class LevelTools
    {
        public static readonly BiMap<Levels, string> levelScenes = new BiMap<Levels, string>()
        {
            { Levels.MANSION, "Assets/Scenes/Levels/Intro.unity" },
            { Levels.TRAIN, "Assets/Scenes/Levels/Push.unity" },
            { Levels.CARRY, "Assets/Scenes/Levels/Carry.unity" },
            { Levels.MOUNTAIN, "Assets/Scenes/Levels/Climb.unity" },
            { Levels.DEMOLITION, "Assets/Scenes/Levels/Break.unity" },
            { Levels.CASTLE, "Assets/Scenes/Levels/Siege.unity" },
            { Levels.WATER, "Assets/Scenes/Levels/River.unity" },
            { Levels.POWER_PLANT, "Assets/Scenes/Levels/Power.unity" },
            { Levels.AZTEC, "Assets/Scenes/Levels/Aztec.unity" },
            { Levels.DARK, "Assets/Scenes/Levels/Halloween.unity" },
            { Levels.STEAM, "Assets/Scenes/SteamExperimental/Steam_merged.unity" },
            { Levels.ICE, "Assets/Scenes/Experiments/IceExperimental/Ice_merged.unity" },
            { Levels.REPRISE, "Assets/Scenes/Levels/Intro_Reprise.unity" },
            { Levels.CREDITS, "Assets/Scenes/Credits.unity" },
            { Levels.THERMAL, "Assets/ContestLevels/ThermalAssets/Thermal.unity" },
            { Levels.FACTORY, "Assets/ContestLevels/FactoryAssets/Factory.unity" },
            { Levels.GOLF, "Assets/ContestLevels/GolfAssets/Golf.unity" },
            { Levels.CITY, "Assets/ContestLevels/CityAssets/City.unity" },
            { Levels.FOREST, "Assets/ContestLevels/ForestAssets/Forest.unity" },
            { Levels.LABORATORY, "Assets/ContestLevels/LabAssets/Lab.unity" },
            { Levels.LUMBER, "Assets/ContestLevels/LumberAssets/Lumber.unity" },
            { Levels.RED_ROCK, "Assets/ContestLevels/RedRockAssets/RedRock.unity" },
            { Levels.TOWER, "Assets/ContestLevels/TowerAssets/Tower.unity" },
            { Levels.MINIATURE, "Assets/ContestLevels/MiniatureAssets/Miniature.unity" },
            { Levels.COPPER_WORLD, "Assets/ContestLevels/CopperWorldAssets/CopperWorld.unity" },
            { Levels.PORT, "Assets/ContestLevels/NavalAssests/Naval_Ben.unity" },
            { Levels.UNDERWATER, "Assets/ContestLevels/UnderwaterAssets/OceanAdventure.unity" },
            { Levels.DOCKYARD, "Assets/ContestLevels/DockyardAssets/Dockyard.unity" },
            { Levels.MUSEUM, "Assets/ContestLevels/MuseumAssets/Museum.unity" },
            { Levels.HIKE, "Assets/ContestLevels/HikeAssets/Scenes/Hike.unity" },
            { Levels.CANDYLAND, "Assets/ContestLevels/CandylandAssets/Candyland.unity" },
            { Levels.TEST_CHAMBER, "Assets/ContestLevels/FacilityAssets/Facility.unity" },
            { Levels.STEAMPUNK_PARTY, "Assets/ContestLevels/Punk/SteamPunk.unity" },
            { Levels.VIKING, "Assets/ContestLevels/VikingAssets/Viking.unity" },
            { Levels.ANNIVERSARY, "Assets/ContestLevels/AnniversaryAssets/Anniversary.unity" },
            { Levels.WORKSHOP_LOBBY, "Assets/WorkShop/Scenes/Levels/WorkshopLobby.unity" },
            { Levels.BOWLING_LOBBY, "Assets/Scenes/Lobby.unity" },
            { Levels.CHRISTMAS_LOBBY, "Assets/Scenes/Special/Xmas.unity" },
            { Levels.LUNAR_LOBBY, "Assets/Scenes/Lobbies/Zodiac.unity" },
            { Levels.STARTUP, "Assets/Scenes/Startup.unity" },
            { Levels.EMPTY, "Assets/Scenes/Empty.unity" },
            { Levels.CUSTOMIZATION, "Assets/Scenes/Customization.unity" }
        };

        public static event Action StartupEvent;
        public static ulong loadingLevelNumber { get; private set; }

        private static Dictionary<ulong, Action> runtimeLevels = new Dictionary<ulong, Action>();
        private const ulong minRuntimeLevelIndex = unchecked ((ulong) (uint) int.MinValue); // Have to cast via uint to prevent sign extension

        public static void LoadLevel(WorkshopLevelMetadata levelData)
        {
            if (Game.currentLevel != null)
                Game.currentLevel.gameObject.SetActive(false);
            Multiplayer.App.instance.LaunchSinglePlayer(levelData.workshopId, levelData.levelType, 0, 0);
        }

        // Returns the level number on success or 0 on failure
        public static ulong RegisterRuntimeLevel(Action levelCallback)
        {
            for (ulong i = ulong.MaxValue - 1; i >= minRuntimeLevelIndex; --i)
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
            Harmony.CreateAndPatchAll(typeof(LevelTools), "LevelTools");
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        internal static void Disable()
        {
            Harmony.UnpatchID("LevelTools");
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.path == levelScenes.Forward[Levels.STARTUP])
            {
                StartupEvent?.Invoke();
                return;
            }

            if (Game.instance.currentLevelType == WorkshopItemSource.BuiltInLobbies ||
                Game.instance.currentLevelType == WorkshopItemSource.SubscriptionLobbies)
            {
                Multiplayer.MultiplayerLobbyController.instance.HideUI();
            }

            if (scene.path == levelScenes.Forward[Levels.EMPTY] &&
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
        private static IEnumerable<CodeInstruction> GameLoadLevelMoveNext(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase originalMethod)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);

            // Get access to the sceneName local variable
            FieldInfo sceneNameField = AccessTools.GetDeclaredFields(originalMethod.DeclaringType).Single(field => field.Name.Contains("<sceneName>"));
            FieldInfo levelNumberField = AccessTools.GetDeclaredFields(originalMethod.DeclaringType).Single(field => field.Name.Contains("levelNumber"));
            FieldInfo levelTypeField = AccessTools.GetDeclaredFields(originalMethod.DeclaringType).Single(field => field.Name.Contains("levelType"));
            FieldInfo gameInstanceField = AccessTools.GetDeclaredFields(originalMethod.DeclaringType).Single(field => field.Name.Contains("$this"));

            // Replace "!= ulong.MaxValue" with "< 0x0000000080000000UL" (negative if viewed as signed int32)
            // Normally the code adds a bundle exclusion for the main menu, which has a level number of `-1` (MaxValue when unsigned)
            // This just extends that exclusion so that custom runtime levels can have level numbers of other negative values
            // Have to use the bound for an `int` instead of a `long` to be safe since the game casts it down to the smaller type in the `Game` class
            // despite storing it as a ulong everywhere else
            codeMatcher.MatchEndForward(
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, levelNumberField),
                    new CodeMatch(OpCodes.Ldc_I4_M1)
                ).SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldc_I8, (Int64) minRuntimeLevelIndex))
                .RemoveInstruction() // There is an unneeded type conversion
                .SetOpcodeAndAdvance(OpCodes.Clt_Un)
                .RemoveInstructions(2); // The "!=" took 3 instructions, our "<" needs only one

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
                    new CodeInstruction(OpCodes.Ldstr, levelScenes.Forward[Levels.EMPTY]),
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

            // Add check to fix not loading different level if they have the same level number
            // Changes the line `if (this.currentLevelNumber != (int)levelNumber)` to `if (this.currentLevelNumber != (int)levelNumber || this.currentLevelType != levelType)`

            // Find the if statement
            codeMatcher.Start().MatchEndForward(
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, gameInstanceField),
                    new CodeMatch(OpCodes.Ldfld, typeof(Game).GetField("currentLevelNumber")),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, levelNumberField),
                    new CodeMatch(OpCodes.Conv_I4),
                    new CodeMatch(OpCodes.Beq)
                );

            // Add the new part
            Label skipLoadingLabel = (Label) codeMatcher.Operand;
            Label loadingLabel;
            codeMatcher.CreateLabelAt(codeMatcher.Pos + 1, out loadingLabel).SetAndAdvance(OpCodes.Bne_Un, loadingLabel);
            codeMatcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, gameInstanceField),
                    new CodeInstruction(OpCodes.Ldfld, typeof(Game).GetField("currentLevelType")),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, levelTypeField),
                    new CodeInstruction(OpCodes.Beq, skipLoadingLabel)
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
            Label label;
            codeMatcher.CreateLabelAt(codeMatcher.Pos + 7, out label);
            codeMatcher.Advance(1).InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldfld, typeof(Game).GetField("currentLevelType")),
                    new CodeInstruction(OpCodes.Ldc_I4_0),
                    new CodeInstruction(OpCodes.Bne_Un, label),
                    new CodeInstruction(OpCodes.Ldarg_0)
                );

            return codeMatcher.Instructions();
        }
    }
}
