using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace Assets.Menus
{
    class AudioControlMenu : IMenu
    {
        private MenuController _menuController;

        public MenuDefinition BuildMenu(MenuController menuController)
        {
            _menuController = menuController;

            return new MenuDefinition
            {
                BackgroundFilename = "6audcon1",
                MenuItems = new MenuItem[] {
                    new MenuBlank(),
                    new MenuButton("Music Level", "3.00", Noop),
                    new MenuBlank(),
                    new MenuBlank(),
                    new MenuButton("SFX Level", "4.00", Noop),
                    new MenuBlank(),
                    new MenuBlank(),
                    new MenuButton("Voice Level", "10.00", Noop),
                    new MenuBlank(),
                    new MenuBlank(),
                    new MenuButton("Back", "", Back)
                }
            };
        }

        private void Noop()
        {
            throw new NotImplementedException();
        }
        
        public void Back()
        {
            _menuController.ShowMenu<OptionsMenu>();
        }
    }
}
