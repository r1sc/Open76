using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.System;
using Assets.Scripts.System.Fileparsers;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Assets.Scripts.Menus
{
    public class MainMenu : MonoBehaviour
    {
        private Game _game;
        private List<Sprite> _sprites;
        private List<Texture2D> _textures;
        private List<I76Button> _buttons;
        private RawImage _videoImage;
        private GameObject _tripSubButtons;
        private GameObject _meleeSubButtons;

        public GameObject VideoCanvasObject;
        public GameObject MenuCanvasObject;

#if UNITY_EDITOR
        public string GamePath;
#endif

        private enum Images
        {
            Options_Highlight,
            Options_Normal,
            AutoMelee_Highlight,
            AutoMelee_Normal,
            Melee_Highlight,
            Melee_Normal,
            Host_Highlight,
            Host_Normal,
            InstantMelee_Highlight,
            InstantMelee_Normal,
            Join_Highlight,
            Join_Normal,
            Modem_Highlight,
            Modem_Normal,
            Ipx_Highlight,
            Ipx_Normal,
            NullModem_Highlight,
            NullModem_Normal,
            Scenario_Highlight,
            Scenario_Normal,
            LoadBookmark_Highlight,
            LoadBookmark_Normal,
            NewTrip_Highlight,
            NewTrip_Normal,
            Trip_Highlight,
            Trip_Normal,
            Internet_Highlight,
            Internet_Normal,
            MultiMelee_Highlight,
            MultiMelee_Normal,
            Exit_Highlight,
            Exit_Normal,
            Training_Highlight,
            Training_Normal,
            Trip_SubText,
            NewTrip_SubText,
            LoadBookmark_SubText,
            Melee_SubText,
            AutoMelee_SubText,
            Unused1,
            Unused2,
            MultiMelee_SubText,
            Options_SubText,
            Exit_SubText,
            Training_SubText,
            Other_Highlight,
            Other_Normal
        }

        private void AddBackground(Texture2D texture, int x, int y)
        {
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            GameObject imageObj = new GameObject("Background");
            Image image = imageObj.AddComponent<Image>();
            image.sprite = sprite;

            RectTransform rectTransform = image.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(texture.width, texture.height);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(x, y);
            rectTransform.SetParent(MenuCanvasObject.transform);

            _sprites.Add(sprite);
        }

        private void Awake()
        {
            _sprites = new List<Sprite>();
            _textures = new List<Texture2D>();
            _buttons = new List<I76Button>();

            _videoImage = VideoCanvasObject.transform.Find("VideoImage").GetComponent<RawImage>();

            VideoCanvasObject.SetActive(false);
            MenuCanvasObject.SetActive(false);
            
            _game = Game.Instance;
#if UNITY_EDITOR
            _game.GamePath = GamePath;
            _game.IntroPlayed = true;
#endif

            if (_game.IntroPlayed)
            {
                ShowMainMenu();
            }
            else
            {
                StartCoroutine(ShowIntro());
            }
        }
        
        private void OnDestroy()
        {
            int spriteCount = _sprites.Count;
            for (int i = 0; i < spriteCount; ++i)
            {
                Destroy(_sprites[i]);
            }

            int textureCount = _textures.Count;
            for (int i = 0; i < textureCount; ++i)
            {
                Destroy(_textures[i]);
            }

            if (_videoImage.texture != null)
            {
                Destroy(_videoImage.texture);
                _videoImage.texture = null;
            }

            _sprites.Clear();
            _textures.Clear();
        }

        private I76Button AddButton(Images normal, Images highlight, Images subText, int x, int y, UnityAction buttonAction)
        {
            Texture2D normalTexture = _textures[(int) normal];
            Texture2D highlightTexture = _textures[(int)highlight];
            Texture2D subTextTexture = _textures[(int)subText];
            
            GameObject subTextObject = new GameObject("SubTextObject");
            Image subTextImage = subTextObject.AddComponent<Image>();
            subTextImage.raycastTarget = false;
            subTextImage.sprite = Sprite.Create(subTextTexture, new Rect(0, 0, subTextTexture.width, subTextTexture.height), new Vector2(0.5f, 0.5f));
            _sprites.Add(subTextImage.sprite);
            subTextObject.SetActive(false);
            RectTransform subTextTransform = subTextObject.GetComponent<RectTransform>();
            subTextTransform.SetParent(MenuCanvasObject.transform);
            subTextTransform.sizeDelta = new Vector2(subTextTexture.width, subTextTexture.height);
            subTextTransform.anchorMin = new Vector2(0.0f, 0.0f);
            subTextTransform.anchorMax = new Vector2(0.0f, 0.0f);
            subTextTransform.pivot = new Vector2(0.5f, 0.5f);
            subTextTransform.anchoredPosition = new Vector2(320f,subTextTexture.height);

            GameObject buttonObj = new GameObject("Button");
            I76Button button = buttonObj.AddComponent<I76Button>();
            button.Setup(MenuCanvasObject.transform, normalTexture, highlightTexture, subTextObject, x, 480 - y);

            button.OnActivate += OnButtonActivated;
            button.OnDeactivate += OnButtonDeactivated;
            if (buttonAction != null)
            {
                button.OnActivate += buttonAction;
            }
            
            return button;
        }

        private void OnTrip()
        {
            _tripSubButtons.SetActive(true);
        }

        private void OnMelee()
        {
            _meleeSubButtons.SetActive(true);
        }

        private void OnTraining()
        {
            StartCoroutine(LevelLoader.Instance.LoadLevel("a01.msn"));
        }

        private void OnNewTrip()
        {
            StartCoroutine(LevelLoader.Instance.LoadLevel("t01.msn"));
        }

        private void OnButtonActivated()
        {
            int buttonCount = _buttons.Count;
            for (int i = 0; i < buttonCount; ++i)
            {
                _buttons[i].Blocked = true;
            }
        }

        private void OnButtonDeactivated()
        {
            _tripSubButtons.SetActive(false);
            _meleeSubButtons.SetActive(false);

            int buttonCount = _buttons.Count;
            for (int i = 0; i < buttonCount; ++i)
            {
                _buttons[i].Blocked = false;
            }
        }

        private void OnExit()
        {
            if (Application.isEditor)
            {
                EditorApplication.isPlaying = false;
            }
            else
            {
                Application.Quit();
            }
        }

        private void ShowMainMenu()
        {
            VideoCanvasObject.SetActive(false);
            MenuCanvasObject.SetActive(true);

            Texture2D menuBackground = Mw2Parser.GetBackground(Mw2Parser.Background.MainMenu);
            Color32[] palette = Mw2Parser.GetBackgroundPalette(Mw2Parser.Background.MainMenu);
            Texture2D[] menuImages = Mw2Parser.GetTextureSet(Mw2Parser.TextureSet.MainMenu, palette);

            if (menuBackground == null || menuImages == null)
            {
                return;
            }

            AddBackground(menuBackground, Screen.width / 2, Screen.height / 2);

            for (int i = 0; i < menuImages.Length; ++i)
            {
                _textures.Add(menuImages[i]);
            }
            _textures.Add(menuBackground);

            _buttons = new List<I76Button>
            {
                AddButton(Images.Trip_Normal, Images.Trip_Highlight, Images.Trip_SubText, 200, 310, OnTrip),
                AddButton(Images.Melee_Normal, Images.Melee_Highlight, Images.Melee_SubText, 440, 310, OnMelee),
                AddButton(Images.Options_Normal, Images.Options_Highlight, Images.Options_SubText, 65, 30, null),
                AddButton(Images.Exit_Normal, Images.Exit_Highlight, Images.Exit_SubText, 595, 30, OnExit)
            };

            _tripSubButtons = new GameObject("Trip Buttons");
            RectTransform tripRectTransform = _tripSubButtons.AddComponent<RectTransform>();
            _tripSubButtons.SetActive(false);
            tripRectTransform.SetParent(MenuCanvasObject.transform);
            tripRectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            tripRectTransform.anchorMax = new Vector2(0.0f, 0.0f);
            tripRectTransform.pivot = new Vector2(0.0f, 0.0f);
            tripRectTransform.anchoredPosition = new Vector2(0.0f, 0.0f);
            tripRectTransform.sizeDelta = new Vector2(640.0f, 480.0f);

            AddButton(Images.Training_Normal, Images.Training_Highlight, Images.Training_SubText, 100, 350, OnTraining).transform.SetParent(_tripSubButtons.transform);
            AddButton(Images.NewTrip_Normal, Images.NewTrip_Highlight, Images.NewTrip_SubText, 170, 350, OnNewTrip).transform.SetParent(_tripSubButtons.transform);
            AddButton(Images.LoadBookmark_Normal, Images.LoadBookmark_Highlight, Images.LoadBookmark_SubText, 265, 350, null).transform.SetParent(_tripSubButtons.transform);

            _meleeSubButtons = new GameObject("Melee Buttons");
            RectTransform meleeRectTransform = _meleeSubButtons.AddComponent<RectTransform>();
            _meleeSubButtons.SetActive(false);
            meleeRectTransform.SetParent(MenuCanvasObject.transform);
            meleeRectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            meleeRectTransform.anchorMax = new Vector2(0.0f, 0.0f);
            meleeRectTransform.pivot = new Vector2(0.0f, 0.0f);
            meleeRectTransform.anchoredPosition = new Vector2(0.0f, 0.0f);
            meleeRectTransform.sizeDelta = new Vector2(640.0f, 480.0f);

            AddButton(Images.MultiMelee_Normal, Images.MultiMelee_Highlight, Images.MultiMelee_SubText, 390, 350, null).transform.SetParent(_meleeSubButtons.transform);
            AddButton(Images.AutoMelee_Normal, Images.AutoMelee_Highlight, Images.AutoMelee_SubText, 475, 350, null).transform.SetParent(_meleeSubButtons.transform);
        }

        private IEnumerator ShowIntro()
        {
            VideoCanvasObject.SetActive(true);

            using (VideoPlayer videoPlayer = new VideoPlayer("INTROF01.SMK"))
            {
                videoPlayer.OutputImage = _videoImage;
                yield return videoPlayer.PlayVideo();
            }

            using (VideoPlayer videoPlayer = new VideoPlayer("CREDF01.SMK"))
            {
                videoPlayer.OutputImage = _videoImage;
                yield return videoPlayer.PlayVideo();
            }

            ShowMainMenu();
        }
    }
}