using System;
using System.Collections.Generic;
using UnityEngine;

namespace StackIsland
{
    /// <summary>Every item in the game. Raw items come from a yard's pen or field; products are made at stations.</summary>
    public enum ItemType
    {
        None = 0, Meat = 1, CrabCake = 2, Cash = 3, Coconut = 4, Juice = 5,
        Fish = 6, Sushi = 7, Banana = 8, Smoothie = 9, Pork = 10, Bacon = 11, Egg = 12, Pancakes = 13,
        Pineapple = 14, Pizza = 15, Strawberry = 16, IceCream = 17, Beef = 18, Burger = 19, Feast = 20,
    }

    /// <summary>Everything about an item type: name, base sale price, how tall it is in a stack, and its model.</summary>
    public static class Items
    {
        struct Info
        {
            public string name;
            public double price;     // what a customer pays (before upgrades); raw items aren't sold
            public float height;     // vertical space in a stack
            public string model;     // Kenney key, or "" for models drawn in code
            public float size;
            public Info(string n, double p, float h, string m = "", float s = 0.45f) { name = n; price = p; height = h; model = m; size = s; }
        }

        static readonly Dictionary<ItemType, Info> Table = new Dictionary<ItemType, Info>
        {
            { ItemType.Meat, new Info("Crab Meat", 0, 0.11f) },
            { ItemType.CrabCake, new Info("Crab Cake", 5, 0.14f) },
            { ItemType.Cash, new Info("Cash", 0, 0.125f) },
            { ItemType.Coconut, new Info("Coconut", 0, 0.34f, "food-kit/coconut", 0.36f) },
            { ItemType.Juice, new Info("Coconut Juice", 40, 0.3f, "food-kit/coconut-half", 0.42f) },
            { ItemType.Fish, new Info("Fish", 0, 0.16f, "food-kit/fish", 0.5f) },
            { ItemType.Sushi, new Info("Sushi", 260, 0.2f, "food-kit/maki-salmon", 0.36f) },
            { ItemType.Banana, new Info("Banana", 0, 0.16f, "food-kit/banana", 0.48f) },
            { ItemType.Smoothie, new Info("Smoothie", 1600, 0.38f, "food-kit/frappe", 0.34f) },
            { ItemType.Pork, new Info("Pork", 0, 0.14f, "food-kit/meat-raw", 0.46f) },
            { ItemType.Bacon, new Info("Crispy Bacon", 10000, 0.1f, "food-kit/bacon", 0.48f) },
            { ItemType.Egg, new Info("Egg", 0, 0.24f, "food-kit/egg", 0.24f) },
            { ItemType.Pancakes, new Info("Pancakes", 62000, 0.24f, "food-kit/pancakes", 0.46f) },
            { ItemType.Pineapple, new Info("Pineapple", 0, 0.42f, "food-kit/pineapple", 0.44f) },
            { ItemType.Pizza, new Info("Pineapple Pizza", 380000, 0.12f, "food-kit/pizza", 0.52f) },
            { ItemType.Strawberry, new Info("Strawberry", 0, 0.16f, "food-kit/strawberry", 0.26f) },
            { ItemType.IceCream, new Info("Ice Cream", 2.4e6, 0.3f, "food-kit/ice-cream-cup", 0.32f) },
            { ItemType.Beef, new Info("Beef Patty", 0, 0.1f, "food-kit/meat-patty", 0.44f) },
            { ItemType.Burger, new Info("Burger", 1.5e7, 0.3f, "food-kit/burger-cheese", 0.44f) },
            { ItemType.Feast, new Info("Volcano Feast", 2.2e8, 0.36f, "food-kit/turkey", 0.52f) },
        };

        public static string Name(ItemType t) => Table.TryGetValue(t, out var i) ? i.name : "?";

        /// <summary>What a customer pays for one (before upgrades).</summary>
        public static double Price(ItemType t) => Table.TryGetValue(t, out var i) ? i.price : 0;

        /// <summary>Vertical space one item takes in a stack.</summary>
        public static float Height(ItemType t) => Table.TryGetValue(t, out var i) ? i.height : 0.15f;

        static GameObject Build(ItemType t)
        {
            switch (t)
            {
                case ItemType.Meat: return ProcArt.MeatSlice(null);
                case ItemType.CrabCake: return ProcArt.CrabCake(null);
                case ItemType.Cash: return ProcArt.CashBundle(null);
            }
            if (Table.TryGetValue(t, out var i) && !string.IsNullOrEmpty(i.model)) return Visuals.Spawn(i.model, null, i.size, Color.white);
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
