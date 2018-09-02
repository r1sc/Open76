using Assets.System;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Menus
{
    public class MenuController : MonoBehaviour
    {

        public Button MenuButtonPrefab;
        public Transform BlankSeparatorPrefab;

        public VerticalLayoutGroup Items;
        public RawImage Background;
        private CacheManager _cacheManager;
        private CanvasGroup _canvasGroup;

        private IMenu _currentMenu;

        // Use this for initialization
        void Start()
        {
            _cacheManager = CacheManager.Instance;

            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_currentMenu != null)
                    _currentMenu.Back();
                else
                    ShowMenu<OptionsMenu>();
            }
        }

        public void CloseMenu()
        {
            _currentMenu = null;

            _canvasGroup.alpha = 0;
            Time.timeScale = 1;
        }

        public void ShowMenu<T>() where T : IMenu, new()
        {
            _currentMenu = new T();
            EventSystem.current.SetSelectedGameObject(null);

            Redraw();

            _canvasGroup.alpha = 1;

            Time.timeScale = 0;
        }

        public void Redraw()
        {
            var menuDefinition = _currentMenu.BuildMenu(this);

            var texture = _cacheManager.GetTexture(menuDefinition.BackgroundFilename);
            Background.texture = texture;
            Background.rectTransform.sizeDelta = new Vector2(texture.width, texture.height);

            var selectedIndex = EventSystem.current.currentSelectedGameObject == null ? 0 : EventSystem.current.currentSelectedGameObject.transform.GetSiblingIndex();

            foreach (Transform child in Items.transform)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < menuDefinition.MenuItems.Length; i++)
            {
                var menuItem = menuDefinition.MenuItems[i];
                if (menuItem is MenuButton)
                {
                    var menuButton = menuItem as MenuButton;
                    var button = Instantiate(MenuButtonPrefab, Items.transform);
                    button.transform.Find("TextContainer").GetComponentInChildren<Text>().text = menuButton.Text;
                    button.transform.Find("Value").GetComponent<Text>().text = menuButton.Value;
                    button.onClick.AddListener(new UnityEngine.Events.UnityAction(menuButton.OnClick));

                    if (selectedIndex == i)
                        EventSystem.current.SetSelectedGameObject(button.gameObject);
                }
                else if (menuItem is MenuBlank)
                {
                    Instantiate(BlankSeparatorPrefab, Items.transform);
                }
            }
        }
    }
}