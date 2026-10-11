using System.Collections.Generic;
using UnityEngine;

namespace StackIsland
{
    /// <summary>
    /// Spawns library models normalised to a target size: centred on x/z, resting on y = 0.
    /// Missing models fall back to a tinted cube so the game is always playable.
    /// </summary>
    public static class Visuals
    {
        static ArtLibrary _art;
        static readonly Dictionary<string, Bounds> BoundsCache = new Dictionary<string, Bounds>();
        static readonly MaterialPropertyBlock Mpb = new MaterialPropertyBlock();

        public static void Init(ArtLibrary art)
        {
            _art = art;
            BoundsCache.Clear();
        }

        public static GameObject Spawn(string key, Transform parent, float size, Color tint, bool fitHeight = false, float maxHeight = 0f)
        {
            var root = new GameObject(string.IsNullOrEmpty(key) ? "visual" : key);
            root.transform.SetParent(parent, false);

            GameObject prefab = _art != null ? _art.Get(key) : null;
            GameObject model;
            Bounds b;
            if (prefab != null)
            {
                model = Object.Instantiate(prefab);
                model.transform.position = Vector3.zero;
                model.transform.rotation = Quaternion.identity;
                if (!BoundsCache.TryGetValue(key, out b))
                {
                    b = WorldBounds(model);
                    BoundsCache[key] = b;
                }
            }
            else
            {
                model = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(model.GetComponent<Collider>());
                model.GetComponent<Renderer>().sharedMaterial = Mats.Lit(tint == Color.white ? new Color(0.9f, 0.4f, 0.8f) : tint);
                b = new Bounds(Vector3.zero, Vector3.one);
                tint = Color.white;
            }

            float extent = fitHeight ? b.size.y : Mathf.Max(b.size.x, b.size.z);
            float s = extent > 0.0001f ? size / extent : 1f;
            if (maxHeight > 0 && b.size.y * s > maxHeight) s = maxHeight / b.size.y;   // keep towers from hiding the player
            model.transform.localScale = model.transform.localScale * s;
            model.transform.position = new Vector3(-b.center.x * s, -b.min.y * s, -b.center.z * s);
            model.transform.SetParent(root.transform, false);
            if (tint != Color.white) Tint(model, tint);
            return root;
        }

        public static Bounds WorldBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        /// <summary>Bounds of a spawned visual relative to its root's parent space (approximate, ignores rotation).</summary>
        public static Bounds LocalBounds(GameObject root)
        {
            var wb = WorldBounds(root);
            var t = root.transform.parent != null ? root.transform.parent : root.transform;
            var c = t.InverseTransformPoint(wb.center);
            var s = wb.size;
            var ls = t.lossyScale;
            s = new Vector3(s.x / Mathf.Max(0.0001f, ls.x), s.y / Mathf.Max(0.0001f, ls.y), s.z / Mathf.Max(0.0001f, ls.z));
            return new Bounds(c, s);
        }

        public static void Tint(GameObject go, Color tint)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                r.GetPropertyBlock(Mpb);
                Mpb.SetColor("_BaseColor", tint);
                r.SetPropertyBlock(Mpb);
            }
        }

        public static GameObject Primitive(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Material mat, string name = null)
        {
            var go = GameObject.CreatePrimitive(type);
            if (name != null) go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        public static void SetShadows(GameObject go, bool cast)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = cast ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
