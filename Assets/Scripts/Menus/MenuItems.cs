using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Menus
{
    public abstract class MenuItem { }

    public class MenuButton : MenuItem
    {
        public string Text { get; set; }
        public string Value { get; set; }
        public Action OnClick { get; set; }

        public MenuButton(string text, string value, Action onClick)
        {
            Text = text;
            Value = value;
            OnClick = onClick;
        }
    }

    public class MenuBlank : MenuItem
    {
    }
}
