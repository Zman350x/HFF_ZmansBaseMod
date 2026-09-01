using System;
using System.Collections.Generic;
using System.Linq;

namespace ZmanBase
{
    using UnityEngine;
    using BepInEx.Configuration;

    public interface IConfigMenuButton
    {
        string Name { get; }
        string Text { get; }
        Action Callback { get; }
    }

    // public class IConfigMenuButtonStored<T> : IConfigMenuButton
    // {
    //     ConfigEntry<T> ConfigEntry { get; }
    // }
    //
    // public class ConfigMenuButtonBepInEx<T> : IConfigMenuButtonStored<T>
    // {
    //     public string Text { get; private set; }
    //     public ConfigEntry<T> ConfigEntry { get; private set; }
    //     public Action Callback { get; private set; }
    //
    //     public ConfigMenuButton(string text, ConfigEntry<T> configEntry, Action callback)
    //     {
    //         this.Text = text;
    //         this.ConfigEntry = configEntry;
    //         this.Callback = callback;
    //     }
    //
    //     public Type DataType
    //     {
    //         get { return typeof(T); }
    //     }
    //
    //     ConfigEntry<object> IConfigMenuButton.ConfigEntry
    //     {
    //         get { return ConfigEntry as ConfigEntry<object>; }
    //     }
    // }

    public class ConfigMenu
    {
        public static bool IsConfigMenuSystemActive { get; private set; }
        public static List<ConfigMenu> ConfigMenus { get; private set; } = new List<ConfigMenu>();
        public static MenuTools.BasicCustomMenu modsMenu;

        public readonly string name;
        public readonly string title;
        public readonly string buttonText;
        public MenuTools.BasicCustomMenu menu;

        public List<IConfigMenuButton> buttons;

        public ConfigMenu(string name, string title, string buttonText)
        {
            this.name = name;
            this.title = title;
            this.buttonText = buttonText;

            buttons = new List<IConfigMenuButton>();

            ConfigMenus.Add(this);

            if (IsConfigMenuSystemActive)
            {
                CreateMenu();
                CreateModButtons();
            }
        }

        public static void Enable()
        {
            modsMenu = MenuTools.BasicCustomMenu.Create("OptionsMenu", "ModConfigMenu", "Mod Options");

            foreach (ConfigMenu configMenu in ConfigMenus)
                if (configMenu.menu is null)
                    configMenu.CreateMenu();

            CreateModButtons();
        }

        public static void Disable()
        {
            DeleteModButtons();
            GameObject.Destroy(modsMenu.gameObject);
            modsMenu = null;

            foreach (ConfigMenu configMenu in ConfigMenus)
                configMenu.DestroyMenu();

            ConfigMenus.Clear();
        }

        private void CreateMenu()
        {
            menu = MenuTools.BasicCustomMenu.Create("ModConfigMenu", name + "Menu", title, false);
        }

        private void DestroyMenu()
        {
            GameObject.Destroy(menu.gameObject);
            menu = null;
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

            int i = 0;
            foreach (ConfigMenu configMenu in orderedMenus)
            {
                MenuTools.AddButton("ModConfigMenu", configMenu.name + "Button", configMenu.buttonText, i, () =>
                {
                    if (!(MenuSystem.CanInvoke))
                        return;

                    MenuTools.TransitionForward(modsMenu, configMenu.menu);
                }, false);
                ++i;
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

            foreach (ConfigMenu configMenu in ConfigMenus)
            {
                MenuTools.DestroyButton("ModConfigMenu", configMenu.name + "Button", false);
            }

            IsConfigMenuSystemActive = false;
        }
    }
}
