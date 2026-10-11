using System.Collections.Generic;
using UnityEngine;

namespace StackIsland
{
    /// <summary>
    /// The tower of items someone carries. Each level trails the one below it a little, so a tall stack sways
    /// when you start, stop and turn - the satisfying wobble from the genre.
    /// </summary>
    public class ItemStack : MonoBehaviour
    {
        public int Capacity = 10;
        public float Sway = 0.06f;

        readonly List<ItemType> _items = new List<ItemType>();
        readonly List<Transform> _views = new List<Transform>();
        readonly List<Vector3> _lag = new List<Vector3>();
        Vector3 _lastPos;
        Vector3 _vel;

        public int Count => _items.Count;
        public int Space => Mathf.Max(0, Capacity - _items.Count);
        public bool IsFull => _items.Count >= Capacity;
        public IReadOnlyList<ItemType> ItemsHeld => _items;

        public int CountOf(ItemType t)
        {
            int n = 0;
            foreach (var i in _items) if (i == t) n++;
            return n;
        }

        public bool Has(ItemType t) => _items.Contains(t);

        float HeightAt(int index)
        {
            float h = 0;
            for (int i = 0; i < index; i++) h += Items.Height(_items[i]);
            return h;
        }

        /// <summary>World position where the next item would sit (target for flights).</summary>
        public Vector3 TopWorld => transform.TransformPoint(new Vector3(0, HeightAt(_items.Count), 0));

        public Vector3 SlotWorld(int index) => transform.TransformPoint(new Vector3(0, HeightAt(index), 0) + (index < _lag.Count ? _lag[index] : Vector3.zero));

        /// <summary>Add an item that visually flies in from <paramref name="from"/> (or appears in place).</summary>
        public bool Add(ItemType t, Vector3? from = null)
        {
            if (IsFull) return false;
            int index = _items.Count;
            _items.Add(t);
            var go = Items.Get(t, transform);
            _views.Add(go.transform);
            _lag.Add(Vector3.zero);
            go.transform.localPosition = new Vector3(0, HeightAt(index), 0);
            if (from.HasValue && Tweens.I != null)
            {
                var tr = go.transform;
                tr.SetParent(null, true);
                Tweens.I.Fly(tr, from.Value, () => SlotWorld(Mathf.Max(0, _views.IndexOf(tr))), 0.22f, 0.6f, () =>
                {
                    if (tr != null && _views.Contains(tr)) tr.SetParent(transform, true);
                });
            }
            return true;
        }

        /// <summary>Remove the highest item of type <paramref name="t"/>. Returns its world position (for the flight out).</summary>
        public bool Remove(ItemType t, out Vector3 at)
        {
            at = transform.position;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (_items[i] != t) continue;
                var v = _views[i];
                at = v != null ? v.position : SlotWorld(i);
                if (v != null) Items.Release(t, v.gameObject);
                _items.RemoveAt(i);
                _views.RemoveAt(i);
                _lag.RemoveAt(i);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            for (int i = 0; i < _items.Count; i++) if (_views[i] != null) Items.Release(_items[i], _views[i].gameObject);
            _items.Clear();
            _views.Clear();
            _lag.Clear();
        }

        void LateUpdate()
        {
            float dt = Mathf.Max(1e-4f, Time.deltaTime);
            var pos = transform.position;
            var v = (pos - _lastPos) / dt;
            _lastPos = pos;
            v.y = 0;
            _vel = Vector3.Lerp(_vel, v, 1 - Mathf.Exp(-8 * dt));
            var localVel = transform.InverseTransformDirection(_vel);
            float h = 0;
            for (int i = 0; i < _views.Count; i++)
            {
                // higher items lag further behind the motion, then spring back
                float k = Mathf.Pow(i / 10f, 1.4f) * Sway;
                var target = -localVel * k;
                target.y = 0;
                _lag[i] = Vector3.Lerp(_lag[i], target, 1 - Mathf.Exp(-(14f - Mathf.Min(10f, i * 0.25f)) * dt));
                var tr = _views[i];
                if (tr != null && tr.parent == transform)
                {
                    tr.localPosition = new Vector3(_lag[i].x, h, _lag[i].z);
                    tr.localRotation = Quaternion.Euler(_lag[i].z * 25f, 0, -_lag[i].x * 25f);
                }
                h += Items.Height(_items[i]);
            }
        }
    }
}
