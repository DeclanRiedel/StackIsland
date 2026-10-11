using System.Collections.Generic;
using UnityEngine;

namespace StackIsland
{
    /// <summary>Shared bits for hired helpers: a body, a carry stack, walking along waypoints.</summary>
    public abstract class Worker : MonoBehaviour
    {
        public float Speed = 3.6f;
        protected ItemStack Stack;
        protected Transform Model;
        readonly List<Vector3> _route = new List<Vector3>();
        Vector3 _routeTarget = new Vector3(9999, 0, 9999);
        float _bob;

        public void Init(string model, Color shirt, int capacity, Vector3 start)
        {
            transform.position = start;
            Model = new GameObject("model").transform;
            Model.SetParent(transform, false);
            var body = Visuals.Spawn(model, Model, 1.6f, Color.white, true);
            Visuals.SetShadows(body, true);
            // a coloured cap so helpers stand out from customers
            var cap = Visuals.Primitive(PrimitiveType.Sphere, Model, new Vector3(0, 1.62f, 0), new Vector3(0.62f, 0.3f, 0.62f), Mats.Lit(shirt), "cap");
            cap.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            var hands = new GameObject("hands");
            hands.transform.SetParent(Model, false);
            hands.transform.localPosition = new Vector3(0, 0.78f, 0.45f);
            Stack = hands.AddComponent<ItemStack>();
            Stack.Capacity = capacity;
        }

        /// <summary>Walk toward <paramref name="target"/> (detouring through the pen gate). True once there.</summary>
        protected bool WalkTo(Vector3 target, float stopAt = 0.3f)
        {
            target.y = 0;
            if ((target - _routeTarget).sqrMagnitude > 0.25f)
            {
                _routeTarget = target;
                Yard.Route(transform.position, target, _route);
            }
            if (_route.Count == 0) return true;
            var next = _route[0];
            var d = next - transform.position;
            d.y = 0;
            float dist = d.magnitude;
            bool last = _route.Count == 1;
            if (dist <= (last ? stopAt : 0.25f))
            {
                _route.RemoveAt(0);
                if (last) { _bob = 0; Model.localPosition = Vector3.zero; return true; }
                return false;
            }
            float dt = Time.deltaTime;
            transform.position += d / dist * Mathf.Min(dist, Speed * dt);
            Model.rotation = Quaternion.Slerp(Model.rotation, Quaternion.LookRotation(d / dist), 12f * dt);
            _bob += dt * Speed * 2.6f;
            Model.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(_bob)) * 0.1f, 0);
            return false;
        }

        protected void Face(Vector3 p)
        {
            var d = p - transform.position;
            d.y = 0;
            if (d.sqrMagnitude > 0.01f) Model.rotation = Quaternion.Slerp(Model.rotation, Quaternion.LookRotation(d.normalized), 12f * Time.deltaTime);
        }
    }

    /// <summary>Crab Catcher: hunts crabs in the pen, scoops the meat and carries it to a grill.</summary>
    public class CatcherWorker : Worker
    {
        enum Mode { Hunt, Deliver }
        Mode _mode;
        float _attack, _transfer;
        LooseItem _claim;
        Transform _tool;

        void Start()
        {
            _tool = Visuals.Spawn("food-kit/cooking-knife", Model, 0.6f, Color.white).transform;
            _tool.localPosition = new Vector3(0.42f, 0.72f, 0.12f);
            _tool.localRotation = Quaternion.Euler(0, 0, -70);
        }

        void Update()
        {
            var g = Game.I;
            if (g == null) return;
            if (_mode == Mode.Hunt) Hunt(g); else Deliver(g);
        }

        void Hunt(Game g)
        {
            if (Stack.IsFull) { Release(); _mode = Mode.Deliver; return; }
            // grab meat on the ground first
            if (_claim == null || !_claim.enabled || (_claim.ClaimedBy != null && _claim.ClaimedBy != (object)this))
            {
                Release();
                _claim = NearestMeat();
                if (_claim != null) _claim.ClaimedBy = this;
            }
            if (_claim != null)
            {
                if (!_claim.Ready) { WalkTo(_claim.transform.position, 0.6f); return; }
                if (WalkTo(_claim.transform.position, 0.6f))
                {
                    var at = _claim.transform.position;
                    _claim.Consume();
                    Stack.Add(ItemType.Meat, at);
                    _claim = null;
                }
                return;
            }
            // nothing to pick up: go catch a crab (or deliver what we have)
            var crab = NearestCrab();
            if (crab == null)
            {
                if (Stack.Count > 0) _mode = Mode.Deliver;
                return;
            }
            if (WalkTo(crab.transform.position, 1.4f) || (crab.transform.position - transform.position).magnitude < 1.6f)
            {
                Face(crab.transform.position);
                _attack -= Time.deltaTime;
                if (_attack <= 0)
                {
                    _attack = 0.5f;
                    var dir = crab.transform.position - transform.position;
                    dir.y = 0;
                    Fx.I?.Slash(transform.position + Vector3.up * 0.9f + dir.normalized * 0.7f, dir.normalized);
                    crab.Hit(1, transform.position);
                }
            }
        }

        void Deliver(Game g)
        {
            var target = g.BestGrillInput();
            if (target == null) return;
            if (!WalkTo(target.transform.position, 1.0f)) return;
            _transfer -= Time.deltaTime;
            if (_transfer > 0) return;
            _transfer = 0.07f;
            if (Stack.Count > 0 && !target.IsFull)
            {
                Stack.Remove(ItemType.Meat, out var at);
                target.Add(at);
            }
            if (Stack.Count == 0 || target.IsFull) _mode = Mode.Hunt;
        }

        void Release()
        {
            if (_claim != null && _claim.ClaimedBy == (object)this) _claim.ClaimedBy = null;
            _claim = null;
        }

        LooseItem NearestMeat()
        {
            LooseItem best = null;
            float bd = float.MaxValue;
            foreach (var li in LooseItem.All)
            {
                if (li.ClaimedBy != null || li.Type != ItemType.Meat) continue;
                float d = (li.transform.position - transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = li; }
            }
            return best;
        }

        Crab NearestCrab()
        {
            Crab best = null;
            float bd = float.MaxValue;
            foreach (var c in Crab.All)
            {
                if (!c.Alive) continue;
                float d = (c.transform.position - transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = c; }
            }
            return best;
        }

        void OnDestroy() => Release();
    }

    /// <summary>Waiter: carries crab cakes from the grills to the counter.</summary>
    public class WaiterWorker : Worker
    {
        bool _delivering;
        float _transfer;

        void Update()
        {
            var g = Game.I;
            if (g == null) return;
            _transfer -= Time.deltaTime;
            if (!_delivering)
            {
                var src = g.BestGrillOutput();
                if (src == null)
                {
                    if (Stack.Count > 0) _delivering = true;
                    return;
                }
                if (!WalkTo(src.transform.position, 1.0f)) return;
                if (_transfer <= 0 && src.Count > 0 && !Stack.IsFull)
                {
                    _transfer = 0.07f;
                    src.Remove(out var at, out _);
                    Stack.Add(src.Type, at);
                }
                if (Stack.IsFull || (src.Count == 0 && Stack.Count > 0)) _delivering = true;
            }
            else
            {
                var shelf = g.Counter.Shelf;
                // stand on the SELL sign like the player does
                if (!WalkTo(shelf.transform.position, 0.8f)) return;
                if (_transfer <= 0 && Stack.Count > 0 && !shelf.IsFull)
                {
                    _transfer = 0.07f;
                    Stack.Remove(shelf.Type, out var at);
                    shelf.Add(at);
                }
                if (Stack.Count == 0) _delivering = false;
            }
        }
    }
}
