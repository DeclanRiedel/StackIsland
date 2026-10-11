using TMPro;
using UnityEngine;

namespace StackIsland
{
    /// <summary>World-space text that always faces the camera.</summary>
    public class Billboard : MonoBehaviour
    {
        static Material _outline;

        /// <summary>One shared, thick-outlined font material for every world label (readable on bright sand).</summary>
        public static void ApplyOutline(TMP_Text t)
        {
            if (_outline == null)
            {
                var src = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Outline");
                if (src != null)
                {
                    _outline = new Material(src) { name = "WorldLabelOutline" };
                    _outline.SetFloat("_OutlineWidth", 0.32f);
                    _outline.SetColor("_OutlineColor", new Color(0.08f, 0.06f, 0.05f, 1f));
                    _outline.SetFloat("_FaceDilate", 0.12f);
                }
            }
            if (_outline != null) t.fontSharedMaterial = _outline;
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }

        public static TextMeshPro Label(Transform parent, Vector3 localPos, float size, Color color, string text = "", bool plate = false)
        {
            var go = new GameObject("label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.AddComponent<Billboard>();
            var t = go.AddComponent<TextMeshPro>();
            t.alignment = TextAlignmentOptions.Center;
            t.fontSize = size;
            t.color = color;
            t.text = text;
            t.fontStyle = FontStyles.Bold;
            ApplyOutline(t);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.rectTransform.sizeDelta = new Vector2(10, 3);
            t.renderer.sortingOrder = 2;
            if (plate) go.AddComponent<LabelPlate>();
            return t;
        }
    }

    /// <summary>A dark rounded plate behind a world label, sized to its text.</summary>
    public class LabelPlate : MonoBehaviour
    {
        TextMeshPro _text;
        SpriteRenderer _plate;
        string _last;
        public Color color = new Color(0.04f, 0.07f, 0.1f, 0.62f);

        void Awake()
        {
            _text = GetComponent<TextMeshPro>();
            var go = new GameObject("plate");
            go.transform.SetParent(transform, false);
            _plate = go.AddComponent<SpriteRenderer>();
            _plate.sprite = UIKit.Rounded;
            _plate.drawMode = SpriteDrawMode.Sliced;
            _plate.color = color;
            _plate.sortingOrder = 1;
            _plate.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void LateUpdate()
        {
            if (_text == null) return;
            if (_text.text == _last) return;
            _last = _text.text;
            _plate.enabled = !string.IsNullOrEmpty(_last);
            if (!_plate.enabled) return;
            _text.ForceMeshUpdate();
            var b = _text.textBounds;
            float pad = _text.fontSize * 0.06f;
            _plate.transform.localPosition = new Vector3(b.center.x, b.center.y, 0.02f);
            _plate.size = new Vector2(b.size.x + pad * 2f, b.size.y + pad * 1.4f);
        }
    }
}
