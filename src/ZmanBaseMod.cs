using System;

namespace ZmanBase
{
    using BepInEx;
    using BepInEx.Configuration;
    using HarmonyLib;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    [BepInPlugin("top.zman350x.hff.zmanbase", "Zman's Human: Fall Flat Base Mod", "0.2.0")]
    [BepInProcess("Human.exe")]
    public sealed class ZmanBaseMod : BaseUnityPlugin
    {
        public static CommandRegistry Commands { get; private set; }
        public static Traverse SetResolution { get; private set; }

        internal static new BepInEx.Logging.ManualLogSource Logger;

        private ConfigEntry<bool> betterInGameShellEnabled;

        private ConfigMenu baseModConfigMenu;

        private void Awake()
        {
            Commands = (CommandRegistry) AccessTools.DeclaredField(typeof(Shell), "commands").GetValue(null);

            Logger = base.Logger;

            betterInGameShellEnabled = Config.Bind<bool>("BetterInGameShell",
                                                         "Enabled",
                                                         false,
                                                         "Enables the better in-game shell");

            baseModConfigMenu = new ConfigMenu("ZmanBaseMod", "Zman's Base Mod", "ZMAN'S BASE MOD");

            LevelTools.Enable();
            LevelTools.StartupEvent += OnStartup;
        }

        private void Start()
        {
            SetResolution = Traverse.Create(MenuSystem.instance.GetMenu<VideoMenu>()).Method("ForceResolution", 0, 0, false);

            if (betterInGameShellEnabled.Value)
                BetterInGameShell.Start();

            RegisterCommands();
        }

        private void OnStartup()
        {
            ConfigMenu.Enable();
        }

        private void RegisterCommands()
        {
            string scenesHelp = "USAGE: scenes\r\n\r\nList all scene paths";
            // List all Unity scene paths
            Shell.RegisterCommand("scenes", () =>
            {
                for(int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                    Debug.Log($"{scenePath}");
                }
            }, scenesHelp);

            string resHelp = "USAGE: res <width> <height> [fullscreen]\r\n\r\n" +
                "Resizes the game window to <width> by <height> pixels. A third optional argument of `true` (fullscreen) or `false` (windowed) can be provided to change the fullscreen state.";
            // Allow the user to change the game resolution from the in-game shell
            Shell.RegisterCommand("res", (string txt) =>
            {
                if (txt is null)
                {
                    Debug.LogError($"ERROR: Invalid number of arguments\r\n{resHelp}");
                    return;
                }

                string[] args = txt.Trim().Split(' ');

                try
                {
                    if (args.Length == 3)
                        StartCoroutine(SetResolution.GetValue<System.Collections.IEnumerator>(int.Parse(args[0]),
                                                                                              int.Parse(args[1]),
                                                                                              bool.Parse(args[2])));
                    else if (args.Length == 2)
                    {
                        StartCoroutine(SetResolution.GetValue<System.Collections.IEnumerator>(int.Parse(args[0]),
                                                                                              int.Parse(args[1]),
                                                                                              Screen.fullScreen));
                    }
                    else
                    {
                        Debug.LogError($"ERROR: Invalid number of arguments\r\n{resHelp}");
                        return;
                    }
                }
                catch (Exception e) when (e is FormatException || e is OverflowException || e is ArgumentNullException)
                {
                    Debug.LogError($"ERROR: Invalid arguments\r\n{resHelp}");
                    return;
                }
            }, resHelp);

            string listResourcesHelp = "USAGE: list_resources <type>\r\n\r\n" +
                "Lists the loaded resources of the provided type. The type string should be in the format required by `Type.GetType(string)` (ex. \"list_resources TMPro.TMP_FontAsset, Assembly-CSharp\"). " +
                "As the type string is case-sensitive, activating BIGS may be necessary.";
            Shell.RegisterCommand("list_resources", (string txt) =>
            {
                if (txt is null)
                {
                    Debug.LogError($"ERROR: Invalid number of arguments\r\n{listResourcesHelp}");
                    return;
                }

                Type t = Type.GetType(txt);
                Object[] found = Resources.FindObjectsOfTypeAll(t);

                foreach (Object resource in found)
                {
                    Debug.Log(resource.ToString());
                }
            }, listResourcesHelp);

            string levelExtraHelp = "USAGE: level_extra <level> <checkpoint>\r\n\r\nLoad checkpoint at extra dream level <level> at checkpoint <checkpoint>. `le` for short.";
            Action<string> levelExtraCommand = (string txt) =>
            {
                if (txt is null)
                {
                    Debug.LogError($"ERROR: Invalid number of arguments\r\n{levelExtraHelp}");
                    return;
                }

                string[] args = txt.Trim().Split(' ');

                try
                {
                    if (args.Length == 2)
                    {
                        int lvlNum = int.Parse(args[0]);
                        int cpNum = int.Parse(args[1]);

                        if (lvlNum < 0 || cpNum < 0 || lvlNum >= Game.instance.editorPickLevels.Length)
                        {
                            Debug.LogError($"ERROR: Invalid arguments\r\n{levelExtraHelp}");
                            return;
                        }

                        if (Multiplayer.NetGame.isClient || Multiplayer.NetGame.isServer)
                        {
                            Debug.LogError($"ERROR: Cannot change the level in multiplayer");
                            return;
                        }

                        if (Game.instance.currentLevelType == WorkshopItemSource.EditorPick && Game.instance.currentLevelNumber == lvlNum)
                        {
                            Game.instance.RestartCheckpoint(cpNum, 0);
                        }
                        else
                        {
                            Multiplayer.App.instance.LaunchSinglePlayer((ulong) lvlNum, WorkshopItemSource.EditorPick, cpNum, 0);
                        }
                    }
                    else
                    {
                        Debug.LogError($"ERROR: Invalid number of arguments\r\n{levelExtraHelp}");
                        return;
                    }
                }
                catch (Exception e) when (e is FormatException || e is OverflowException || e is ArgumentNullException)
                {
                    Debug.LogError($"ERROR: Invalid arguments\r\n{levelExtraHelp}");
                    return;
                }
            };
            Shell.RegisterCommand("le", levelExtraCommand, null);
		    Shell.RegisterCommand("level_extra", levelExtraCommand, levelExtraHelp);

            string echoHelp = "USAGE: echo [text]\r\n\r\nDisplay argument text";
            // Echos text
            Shell.RegisterCommand("echo", (string txt) =>
            {
                if (!(txt is null))
                    Debug.Log(txt);
            }, echoHelp);

            string useBigsHelp = "USAGE: use_bigs <true|false>\r\n\r\n" +
                "Enables/disables the Better In-Game Shell (BIGS). Unlike the default shell, BIGS is case-sensitive and does not trim argument whitespace. It may do more in the future.";
            // Enables/disables BIGS
            Shell.RegisterCommand("use_bigs", (string txt) =>
            {
                if (txt is null)
                {
                    Debug.LogError($"ERROR: Invalid number of arguments\r\n{useBigsHelp}");
                    return;
                }

                string[] args = txt.Trim().Split(' ');

                try
                {
                    if (args.Length == 1)
                    {
                        betterInGameShellEnabled.Value = bool.Parse(args[0]);

                        if (betterInGameShellEnabled.Value)
                            BetterInGameShell.Start();
                        else
                            BetterInGameShell.Stop();
                    }
                    else
                    {
                        Debug.LogError($"ERROR: Invalid number of arguments\r\n{useBigsHelp}");
                        return;
                    }
                }
                catch (Exception e) when (e is FormatException || e is OverflowException || e is ArgumentNullException)
                {
                    Debug.LogError($"ERROR: Invalid arguments\r\n{useBigsHelp}");
                    return;
                }
            }, useBigsHelp);
        }
    }
}
