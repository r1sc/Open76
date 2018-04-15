using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Menus
{
    public interface IMenu
    {
        MenuDefinition BuildMenu(MenuController menuController);
        void Back();
    }
}
