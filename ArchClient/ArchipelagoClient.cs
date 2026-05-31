using System;

namespace HffArchipelagoClient
{
    using BepInEx;
    using HarmonyLib;
    using UnityEngine.SceneManagement;

    [BepInPlugin("top.zman350x.hff.archipelagoclient", "Human: Fall Flat Archipelago Client", "0.1.0")]
    [BepInProcess("Human.exe")]
    public sealed class ArchipelagoClient : BaseUnityPlugin
    {
        public static ArchipelagoClient Instance { get; private set; }
        public static bool IsActive { get; private set; } = false;

        public static CommandRegistry commands;

        internal static new BepInEx.Logging.ManualLogSource Logger;

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            commands = (CommandRegistry) AccessTools.DeclaredField(typeof(Shell), "commands").GetValue(null);

#if DEBUG
            Shell.RegisterCommand("scenes", (string x) => {
                for(int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                    Shell.print($"{scenePath}");
                }
            }, "scenes\r\nList all scenes");
#endif

            SceneManager.sceneLoaded += OnSceneLoaded;
            Harmony.CreateAndPatchAll(typeof(ArchipelagoClient), "ArchipelagoClient");
            LoadingTools.Patch();
        }

        private static void OnStartup()
        {
            MenuButtonTools.AddButton("SelectPlayersMenu", "ArchipelagoButton", "ARCHIPELAGO", 3, ArchipelagoStart);
        }

        public static void ArchipelagoStart()
        {
            if (!MenuSystem.CanInvoke)
                return;

            LevelSource.FetchEnabledLevels(new WorkshopItemSource[] {
                WorkshopItemSource.BuiltIn,
                WorkshopItemSource.EditorPick,
                WorkshopItemSource.BuiltInLobbies
            });
            ControlLockSource.FetchEnabledControlLocks(new ControlType[] {
                // ControlType.GRAB_LEFT,
                // ControlType.GRAB_RIGHT,
                // ControlType.JUMP,
                // ControlType.PLAY_DEAD,
                // ControlType.LOOK_LEFT,
                // ControlType.LOOK_RIGHT,
                // ControlType.LOOK_UP,
                // ControlType.LOOK_DOWN,
                // ControlType.SHOOT_FIREWORKS
            });

            InputLimiter.Patch();
            Shell.RegisterCommand("hub", new Action(HubWorld.LoadHubWorld), "hub\r\nReturn to the Archipelago hub");
            MenuButtonTools.AddButton("PauseMenu", "HubButton", "RETURN TO ARCHIPELAGO HUB", 7, () => {
                    Game.instance.Resume();
                    MenuSystem.instance.HideMenus();
                    HubWorld.LoadHubWorld();
            });

            HubWorld.LoadHubWorld();
            IsActive = true;
        }

        public static void ArchipelagoEnd()
        {
            InputLimiter.Unpatch();
            commands.UnRegisterCommand("hub", new Action(HubWorld.LoadHubWorld));
            MenuButtonTools.DestroyButton("PauseMenu", "HubButton");
            MenuButtonTools.EnableDisableButton("PauseMenu", "ExitButton", true);

            IsActive = false;
        }

        public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.path == "Assets/Scenes/Startup.unity")
            {
                OnStartup();
                return;
            }

            if (scene.path == LoadingTools.emptySceneName &&
                Game.instance.currentLevelType == WorkshopItemSource.NotSpecified &&
                Multiplayer.App.state != Multiplayer.AppSate.Menu)
            {
                MenuButtonTools.EnableDisableButton("PauseMenu", "HubButton", false);
                MenuButtonTools.EnableDisableButton("PauseMenu", "ExitButton", true);
                HubWorld.OnHubWorldLoaded();
                return;
            }

            if (Game.instance.currentLevelType == WorkshopItemSource.BuiltInLobbies ||
                Game.instance.currentLevelType == WorkshopItemSource.SubscriptionLobbies)
            {
                Multiplayer.MultiplayerLobbyController.instance.HideUI();
            }

            if (IsActive)
            {
                MenuButtonTools.EnableDisableButton("PauseMenu", "HubButton", true);
                MenuButtonTools.EnableDisableButton("PauseMenu", "ExitButton", false);
                Barrier.OnSceneLoaded(scene, mode);
                Portal.OnSceneLoaded(scene, mode);
            }
        }

        [HarmonyPatch(typeof(Multiplayer.App), "ExitGame")]
        [HarmonyPostfix]
        private static void ExitGame()
        {
            ArchipelagoEnd();
        }
    }
}
