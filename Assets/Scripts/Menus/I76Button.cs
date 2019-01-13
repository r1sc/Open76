using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.Menus
{
    public class I76Button : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        public UnityAction OnActivate;
        public UnityAction OnDeactivate;
        
        private GameObject _subTextObject;
        private bool _activated;
        private Sprite _normalSprite;
        private Sprite _highlightSprite;
        private Image _buttonImage;
        private bool _activeLastFrame;

        public bool Blocked { get; set; }

        public void Setup(Transform parentTransform, Texture2D normalTexture, Texture2D highlightTexture, GameObject subTextObject, int x, int y)
        {
            _buttonImage = gameObject.AddComponent<Image>();
            _normalSprite = Sprite.Create(normalTexture, new Rect(0, 0, normalTexture.width, normalTexture.height), new Vector2(0.5f, 0.5f));
            _highlightSprite = Sprite.Create(highlightTexture, new Rect(0, 0, highlightTexture.width, highlightTexture.height), new Vector2(0.5f, 0.5f));
            _buttonImage.sprite = _normalSprite;
            _subTextObject = subTextObject;

            RectTransform rectTransform = _buttonImage.GetComponent<RectTransform>();
            rectTransform.anchorMax = new Vector2(0.0f, 0.0f);
            rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(normalTexture.width, normalTexture.height);
            rectTransform.anchoredPosition = new Vector2(x, y);
            rectTransform.SetParent(parentTransform);
        }

        private void OnDestroy()
        {
            if (_normalSprite != null)
            {
                Destroy(_normalSprite);
                _normalSprite = null;
            }

            if (_highlightSprite != null)
            {
                Destroy(_highlightSprite);
                _highlightSprite = null;
            }
        }

        private void Update()
        {
            if (!_activated || !Blocked)
            {
                return;
            }

            if (_activeLastFrame && Input.GetMouseButtonDown(0))
            {
                OnDeactivate?.Invoke();
                _activated = false;
                _activeLastFrame = false;
                _buttonImage.sprite = _normalSprite;
            }

            _activeLastFrame = _activated;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_activated || Blocked)
            {
                return;
            }

            OnActivate?.Invoke();

            _activated = true;
            _buttonImage.sprite = _highlightSprite;
            _subTextObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_activated || Blocked)
            {
                return;
            }

            _buttonImage.sprite = _highlightSprite;
            _subTextObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_activated || Blocked)
            {
                return;
            }

            _buttonImage.sprite = _normalSprite;
            _subTextObject.SetActive(false);
        }
    }
}
