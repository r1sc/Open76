namespace Assets.Scripts.Menus
{
    internal class OptionsMenu : IMenu
    {
        private MenuController _menuController;

        public MenuDefinition BuildMenu(MenuController menuController)
        {
            _menuController = menuController;

            return new MenuDefinition
            {
                BackgroundFilename = "6mainmn1",
                MenuItems = new MenuItem[] {
                    new MenuButton("Abort Mission", "$6.78", Back),
                    new MenuBlank(),
                    new MenuButton("Graphic Detail", "$8.98", menuController.ShowMenu<GraphicsMenu>),
                    new MenuButton("Audio Control", "$9.01", menuController.ShowMenu<AudioControlMenu>),
                    new MenuBlank(),
                    new MenuButton("Continue Mission", "", Back),
                }
            };
        }

        public void Back()
        {
            _menuController.CloseMenu();
        }
    }
}
