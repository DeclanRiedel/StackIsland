using UnityEngine;

namespace StackIsland
{
    /// <summary>
    /// You. Walk with WASD (or right-click to walk somewhere). Items you carry tower up in your hands, your cash
    /// towers up on your back. Near a crab you swing at it automatically; near a tray, table or cash pile items
    /// hop on and off your stack by themselves.
    /// </summary>
    public class Player : MonoBehaviour
    {
        public float Speed = 5.6f;
        public float AttackRange = 1.9f;
        public float AttackEvery = 0.36f;
        public int Damage = 1;
        public const double CashPerBundle = 10;
        const int MaxCashBundles = 30;

        public ItemStack Stack { get; private set; }
        public ItemStack CashTower { get; private set; }
        public bool IsMoving { get; private set; }

        CharacterController _cc;
        Transform _model, _tool;
        float _bob, _attackT, _swing, _transfer;
        Vector3 _clickTarget;
        bool _hasClick;

        public void Init()
        {
            _cc = gameObject.AddComponent<CharacterController>();
            _cc.height = 1.6f;
            _cc.radius = 0.38f;
            _cc.center = new Vector3(0, 0.85f, 0);
            _cc.stepOffset = 0.3f;
            var modelRoot = new GameObject("model").transform;
            modelRoot.SetParent(transform, false);
            _model = modelRoot;
            var body = Visuals.Spawn("mini-characters/character-male-b", modelRoot, 1.75f, Color.white, true);
            Visuals.SetShadows(body, true);
            _tool = Visuals.Spawn("food-kit/cooking-knife", modelRoot, 0.7f, Color.white).transform;
            _tool.localPosition = new Vector3(0.45f, 0.75f, 0.15f);
            _tool.localRotation = Quaternion.Euler(0, 0, -70);

            var hands = new GameObject("hands");
            hands.transform.SetParent(modelRoot, false);
            hands.transform.localPosition = new Vector3(0, 0.82f, 0.5f);
            Stack = hands.AddComponent<ItemStack>();
            Stack.Capacity = 12;
            var back = new GameObject("back");
            back.transform.SetParent(modelRoot, false);
            back.transform.localPosition = new Vector3(0, 0.95f, -0.42f);
            CashTower = back.AddComponent<ItemStack>();
            CashTower.Capacity = MaxCashBundles;
            CashTower.Sway = 0.045f;
        }

        public void Teleport(Vector3 p)
        {
            _cc.enabled = false;
            transform.position = p;
            _cc.enabled = true;
            _hasClick = false;
        }

        void Update()
        {
            var g = Game.I;
            if (g == null) return;
            float dt = Time.deltaTime;
            Move(dt, g.InputBlocked);
            Attack(dt);
            Scoop();
            _transfer -= dt;
            if (_transfer <= 0)
            {
                _transfer = 0.055f;
                Trade(g);
            }
            SyncCashTower(g.Cash);
        }

        void Move(float dt, bool blocked)
        {
            Vector2 input = blocked ? Vector2.zero : GameInput.Move.ReadValue<Vector2>();
            if (!blocked && GameInput.ClickMove.WasPressedThisFrame() && Camera.main != null)
            {
                var ray = Camera.main.ScreenPointToRay(GameInput.MousePosition);
                if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float enter)) { _clickTarget = ray.GetPoint(enter); _hasClick = true; }
            }
            Vector3 dir = Vector3.zero;
            if (input.sqrMagnitude > 0.01f)
            {
                _hasClick = false;
                float yaw = Game.I.Cam != null ? Game.I.Cam.Yaw : 0;
                dir = Quaternion.Euler(0, yaw, 0) * new Vector3(input.x, 0, input.y);
                if (dir.sqrMagnitude > 1) dir.Normalize();
            }
            else if (_hasClick)
            {
                var d = _clickTarget - transform.position;
                d.y = 0;
                if (d.magnitude < 0.25f) _hasClick = false;
                else dir = d.normalized;
            }
            IsMoving = dir.sqrMagnitude > 0.0001f;
            var vel = dir * Speed;
            vel.y = -6f;   // keep grounded
            if (_cc.enabled) _cc.Move(vel * dt);
            if (IsMoving)
            {
                _model.rotation = Quaternion.Slerp(_model.rotation, Quaternion.LookRotation(dir), 14f * dt);
                _bob += dt * Speed * 2.6f;
            }
            else _bob = Mathf.MoveTowards(_bob, Mathf.Round(_bob / Mathf.PI) * Mathf.PI, dt * 6f);
            _model.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(_bob)) * 0.1f, 0);
        }

        void Attack(float dt)
        {
            _attackT -= dt;
            _swing = Mathf.MoveTowards(_swing, 0, dt * 6f);
            _tool.localRotation = Quaternion.Euler(0, -_swing * 120f, -70);
            if (_attackT > 0 || !Yard.InPen(transform.position)) return;
            var crab = CrabPen.NearestAlive(transform.position, AttackRange);
            if (crab == null) return;
            _attackT = AttackEvery;
            _swing = 1f;
            var to = crab.transform.position - transform.position;
            to.y = 0;
            if (!IsMoving && to.sqrMagnitude > 0.01f) _model.rotation = Quaternion.LookRotation(to.normalized);
            Fx.I?.Slash(transform.position + Vector3.up * 0.9f + to.normalized * 0.8f, to.normalized);
            Sfx.I?.Play(Sfx.I.Swing, 0.35f, Random.Range(0.9f, 1.15f));
            crab.Hit(Damage, transform.position);
        }

        /// <summary>Hoover up meat lying on the ground nearby.</summary>
        void Scoop()
        {
            if (Stack.IsFull) return;
            var all = LooseItem.All;
            for (int i = all.Count - 1; i >= 0 && !Stack.IsFull; i--)
            {
                var li = all[i];
                if (li == null || !li.Ready || li.ClaimedBy != null) continue;
                var d = li.transform.position - transform.position;
                d.y = 0;
                if (d.magnitude > 1.9f) continue;
                var at = li.transform.position;
                var t = li.Type;
                li.Consume();
                Stack.Add(t, at);
                Sfx.I?.Play(Sfx.I.Pop, 0.3f, Random.Range(1f, 1.25f));
            }
        }

        /// <summary>Swap items with every tray / table / cash pile in reach (one item per tick).</summary>
        void Trade(Game g)
        {
            var pos = transform.position;
            foreach (var z in PileZone.All)
            {
                if (!z.PlayerAllowed || !z.InReach(pos)) continue;
                if (z.Drop && Stack.Has(z.Type) && !z.IsFull)
                {
                    Stack.Remove(z.Type, out var at);
                    z.Add(at);
                    Sfx.I?.Play(Sfx.I.Drop, 0.25f, Random.Range(0.95f, 1.1f));
                }
                else if (z.Take && z.Count > 0)
                {
                    if (z.Type == ItemType.Cash)
                    {
                        // scoop several bundles a tick: cash piles can be big
                        for (int k = 0; k < 3 && z.Count > 0; k++)
                        {
                            z.Remove(out var at, out var v);
                            g.AddCash(v, at);
                        }
                    }
                    else if (!Stack.IsFull)
                    {
                        z.Remove(out var at, out _);
                        Stack.Add(z.Type, at);
                        Sfx.I?.Play(Sfx.I.Pop, 0.25f, Random.Range(1f, 1.2f));
                    }
                }
            }
        }

        /// <summary>The back tower shows your money: one bundle per 10 coins, up to a tall-but-sane cap.</summary>
        void SyncCashTower(double cash)
        {
            int want = Mathf.Clamp((int)System.Math.Ceiling(cash / CashPerBundle), 0, MaxCashBundles);
            if (CashTower.Count < want) CashTower.Add(ItemType.Cash);
            else if (CashTower.Count > want) CashTower.Remove(ItemType.Cash, out _);
        }

        /// <summary>Visual: a bundle leaves your back and flies to <paramref name="target"/>.</summary>
        public void FlyCashTo(Vector3 target)
        {
            if (Tweens.I == null) return;
            var go = Items.Get(ItemType.Cash, null);
            var tr = go.transform;
            Tweens.I.Fly(tr, CashTower.TopWorld, () => target, 0.25f, 1.0f, () => Items.Release(ItemType.Cash, go));
        }

        /// <summary>Visual: a bundle flies from <paramref name="from"/> onto your back.</summary>
        public void FlyCashFrom(Vector3 from)
        {
            if (Tweens.I == null) return;
            var go = Items.Get(ItemType.Cash, null);
            var tr = go.transform;
            Tweens.I.Fly(tr, from, () => CashTower.TopWorld, 0.25f, 0.8f, () => Items.Release(ItemType.Cash, go));
        }
    }
}
