using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace StackIsland
{
    /// <summary>Tiny helpers to build uGUI in code (keeps the project free of hand-made UI prefabs).</summary>
    public static class UIKit
    {
        public static readonly Color PanelColor = new Color(0.07f, 0.12f, 0.18f, 0.92f);
        public static readonly Color PanelLight = new Color(0.14f, 0.22f, 0.3f, 0.95f);
        public static readonly Color Accent = new Color(0.98f, 0.78f, 0.25f);
        public static readonly Color ButtonColor = new Color(0.16f, 0.45f, 0.6f);
        public static readonly Color TextColor = new Color(0.95f, 0.96f, 0.92f);
        public static readonly Color Muted = new Color(0.65f, 0.72f, 0.78f);

        static Sprite _rounded, _circle;

        public static Sprite Rounded
        {
            get
            {
                if (_rounded == null) _rounded = MakeRounded(32, 10);
                return _rounded;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (_circle == null) _circle = MakeRounded(64, 32, false);
                return _circle;
            }
        }

        static Sprite MakeRounded(int size, int radius, bool sliced = true)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius), 0);
                    float dy = Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius), 0);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            tex.Apply();
            var border = sliced ? new Vector4(radius, radius, radius, radius) : Vector4.zero;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, border);
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static void Stretch(RectTransform rt, float pad = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
        }

        public static Image Panel(Transform parent, Color color, string name = "panel")
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Rounded;
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        public static TextMeshProUGUI Text(Transform parent, string text, float size, TextAlignmentOptions align = TextAlignmentOptions.Left, Color? color = null, string name = "text")
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color ?? TextColor;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        public static Button Button(Transform parent, string label, UnityAction onClick, float w = 200, float h = 44, Color? color = null, float fontSize = 22)
        {
            var img = Panel(parent, color ?? ButtonColor, "button");
            img.raycastTarget = true;
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(w, h);
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = w;
            le.preferredHeight = h;
            var b = img.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            b.colors = colors;
            if (onClick != null) b.onClick.AddListener(onClick);
            var t = Text(rt, label, fontSize, TextAlignmentOptions.Center);
            Stretch(t.rectTransform, 4);
            t.fontStyle = FontStyles.Bold;
            return b;
        }

        public static void SetLabel(Button b, string label)
        {
            var t = b.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.text = label;
        }

        public static VerticalLayoutGroup VLayout(GameObject go, float spacing = 8, int pad = 12, bool expandWidth = true)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(pad, pad, pad, pad);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = expandWidth;
            v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup HLayout(GameObject go, float spacing = 8, int pad = 0)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = new RectOffset(pad, pad, pad, pad);
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            h.childAlignment = TextAnchor.MiddleLeft;
            return h;
        }

        public static RectTransform Row(Transform parent, float height = 44, float spacing = 8)
        {
            var rt = Rect("row", parent);
            HLayout(rt.gameObject, spacing);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            return rt;
        }

        public static LayoutElement Size(Component c, float w = -1, float h = -1, float flex = -1)
        {
            var le = c.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            if (w >= 0) le.preferredWidth = w;
            if (h >= 0) { le.preferredHeight = h; le.minHeight = h; }
            if (flex >= 0) le.flexibleWidth = flex;
            return le;
        }

        /// <summary>Scrollable vertical list. Returns the content transform to add rows to.</summary>
        public static RectTransform Scroll(Transform parent, out ScrollRect scroll)
        {
            var view = Rect("scroll", parent);
            var img = view.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.15f);
            view.gameObject.AddComponent<RectMask2D>();
            scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 40;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var content = Rect("content", view);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            VLayout(content.gameObject, 6, 10);
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            scroll.viewport = view;
            return content;
        }

        public static Slider Slider(Transform parent, string label, float value, Action<float> onChange)
        {
            var row = Row(parent, 40);
            var t = Text(row, label, 22);
            Size(t, 260, 40);
            var sRt = Rect("slider", row);
            Size(sRt, 360, 24);
            var bg = sRt.gameObject.AddComponent<Image>();
            bg.sprite = Rounded; bg.type = Image.Type.Sliced; bg.color = new Color(0, 0, 0, 0.4f);
            var fillArea = Rect("fillArea", sRt); Stretch(fillArea, 2);
            var fill = Panel(fillArea, Accent, "fill");
            fill.rectTransform.sizeDelta = Vector2.zero;   // the slider drives its anchors; no extra size
            var handleArea = Rect("handleArea", sRt); Stretch(handleArea);
            var handle = Rect("handle", handleArea);
            var hImg = handle.gameObject.AddComponent<Image>();
            hImg.sprite = Circle; hImg.color = Color.white;
            handle.sizeDelta = new Vector2(28, 28);
            var s = sRt.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform;
            s.handleRect = handle;
            s.targetGraphic = hImg;
            s.minValue = 0; s.maxValue = 1;
            s.value = value;
            s.onValueChanged.AddListener(v => onChange(v));
            return s;
        }

        public static Toggle Toggle(Transform parent, string label, bool value, Action<bool> onChange)
        {
            var row = Row(parent, 40);
            var boxRt = Rect("box", row);
            Size(boxRt, 32, 32);
            var box = boxRt.gameObject.AddComponent<Image>();
            box.sprite = Rounded; box.type = Image.Type.Sliced; box.color = new Color(0, 0, 0, 0.45f);
            var check = Panel(boxRt, Accent, "check");
            Stretch(check.rectTransform, 6);
            var t = Text(row, label, 22);
            Size(t, 520, 36);
            var tg = boxRt.gameObject.AddComponent<Toggle>();
            tg.targetGraphic = box;
            tg.graphic = check;
            tg.isOn = value;
            tg.onValueChanged.AddListener(v => onChange(v));
            return tg;
        }

        public static TextMeshProUGUI Header(Transform parent, string text, float size = 34)
        {
            var t = Text(parent, text, size, TextAlignmentOptions.Left, Accent, "header");
            t.fontStyle = FontStyles.Bold;
            Size(t, -1, size + 14);
            return t;
        }
    }
}
