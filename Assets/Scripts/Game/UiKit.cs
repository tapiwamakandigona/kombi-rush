using UnityEngine;
using UnityEngine.UI;

namespace KombiRush.Game
{
    /// <summary>
    /// Builds uGUI from code. The whole interface is generated, so there are no prefabs to keep
    /// in sync and the layout is readable in one place.
    /// </summary>
    public static class UiKit
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);
        private static Font _font;

        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _font;
            }
        }

        public static Canvas CreateCanvas(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform Group(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            Stretch(rt);
            return rt;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>Anchored box. Anchors are 0..1 of the parent; size and offset are in reference pixels.</summary>
        public static RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 offset)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
            return rt;
        }

        public static Image Panel(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 offset, Color color)
        {
            RectTransform rt = Box(parent, name, anchor, pivot, size, offset);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = SpriteFactory.Panel;
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        public static Image Bar(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 offset, Color color)
        {
            Image image = Panel(parent, name, anchor, pivot, size, offset, color);
            image.type = Image.Type.Sliced;
            image.fillMethod = Image.FillMethod.Horizontal;
            return image;
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color,
            TextAnchor anchor, Vector2 anchorPoint, Vector2 pivot, Vector2 boxSize, Vector2 offset,
            FontStyle style = FontStyle.Bold)
        {
            RectTransform rt = Box(parent, name, anchorPoint, pivot, boxSize, offset);
            var label = rt.gameObject.AddComponent<Text>();
            label.font = Font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = anchor;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        public static Button TextButton(Transform parent, string name, string caption, Vector2 anchor, Vector2 pivot,
            Vector2 size, Vector2 offset, Color background, Color foreground, int fontSize, System.Action onClick)
        {
            Image image = Panel(parent, name, anchor, pivot, size, offset, background);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.06f;
            button.colors = colors;

            Text label = Label(image.transform, "Caption", caption, fontSize, foreground, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, Vector2.zero);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;

            if (onClick != null) button.onClick.AddListener(() => onClick());
            return button;
        }

        /// <summary>Formats metres the way a driver would read them.</summary>
        public static string Distance(float metres)
        {
            if (metres < 1000f) return Mathf.FloorToInt(metres).ToString() + " m";
            return (metres / 1000f).ToString("0.00") + " km";
        }
    }
}
