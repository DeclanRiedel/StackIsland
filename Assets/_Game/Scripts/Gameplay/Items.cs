using System;
using System.Collections.Generic;
using UnityEngine;

namespace StackIsland
{
    /// <summary>Every item in the game. Raw items come from a yard's pen or field; products are made at stations.</summary>


    /// <summary>Everything about an item type: name, base sale price, how tall it is in a stack, and its model.</summary>
    public static class Items
    {


        public static string Name(ItemType t) => ItemInfo.Name(t);

        /// <summary>What a customer pays for one (before upgrades).</summary>
        public static double Price(ItemType t) => ItemInfo.Price(t);

        /// <summary>Vertical space one item takes in a stack.</summary>
        public static float Height(ItemType t) => ItemInfo.Height(t);

        static GameObject Build(ItemType t)
        {
            switch (t)
            {
                case ItemType.Meat: return ProcArt.MeatSlice(null);
                case ItemType.CrabCake: return ProcArt.CrabCake(null);
                case ItemType.Cash: return ProcArt.CashBundle(null);
            }
            if (ItemInfo.Table.TryGetValue(t, out var i) && !string.IsNullOrEmpty(i.model)) return Visuals.Spawn(i.model, null, i.size, Color.white);
            return Visuals.Primitive(PrimitiveType.Cube, null, Vector3.zero, Vector3.one * 0.3f, Mats.Lit(Color.magenta));
        }

        // ------------------------------------------------------------------ pooling

        static readonly Dictionary<ItemType, Stack<GameObject>> Free = new Dictionary<ItemType, Stack<GameObject>>();
        static Transform _root;

        static Transform Root
        {
            get
            {
                if (_root == null)
                {
                    _root = new GameObject("[ItemPool]").transform;
                    _root.gameObject.SetActive(false);
                }
                return _root;
            }
        }

        public static void ResetPool()
        {
            Free.Clear();
            if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
            _root = null;
        }

        public static GameObject Get(ItemType t, Transform parent)
        {
            GameObject go = null;
            if (Free.TryGetValue(t, out var st))
                while (st.Count > 0 && go == null) go = st.Pop();
            if (go == null)
            {
                go = Build(t);
                go.name = t.ToString();
                Visuals.SetShadows(go, false);
            }
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.SetActive(true);
            return go;
        }

        public static void Release(ItemType t, GameObject go)
        {
            if (go == null) return;
            Tweens.I?.Cancel(go.transform);   // a pooled object must not keep flying
            go.transform.SetParent(Root, false);
            if (!Free.TryGetValue(t, out var st)) Free[t] = st = new Stack<GameObject>();
            st.Push(go);
        }
    }

    /// <summary>
    /// Tiny tween runner for "juice": items hopping from a stack to a tray, cash flying into a pad.
    /// Logic never waits on these - the item already belongs to its new home when the flight starts.
    /// </summary>
    public class Tweens : MonoBehaviour
    {
        public static Tweens I;

        class Flight
        {
            public Transform t;
            public Vector3 from;
            public Func<Vector3> to;
            public Quaternion fromRot;
            public Func<Quaternion> toRot;
            public float time, dur, arc;
            public Action done;
        }

        readonly List<Flight> _flights = new List<Flight>();

        void Awake() => I = this;

        /// <summary>Arc <paramref name="t"/> from a world position to a moving target, then call <paramref name="done"/>.</summary>
        public void Fly(Transform t, Vector3 from, Func<Vector3> to, float dur, float arc, Action done, Func<Quaternion> toRot = null)
        {
            t.position = from;
            _flights.Add(new Flight { t = t, from = from, to = to, fromRot = t.rotation, toRot = toRot, dur = Mathf.Max(0.01f, dur), arc = arc, done = done });
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _flights.Count - 1; i >= 0; i--)
            {
                if (i >= _flights.Count) continue;
                var f = _flights[i];
                if (f.t == null) { _flights.RemoveAt(i); continue; }
                f.time += dt;
                float k = Mathf.Clamp01(f.time / f.dur);
                float e = k * k * (3 - 2 * k);
                var target = f.to();
                f.t.position = Vector3.Lerp(f.from, target, e) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * f.arc);
                if (f.toRot != null) f.t.rotation = Quaternion.Slerp(f.fromRot, f.toRot(), e);
                if (k >= 1f)
                {
                    _flights.Remove(f);
                    f.done?.Invoke();
                }
            }
        }

        public void Cancel(Transform t) => _flights.RemoveAll(f => f.t == t);

        public bool IsFlying(Transform t)
        {
            foreach (var f in _flights) if (f.t == t) return true;
            return false;
        }
    }
}
