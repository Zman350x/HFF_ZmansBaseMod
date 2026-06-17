using System.Collections.Generic;
using System.Linq;

namespace ZmanBase
{
    using UnityEngine;

    public class ConfigMenu
    {
        public static bool IsConfigMenuSystemActive { get; private set; }
        public static List<ConfigMenu> ConfigMenus { get; private set; } = new List<ConfigMenu>();

        public static MenuTools.BasicCustomMenu modsMenu;

        public bool IsValid { get; private set; } = true;
        public readonly string name;
        public readonly string text;

        public ConfigMenu(string name, string text)
        {
            this.name = name;
            this.text = text;

            ConfigMenus.Add(this);

            if (IsConfigMenuSystemActive)
                CreateModButtons();
        }

        public static void Enable()
        {
            modsMenu = MenuTools.BasicCustomMenu.Create("OptionsMenu", "ModConfigMenu", "Mod Options");
            CreateModButtons();
        }

        public static void Disable()
        {
            DeleteModButtons();
            GameObject.Destroy(modsMenu.gameObject);
            modsMenu = null;

            foreach (ConfigMenu configMenu in ConfigMenus)
                configMenu.IsValid = false;

            ConfigMenus.Clear();
        }

        private static void CreateModButtons()
        {
            // Destroy buttons before recreating
            DeleteModButtons();

            MenuTools.MoveButtons("OptionsMenu", 0.0f, MenuTools.buttonAnchorVerticalOffset); // Move buttons up to make room for new button
            MenuTools.AddButton("OptionsMenu", "ModOptionsButton", "MOD OPTIONS", 5, () =>
            {
                if (!(MenuSystem.CanInvoke))
                    return;

                MenuTools.TransitionForward(MenuSystem.instance.GetMenu<OptionsMenu>(), modsMenu);
            });

            IEnumerable<ConfigMenu> orderedMenus = ConfigMenus.OrderBy(configMenu => configMenu.name);

            foreach (ConfigMenu configMenu in orderedMenus)
            {

            }

            IsConfigMenuSystemActive = true;
        }

        private static void DeleteModButtons()
        {
            // Already disabled, do nothing
            if (!IsConfigMenuSystemActive)
                return;

            MenuTools.DestroyButton("OptionsMenu", "ModOptionsButton");
            MenuTools.MoveButtons("OptionsMenu", 0.0f, -MenuTools.buttonAnchorVerticalOffset);

            IsConfigMenuSystemActive = false;
        }
    }
}
