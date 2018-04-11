using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace Assets.Menus
{
    class GraphicsMenu : IMenu
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
            var current = Screen.currentResolution;
            return current.width + "x" + current.height + "@" + current.refreshRate;
        }

        private string GetCurrentQuality()
        {
            var level = QualitySettings.GetQualityLevel();
            return QualitySettings.names[level];
        }

        private void NextResolution()
        {
            var nextIndex = (Array.IndexOf(Screen.resolutions, Screen.currentResolution) + 1) % Screen.resolutions.Length;
            var newResolution = Screen.resolutions[nextIndex];
            Screen.SetResolution(newResolution.width, newResolution.height, false, newResolution.refreshRate);

            _menuController.Redraw();
        }

        private void NextQuality()
        {
            var nextLevel = (QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length;
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
