namespace Assets.Scripts.Menus
{
    public interface IMenu
    {
        MenuDefinition BuildMenu(MenuController menuController);
        void Back();
    }
}
