namespace HffArchipelagoClient
{
    using HarmonyLib;

    public static class InputLimiter
    {

        public static void Patch()
        {
            Harmony.CreateAndPatchAll(typeof(InputLimiter), "InputLimiter");
        }

        public static void Unpatch()
        {
            Harmony.UnpatchID("InputLimiter");
        }

        [HarmonyPatch(typeof(InControl.PlayerAction), "Update")]
        [HarmonyPrefix]
        public static void Update(InControl.PlayerAction __instance, ref bool __runOriginal)
        {
            __runOriginal = true;

            switch (__instance.Name)
            {
                case "Left Hand":
                    __runOriginal = ControlLockSource.EnabledControlLocks[(int) ControlType.GRAB_LEFT].IsUnlocked();
                    break;
                case "Right Hand":
                    __runOriginal = ControlLockSource.EnabledControlLocks[(int) ControlType.GRAB_RIGHT].IsUnlocked();
                    break;
                case "Jump":
                    __runOriginal = ControlLockSource.EnabledControlLocks[(int) ControlType.JUMP].IsUnlocked();
                    break;
                case "Play Dead":
                    __runOriginal = ControlLockSource.EnabledControlLocks[(int) ControlType.PLAY_DEAD].IsUnlocked();
                    break;
                case "Look Left":
                    __runOriginal = ControlLockSource.EnabledControlLocks[(int) ControlType.LOOK_LEFT].IsUnlocked();
                    break;
                case "Look Right":
                    __runOriginal = ControlLockSource.EnabledControlLocks[(int) ControlType.LOOK_RIGHT].IsUnlocked();
                    break;
                case "Look Up":
                    __runOriginal = ControlLockSource.EnabledControlLocks[(int) ControlType.LOOK_UP].IsUnlocked();
                    break;
                case "Look Down":
                    __runOriginal = ControlLockSource.EnabledControlLocks[(int) ControlType.LOOK_DOWN].IsUnlocked();
                    break;
                case "Shoot Fireworks":
                    __runOriginal = ControlLockSource.EnabledControlLocks[(int) ControlType.SHOOT_FIREWORKS].IsUnlocked();
                    break;
                default:
                    break;
            }
        }
    }
}
