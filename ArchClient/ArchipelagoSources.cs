using System;
using System.Collections.Generic;
using System.Linq;

namespace HffArchipelagoClient
{
    using HumanAPI;

    public abstract class Unlockable
    {
        private bool _unlocked = false;
        private HashSet<Action<bool>> callbacks = new HashSet<Action<bool>>();

        public bool IsUnlocked()
        {
            return _unlocked;
        }

        public void SetUnlocked(bool isUnlocked)
        {
            _unlocked = isUnlocked;

            foreach (Action<bool> callback in callbacks)
            {
                callback.Invoke(isUnlocked);
            }
        }

        public void RegisterCallback(Action<bool> callback)
        {
            callbacks.Add(callback);
        }

        public void UnregisterCallback(Action<bool> callback)
        {
            callbacks.Remove(callback);
        }
    }

    public class LevelSource : Unlockable
    {
        public WorkshopLevelMetadata levelData;

        public LevelSource(WorkshopLevelMetadata levelData, bool isUnlocked = true)
        {
            this.levelData = levelData;
            SetUnlocked(isUnlocked);
        }

        public static List<LevelSource> EnabledLevels { get; private set; }
        public static void FetchEnabledLevels(WorkshopItemSource[] sources)
        {
            // Force the game to load the metadata for all levels and lobbies
            WorkshopRepository.instance.LoadBuiltinLevels(true);
            WorkshopRepository.instance.LoadEditorPickLevels(false);

            EnabledLevels = new List<LevelSource>();

            foreach (WorkshopItemSource source in sources)
            {
                EnabledLevels.AddRange(
                    WorkshopRepository.instance.levelRepo.BySource(source)
                        .Select(level => new LevelSource(level))
                );
            }

            // Always unlock the first level
            if (EnabledLevels.Count > 0)
                EnabledLevels[0].SetUnlocked(true);
        }

        public static void PrintEnabledLevels()
        {
            foreach (LevelSource level in EnabledLevels)
            {
                Shell.print($"Level {level.levelData.title} is {(level.IsUnlocked() ? "unlocked" : "locked")} | type {level.levelData.levelType}");
            }
        }
    }

    public enum ControlType
    {
        GRAB_LEFT,
        GRAB_RIGHT,
        JUMP,
        PLAY_DEAD,
        LOOK_LEFT,
        LOOK_RIGHT,
        LOOK_UP,
        LOOK_DOWN,
        SHOOT_FIREWORKS
    }

    public class ControlLockSource : Unlockable
    {
        public const int ControlTypeCount = 9; // Number of items in the enum above
        public static ControlLockSource[] EnabledControlLocks { get; private set; }

        public ControlLockSource(bool isUnlocked = true)
        {
            SetUnlocked(isUnlocked);
        }

        public static void FetchEnabledControlLocks(ControlType[] sources)
        {
            EnabledControlLocks = new ControlLockSource[ControlTypeCount];

            // Initialize array items
            for (int i = 0; i < ControlTypeCount; ++i)
            {
                EnabledControlLocks[i] = new ControlLockSource();
            }

            // Only lock the limited controls
            foreach (ControlType source in sources)
            {
                EnabledControlLocks[(int) source].SetUnlocked(false);
            }
        }
    }
}
