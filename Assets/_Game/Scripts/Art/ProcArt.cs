using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace StackIsland
{
    /// <summary>
    /// Art made in code, in the chunky "idle tycoon" style: meat slices, cash bundles, the dome grill, counters,
    /// trays and everything painted on the floor (rounded signs, dashed buy outlines, big letters).
    /// </summary>
    public static class ProcArt
    {
        // ------------------------------------------------------------------ meshes

        static readonly Dictionary<string, Mesh> MeshCache = new Dictionary<string, Mesh>();

        /// <summary>Points around a rounded rectangle in the XZ plane (counter-clockwise seen from above).</summary>
        static List<Vector2> RoundedOutline(float w, float h, float r, int seg)
        {
            r = Mathf.Min(r, w * 0.5f, h * 0.5f);
            var pts = new List<Vector2>();
            var centres = new[]
            {
                new Vector2(w * 0.5f - r, h * 0.5f - r), new Vector2(-w * 0.5f + r, h * 0.5f - r),
                new Vector2(-w * 0.5f + r, -h * 0.5f + r), new Vector2(w * 0.5f - r, -h * 0.5f + r),
            };
            for (int c = 0; c < 4; c++)
                for (int i = 0; i <= seg; i++)
                {
                    float a = (c * 90f + i * 90f / seg) * Mathf.Deg2Rad;
                    pts.Add(centres[c] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
                }
            return pts;
        }

        /// <summary>Flat rounded rectangle facing up, centred on the origin.</summary>
        public static Mesh RoundedRect(float w, float h, float r, int seg = 6)
        {
            string key = $"rr{w:0.00}|{h:0.00}|{r:0.00}";
            if (MeshCache.TryGetValue(key, out var m) && m != null) return m;
            var outline = RoundedOutline(w, h, r, seg);
            var verts = new List<Vector3> { Vector3.zero };
            foreach (var p in outline) verts.Add(new Vector3(p.x, 0, p.y));
            var tris = new List<int>();
            for (int i = 0; i < outline.Count; i++)
            {
                tris.Add(0);
                tris.Add(1 + (i + 1) % outline.Count);
                tris.Add(1 + i);
            }
            m = new Mesh { name = key };
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            var n = new Vector3[verts.Count];
            for (int i = 0; i < n.Length; i++) n[i] = Vector3.up;
            m.normals = n;
            m.RecalculateBounds();
            MeshCache[key] = m;
            return m;
        }

        /// <summary>Raised rounded slab (a sign lying on the floor) with sides, so it catches light like a tile.</summary>
        public static Mesh RoundedSlab(float w, float h, float r, float thick, int seg = 6)
        {
            string key = $"rs{w:0.00}|{h:0.00}|{r:0.00}|{thick:0.00}";
            if (MeshCache.TryGetValue(key, out var m) && m != null) return m;
            var o = RoundedOutline(w, h, r, seg);
            var verts = new List<Vector3>();
            var tris = new List<int>();
            verts.Add(new Vector3(0, thick, 0));
            foreach (var p in o) verts.Add(new Vector3(p.x, thick, p.y));
            for (int i = 0; i < o.Count; i++) { tris.Add(0); tris.Add(1 + (i + 1) % o.Count); tris.Add(1 + i); }
            int side = verts.Count;
            foreach (var p in o) { verts.Add(new Vector3(p.x, thick, p.y)); verts.Add(new Vector3(p.x, 0, p.y)); }
            for (int i = 0; i < o.Count; i++)
            {
                int a = side + i * 2, b = side + ((i + 1) % o.Count) * 2;
                tris.Add(a); tris.Add(b); tris.Add(a + 1);
                tris.Add(b); tris.Add(b + 1); tris.Add(a + 1);
            }
            m = new Mesh { name = key };
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            MeshCache[key] = m;
            return m;
        }

        /// <summary>Dashed outline of a rounded rectangle (the "buy here" marking).</summary>
        public static Mesh DashedOutline(float w, float h, float r, float thick, float dash, float gap)
        {
            string key = $"do{w:0.00}|{h:0.00}|{r:0.00}|{thick:0.00}|{dash:0.00}|{gap:0.00}";
            if (MeshCache.TryGetValue(key, out var m) && m != null) return m;
            var o = RoundedOutline(w, h, r, 8);
            o.Add(o[0]);
            // a whole number of evenly spaced dashes around the perimeter, each drawn as a few short quads
            var cum = new float[o.Count];
            for (int i = 1; i < o.Count; i++) cum[i] = cum[i - 1] + (o[i] - o[i - 1]).magnitude;
            float perimeter = cum[o.Count - 1];
            Vector2 PointAt(float d)
            {
                d = Mathf.Repeat(d, perimeter);
                for (int i = 1; i < o.Count; i++)
                    if (d <= cum[i])
                    {
                        float seg = cum[i] - cum[i - 1];
                        return seg < 1e-5f ? o[i] : Vector2.Lerp(o[i - 1], o[i], (d - cum[i - 1]) / seg);
                    }
                return o[0];
            }
            var verts = new List<Vector3>();
            var tris = new List<int>();
            int dashes = Mathf.Max(4, Mathf.RoundToInt(perimeter / (dash + gap)));
            float slot = perimeter / dashes, on = slot * dash / (dash + gap);
            const int pieces = 4;
            for (int k = 0; k < dashes; k++)
            {
                for (int s = 0; s < pieces; s++)
                {
                    var p0 = PointAt(k * slot + on * s / pieces);
                    var p1 = PointAt(k * slot + on * (s + 1) / pieces);
                    var dir = p1 - p0;
                    if (dir.sqrMagnitude < 1e-8f) continue;
                    dir.Normalize();
                    var nrm = new Vector2(-dir.y, dir.x) * thick * 0.5f;
                    int v = verts.Count;
                    verts.Add(new Vector3(p0.x + nrm.x, 0, p0.y + nrm.y));
                    verts.Add(new Vector3(p0.x - nrm.x, 0, p0.y - nrm.y));
                    verts.Add(new Vector3(p1.x + nrm.x, 0, p1.y + nrm.y));
                    verts.Add(new Vector3(p1.x - nrm.x, 0, p1.y - nrm.y));
                    tris.Add(v); tris.Add(v + 2); tris.Add(v + 1);
                    tris.Add(v + 1); tris.Add(v + 2); tris.Add(v + 3);
                    tris.Add(v); tris.Add(v + 1); tris.Add(v + 2);
                    tris.Add(v + 1); tris.Add(v + 3); tris.Add(v + 2);
                }
            }
            m = new Mesh { name = key };
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            var n = new Vector3[verts.Count];
            for (int i = 0; i < n.Length; i++) n[i] = Vector3.up;
            m.normals = n;
            m.RecalculateBounds();
            MeshCache[key] = m;
            return m;
        }

        public static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material mat, Vector3 localPos, bool shadows = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        static GameObject Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Color c, bool shadows = true, string name = "part")
        {
            var go = Visuals.Primitive(type, parent, pos, scale, Mats.Lit(c), name);
            if (shadows) go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }

        // ------------------------------------------------------------------ items

        public static readonly Color MeatRed = new Color(0.86f, 0.28f, 0.3f);
        public static readonly Color MeatPink = new Color(1f, 0.72f, 0.7f);
        public static readonly Color CashGreen = new Color(0.33f, 0.78f, 0.36f);

        /// <summary>A thick slice of crab meat: red disc with a pale rim and a marbled centre.</summary>
        public static GameObject MeatSlice(Transform parent)
        {
            var root = new GameObject("meat");
            root.transform.SetParent(parent, false);
            Part(PrimitiveType.Cylinder, root.transform, new Vector3(0, 0.05f, 0), new Vector3(0.5f, 0.05f, 0.42f), MeatPink, false);
            Part(PrimitiveType.Cylinder, root.transform, new Vector3(0, 0.055f, 0), new Vector3(0.44f, 0.052f, 0.36f), MeatRed, false);
            Part(PrimitiveType.Cylinder, root.transform, new Vector3(0.06f, 0.06f, 0.03f), new Vector3(0.14f, 0.05f, 0.1f), MeatPink, false);
            return root;
        }

        /// <summary>Grilled crab cake: golden disc with grill marks.</summary>
        public static GameObject CrabCake(Transform parent)
        {
            var root = new GameObject("cake");
            root.transform.SetParent(parent, false);
            Part(PrimitiveType.Cylinder, root.transform, new Vector3(0, 0.06f, 0), new Vector3(0.48f, 0.06f, 0.48f), new Color(0.82f, 0.5f, 0.22f), false);
            Part(PrimitiveType.Cylinder, root.transform, new Vector3(0, 0.07f, 0), new Vector3(0.42f, 0.062f, 0.42f), new Color(0.95f, 0.66f, 0.3f), false);
            var mark = new Color(0.45f, 0.22f, 0.1f);
            for (int i = -1; i <= 1; i++)
            {
                var g = Part(PrimitiveType.Cube, root.transform, new Vector3(i * 0.11f, 0.13f, 0), new Vector3(0.035f, 0.01f, 0.34f), mark, false);
                g.transform.localRotation = Quaternion.Euler(0, 35, 0);
            }
            return root;
        }

        /// <summary>A bundle of green bills with a paper band.</summary>
        public static GameObject CashBundle(Transform parent)
        {
            var root = new GameObject("cash");
            root.transform.SetParent(parent, false);
            Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.06f, 0), new Vector3(0.56f, 0.12f, 0.28f), CashGreen, false);
            Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.06f, 0), new Vector3(0.1f, 0.125f, 0.285f), new Color(0.95f, 0.97f, 0.9f), false);
            Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.122f, 0), new Vector3(0.44f, 0.004f, 0.2f), new Color(0.24f, 0.62f, 0.28f), false);
            return root;
        }

        // ------------------------------------------------------------------ props

        /// <summary>Clay dome grill (like a pizza oven) with a dark mouth and a chimney.</summary>
        public static GameObject DomeOven(Transform parent, out Transform mouth, out Transform chimneyTop)
        {
            var root = new GameObject("oven");
            root.transform.SetParent(parent, false);
            var clay = new Color(0.62f, 0.3f, 0.24f);
            var clayDark = new Color(0.5f, 0.23f, 0.19f);
            Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.45f, 0), new Vector3(3.2f, 0.9f, 2.6f), clayDark);
            Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.95f, 0), new Vector3(3.4f, 0.12f, 2.8f), clay);
            Part(PrimitiveType.Sphere, root.transform, new Vector3(0, 1.0f, 0), new Vector3(2.4f, 2.4f, 2.4f), clay);
            for (int i = 0; i < 3; i++)
                Part(PrimitiveType.Cylinder, root.transform, new Vector3(0, 1.4f + i * 0.35f, 0), new Vector3(2.0f - i * 0.5f, 0.04f, 2.0f - i * 0.5f), clayDark, false);
            var m = Part(PrimitiveType.Cylinder, root.transform, new Vector3(0, 1.35f, -1.05f), new Vector3(0.95f, 0.08f, 0.95f), new Color(0.12f, 0.06f, 0.05f), false);
            m.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var glow = Visuals.Primitive(PrimitiveType.Cylinder, root.transform, new Vector3(0, 1.3f, -1.0f), new Vector3(0.7f, 0.05f, 0.5f), Mats.Glow(new Color(1f, 0.55f, 0.15f), 1.6f), "fire");
            glow.transform.localRotation = Quaternion.Euler(90, 0, 0);
            mouth = m.transform;
            var chimney = Part(PrimitiveType.Cylinder, root.transform, new Vector3(0.7f, 2.25f, 0.4f), new Vector3(0.38f, 0.35f, 0.38f), clayDark);
            chimneyTop = new GameObject("chimneyTop").transform;
            chimneyTop.SetParent(root.transform, false);
            chimneyTop.localPosition = new Vector3(0.7f, 2.65f, 0.4f);
            _ = chimney;
            return root;
        }

        /// <summary>Wooden counter table running along local X.</summary>
        public static GameObject Table(Transform parent, float length, float depth = 1.2f, float height = 0.95f)
        {
            var root = new GameObject("table");
            root.transform.SetParent(parent, false);
            var wood = new Color(0.66f, 0.43f, 0.27f);
            var woodDark = new Color(0.5f, 0.31f, 0.2f);
            Part(PrimitiveType.Cube, root.transform, new Vector3(0, height - 0.06f, 0), new Vector3(length, 0.12f, depth), wood);
            for (int i = 0; i < 3; i++)
                Part(PrimitiveType.Cube, root.transform, new Vector3(0, height - 0.003f, -depth * 0.33f + i * depth * 0.33f), new Vector3(length * 0.98f, 0.01f, 0.03f), woodDark, false);
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    Part(PrimitiveType.Cube, root.transform, new Vector3(sx * (length * 0.5f - 0.15f), (height - 0.12f) * 0.5f, sz * (depth * 0.5f - 0.15f)), new Vector3(0.14f, height - 0.12f, 0.14f), woodDark);
            return root;
        }

        /// <summary>Low metal tray on the floor where a station takes in or puts out items.</summary>
        public static GameObject Tray(Transform parent, float w, float d, Color rim)
        {
            var root = new GameObject("tray");
            root.transform.SetParent(parent, false);
            Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.06f, 0), new Vector3(w, 0.12f, d), new Color(0.42f, 0.24f, 0.2f));
            Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.125f, 0), new Vector3(w - 0.2f, 0.02f, d - 0.2f), rim, false);
            return root;
        }

        // ------------------------------------------------------------------ walls & fences

        public static readonly Color WallRed = new Color(0.93f, 0.42f, 0.36f);
        public static readonly Color WallCap = new Color(1f, 0.82f, 0.68f);
        public static readonly Color CageBlue = new Color(0.22f, 0.56f, 0.92f);

        /// <summary>Thick low wall with a light top edge (the bright "tycoon" look), running from a to b in world space.</summary>
        public static GameObject LowWall(Transform parent, Vector3 a, Vector3 b, float height = 0.9f, float thick = 0.7f, Color? wall = null)
        {
            var d = b - a;
            d.y = 0;
            float len = d.magnitude;
            var root = new GameObject("wall");
            root.transform.SetParent(parent, false);
            root.transform.position = (a + b) * 0.5f;
            root.transform.rotation = Quaternion.LookRotation(d.normalized);
            var wc = wall ?? WallRed;
            Part(PrimitiveType.Cube, root.transform, new Vector3(0, height * 0.5f, 0), new Vector3(thick, height, len + thick), wc);
            Part(PrimitiveType.Cube, root.transform, new Vector3(0, height + 0.06f, 0), new Vector3(thick + 0.12f, 0.12f, len + thick + 0.12f), WallCap);
            // darker band near the floor
            Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.08f, 0), new Vector3(thick + 0.04f, 0.16f, len + thick + 0.04f), wc * 0.8f, false);
            return root;
        }

        /// <summary>Blue grid cage fence from a to b (posts, rails and thin bars).</summary>
        public static GameObject CageFence(Transform parent, Vector3 a, Vector3 b, float height = 1.6f, Color? color = null)
        {
            var d = b - a;
            d.y = 0;
            float len = d.magnitude;
            var root = new GameObject("cage");
            root.transform.SetParent(parent, false);
            root.transform.position = (a + b) * 0.5f;
            root.transform.rotation = Quaternion.LookRotation(d.normalized);
            var blue = color ?? CageBlue;
            var light = Color.Lerp(blue, Color.white, 0.25f);
            Part(PrimitiveType.Cube, root.transform, new Vector3(0, height, 0), new Vector3(0.12f, 0.1f, len), blue);
            Part(PrimitiveType.Cube, root.transform, new Vector3(0, height * 0.5f, 0), new Vector3(0.08f, 0.06f, len), light, false);
            Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.12f, 0), new Vector3(0.1f, 0.08f, len), blue, false);
            int posts = Mathf.Max(1, Mathf.RoundToInt(len / 2.4f));
            for (int i = 0; i <= posts; i++)
                Part(PrimitiveType.Cube, root.transform, new Vector3(0, height * 0.5f, -len * 0.5f + len * i / posts), new Vector3(0.16f, height, 0.16f), blue);
            int bars = Mathf.Max(2, Mathf.RoundToInt(len / 0.35f));
            for (int i = 0; i <= bars; i++)
                Part(PrimitiveType.Cube, root.transform, new Vector3(0, height * 0.5f, -len * 0.5f + len * i / bars), new Vector3(0.04f, height, 0.04f), light, false);
            return root;
        }

        /// <summary>Round, bright cartoon tree (sphere canopy on a short trunk).</summary>
        public static GameObject BlobTree(Transform parent, Vector3 pos, float size, Color leaves)
        {
            var root = new GameObject("tree");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            Part(PrimitiveType.Cylinder, root.transform, new Vector3(0, size * 0.25f, 0), new Vector3(size * 0.16f, size * 0.25f, size * 0.16f), new Color(0.55f, 0.36f, 0.22f));
            Part(PrimitiveType.Sphere, root.transform, new Vector3(0, size * 0.75f, 0), new Vector3(size * 0.8f, size * 0.7f, size * 0.8f), leaves);
            Part(PrimitiveType.Sphere, root.transform, new Vector3(size * 0.18f, size * 0.95f, size * 0.1f), new Vector3(size * 0.45f, size * 0.4f, size * 0.45f), Color.Lerp(leaves, Color.white, 0.15f));
            return root;
        }

        // ------------------------------------------------------------------ icons (for pads and bubbles)

        /// <summary>Little person icon (hire), drawn flat-ish from primitives.</summary>
        public static GameObject PersonIcon(Transform parent, Color c)
        {
            var root = new GameObject("personIcon");
            root.transform.SetParent(parent, false);
            Part(PrimitiveType.Sphere, root.transform, new Vector3(0, 0.05f, 0.22f), new Vector3(0.3f, 0.08f, 0.3f), c, false);
            Part(PrimitiveType.Sphere, root.transform, new Vector3(0, 0.05f, -0.12f), new Vector3(0.55f, 0.08f, 0.38f), c, false);
            return root;
        }

        /// <summary>Upward arrow icon (upgrade).</summary>
        public static GameObject ArrowIcon(Transform parent, Color c)
        {
            var root = new GameObject("arrowIcon");
            root.transform.SetParent(parent, false);
            Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.04f, -0.08f), new Vector3(0.16f, 0.06f, 0.36f), c, false);
            var l = Part(PrimitiveType.Cube, root.transform, new Vector3(-0.1f, 0.04f, 0.14f), new Vector3(0.12f, 0.06f, 0.3f), c, false);
            l.transform.localRotation = Quaternion.Euler(0, 40, 0);
            var r = Part(PrimitiveType.Cube, root.transform, new Vector3(0.1f, 0.04f, 0.14f), new Vector3(0.12f, 0.06f, 0.3f), c, false);
            r.transform.localRotation = Quaternion.Euler(0, -40, 0);
            return root;
        }

        /// <summary>Plus icon (unlock / build).</summary>
        public static GameObject PlusIcon(Transform parent, Color c)
        {
            var root = new GameObject("plusIcon");
            root.transform.SetParent(parent, false);
            Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.04f, 0), new Vector3(0.14f, 0.06f, 0.5f), c, false);
            Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.04f, 0), new Vector3(0.5f, 0.06f, 0.14f), c, false);
            return root;
        }

        /// <summary>Green check mark (order complete).</summary>
        public static GameObject CheckIcon(Transform parent)
        {
            var root = new GameObject("check");
            root.transform.SetParent(parent, false);
            var g = new Color(0.3f, 0.85f, 0.35f);
            var a = Part(PrimitiveType.Cube, root.transform, new Vector3(-0.1f, -0.05f, 0), new Vector3(0.1f, 0.26f, 0.05f), g, false);
            a.transform.localRotation = Quaternion.Euler(0, 0, 40);
            var b = Part(PrimitiveType.Cube, root.transform, new Vector3(0.09f, 0.04f, 0), new Vector3(0.1f, 0.46f, 0.05f), g, false);
            b.transform.localRotation = Quaternion.Euler(0, 0, -35);
            return root;
        }

        // ------------------------------------------------------------------ floor paint

        static readonly Quaternion FlatOnFloor = Quaternion.Euler(90, 0, 0);

        /// <summary>Big letters painted on the floor (reads with the camera's diagonal like the video).</summary>
        public static TextMeshPro FloorText(Transform parent, string text, float size, Color color, Vector3 localPos, float rotY = 0)
        {
            var go = new GameObject("floorText");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos + Vector3.up * 0.03f;
            go.transform.localRotation = Quaternion.Euler(90, rotY, 0);
            var t = go.AddComponent<TextMeshPro>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.rectTransform.sizeDelta = new Vector2(8, 2);
            return t;
        }

        public static void SetFlat(Transform t) => t.localRotation = FlatOnFloor;
    }
}
