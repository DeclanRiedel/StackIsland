using System;
using System.Collections.Generic;
using UnityEngine;

namespace StackIsland
{
    /// <summary>Maps short keys ("pirate-kit/palm-straight") to imported models. Filled by the editor setup tool.</summary>
    public class ArtLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string key;
            public GameObject prefab;
        }

        public List<Entry> entries = new List<Entry>();

        [Header("Template materials (cloned and tinted at runtime)")]
        public Material litOpaque;
        public Material litTransparent;
        public Material unlitOpaque;
        public Material unlitTransparent;

        Dictionary<string, GameObject> _lookup;

        public GameObject Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_lookup == null)
            {
                _lookup = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
                foreach (var e in entries)
                    if (e.prefab != null) _lookup[e.key] = e.prefab;
            }
            return _lookup.TryGetValue(key, out var go) ? go : null;
        }

        public bool Has(string key) => Get(key) != null;

        public void ResetCache() => _lookup = null;
    }
}
