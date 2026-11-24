using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CricketBlitz.UI
{
    public static class UiKit
    {
        static Sprite white;
        static Font font;

        public static readonly Color Navy = new Color(0.05f, 0.09f, 0.14f, 0.94f);
        public static readonly Color Panel = new Color(0.1f, 0.16f, 0.24f, 0.96f);
        public static readonly Color Gold = new Color(0.95f, 0.74f, 0.22f, 1f);
        public static readonly Color Ink = new Color(0.94f, 0.96f, 0.97f, 1f);
        public static readonly Color Muted = new Color(0.7f, 0.76f, 0.8f, 1f);
        public static readonly Color Danger = new Color(0.75f, 0.24f, 0.18f, 1f);
        public static readonly Color Good = new Color(0.2f, 0.62f, 0.4f, 1f);

        public static Font Font
        {
            get
            {
                if (font == null)
                {
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (font == null)
                    {
                        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    }
                }

                return font;
            }
        }

        public static Sprite White
        {
            get
            {
                if (white == null)
                {
                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                    texture.Apply();
                    white = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 100f);
                }

                return white;
            }
        }

        public static Canvas CreateCanvas(string name, int sort)
        {
            GameObject go = new GameObject(name);
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sort;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform Box(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.sprite = White;
            image.color = color;
            image.raycastTarget = color.a > 0.2f;
            return go.GetComponent<RectTransform>();
        }

        public static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        public static void Place(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }

        public static Text Label(Transform parent, string contents, int size, TextAnchor anchor, Color color)
        {
            GameObject go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = Font;
            text.text = contents;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            RectTransform rect = text.rectTransform;
            Stretch(rect, Vector2.zero, Vector2.one);
            rect.offsetMin = new Vector2(8f, 4f);
            rect.offsetMax = new Vector2(-8f, -4f);
            return text;
        }

        public static Button Button(Transform parent, string label, Action onClick, int fontSize)
        {
            RectTransform rect = Box(parent, label + "Button", new Color(0.16f, 0.28f, 0.38f, 1f));
            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = Gold;
            colors.pressedColor = new Color(0.95f, 0.8f, 0.4f, 1f);
            colors.normalColor = Color.white;
            button.colors = colors;
            Text text = Label(rect, label, fontSize, TextAnchor.MiddleCenter, Ink);
            text.color = Ink;
            if (onClick != null)
            {
                button.onClick.AddListener(new UnityAction(onClick));
            }

            return button;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }

    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Action Down;
        public Action Up;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Down != null)
            {
                Down();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (Up != null)
            {
                Up();
            }
        }
    }

    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Vector2 Value;
        RectTransform stick;
        RectTransform rect;
        float radius = 70f;

        public static VirtualJoystick Create(Transform parent, string name, Vector2 anchor, Vector2 size)
        {
            RectTransform background = UiKit.Box(parent, name, new Color(1f, 1f, 1f, 0.12f));
            UiKit.Place(background, anchor, size, new Vector2(0.5f, 0.5f));
            VirtualJoystick joystick = background.gameObject.AddComponent<VirtualJoystick>();
            joystick.rect = background;
            joystick.radius = size.x * 0.38f;
            RectTransform knob = UiKit.Box(background, "Knob", new Color(0.95f, 0.74f, 0.22f, 0.95f));
            UiKit.Place(knob, new Vector2(0.5f, 0.5f), new Vector2(size.x * 0.38f, size.y * 0.38f), new Vector2(0.5f, 0.5f));
            joystick.stick = knob;
            return joystick;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out local);
            Vector2 clamped = Vector2.ClampMagnitude(local, radius);
            stick.anchoredPosition = clamped;
            Value = clamped / radius;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Value = Vector2.zero;
            stick.anchoredPosition = Vector2.zero;
        }
    }

    public sealed class DragAxis : MonoBehaviour, IDragHandler
    {
        public Action<float> OnDeltaX;

        public void OnDrag(PointerEventData eventData)
        {
            if (OnDeltaX != null)
            {
                OnDeltaX(eventData.delta.x / 90f);
            }
        }
    }
}
