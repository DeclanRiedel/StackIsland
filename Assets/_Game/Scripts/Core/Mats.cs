using System.Collections.Generic;

using UnityEngine;

namespace StackIsland
{
    /// <summary>Cached, tinted runtime materials cloned from the template materials in the ArtLibrary.</summary>
    public static class Mats
    {
        static readonly Dictionary<(int kind, Color c, float e), Material> Cache = new Dictionary<(int, Color, float), Material>();
        static ArtLibrary _art;

        public static void Init(ArtLibrary art)
        {
            _art = art;
            Cache.Clear();
        }

        static Material Template(int kind)
        {
            if (_art != null)
            {
                switch (kind)
                {
                    case 0: if (_art.litOpaque) return _art.litOpaque; break;
                    case 1: if (_art.litTransparent) return _art.litTransparent; break;
                    case 2: if (_art.unlitOpaque) return _art.unlitOpaque; break;
                    case 3: if (_art.unlitTransparent) return _art.unlitTransparent; break;
                }
            }
            // Fallback (tests without data): plain URP shaders.
            var sh = Shader.Find(kind >= 2 ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            return new Material(sh);
        }

        static Material Get(int kind, Color c, float emission = 0f)
        {
            var key = (kind, c, emission);
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Template(kind)) { name = $"rt_{kind}_{ColorUtility.ToHtmlStringRGBA(c)}" };
            m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (emission > 0 && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * emission);
            }
            Cache[key] = m;
            return m;
        }

        public static Material Lit(Color c) => Get(0, c);
        public static Material Glow(Color c, float intensity = 1.5f) => Get(0, c, intensity);
        public static Material LitTransparent(Color c) => Get(1, c);
        public static Material Unlit(Color c) => Get(2, c);
        public static Material UnlitTransparent(Color c) => Get(3, c);

        /// <summary>A unique (non-shared) transparent material, for things that fade individually.</summary>
        public static Material NewFadeable(Color c)
        {
            var m = new Material(Template(3));
            m.SetColor("_BaseColor", c);
            return m;
        }
    }
}
