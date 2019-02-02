using System;
using UnityEngine;
using UnityEngine.XR;

namespace Assets.Scripts.Menus
{
    internal class GraphicsMenu : IMenu
    {
        private MenuController _menuController;

        public MenuDefinition BuildMenu(MenuController menuController)
        {
            _menuController = menuController;

            return new MenuDefinition
            {
                BackgroundFilename = "6grxdet1",
                MenuItems = new MenuItem[] {
                    new MenuButton("Screen Resolution", GetCurrentResolution(), NextResolution),
                    new MenuButton("Quality", GetCurrentQuality(), NextQuality),
                    new MenuBlank(),
                    new MenuButton("Virtual Reality", GetVRStatus(), ToggleVR),
                    new MenuBlank(),
                    new MenuButton("Cancel", "", Back)
                }
            };
        }

        private string GetVRStatus()
        {
            return XRSettings.enabled ? "On" : "Off";
        }

        private string GetCurrentResolution()
        {
            Resolution current = Screen.currentResolution;
            return current.width + "x" + current.height + "@" + current.refreshRate;
        }

        private string GetCurrentQuality()
        {
            int level = QualitySettings.GetQualityLevel();
            return QualitySettings.names[level];
        }

        private void NextResolution()
        {
            int nextIndex = (Array.IndexOf(Screen.resolutions, Screen.currentResolution) + 1) % Screen.resolutions.Length;
            Resolution newResolution = Screen.resolutions[nextIndex];
            Screen.SetResolution(newResolution.width, newResolution.height, false, newResolution.refreshRate);

            _menuController.Redraw();
        }

        private void NextQuality()
        {
            int nextLevel = (QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length;
            QualitySettings.SetQualityLevel(nextLevel);

            _menuController.Redraw();
        }

        private void ToggleVR()
        {
            XRSettings.enabled = !XRSettings.enabled;

            _menuController.Redraw();
        }

        public void Back()
        {
            _menuController.ShowMenu<OptionsMenu>();
        }
    }
}
