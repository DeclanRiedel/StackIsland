using System.Collections.Generic;
using UnityEngine;

namespace StackIsland
{
    /// <summary>An item lying on the ground, waiting to be scooped up by the player or a helper.</summary>
    public class LooseItem : MonoBehaviour
    {
        public static readonly List<LooseItem> All = new List<LooseItem>();
        public ItemType Type;
        public object ClaimedBy;
        public Yard Yard;
        float _age;

        public bool Ready => _age > 0.45f;   // let it land before it can be grabbed

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        void Update() => _age += Time.deltaTime;

        public static LooseItem Spawn(Yard yard, ItemType t, Vector3 from, Vector3 land)
        {
            var go = Items.Get(t, null);
            go.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
            var li = go.GetComponent<LooseItem>() ?? go.AddComponent<LooseItem>();
            li.Type = t;
            li.Yard = yard;
            li.ClaimedBy = null;
            li._age = 0;
            li.enabled = true;
            Tweens.I?.Fly(go.transform, from, () => land, 0.4f, 1.4f, null);
            return li;
        }

        /// <summary>Remove from the ground (the caller adds the item to a stack).</summary>
        public void Consume()
        {
            enabled = false;
            Items.Release(Type, gameObject);
        }
    }

    /// <summary>A wandering animal (crab, fish, hog, cow). Hit it a few times and it drops its raw item, then a new one arrives.</summary>
    public class Animal : MonoBehaviour
    {
        public static readonly List<Animal> All = new List<Animal>();

        public AnimalPen Pen;
        public int MaxHp = 3;
        public int Hp;
        public bool Alive => Hp > 0 && _spawnT >= 1f;
        Vector3 _target;
        float _wait, _flash, _spawnT;
        bool _tinted, _sideways;
        Transform _model, _barFill, _barRoot;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void Init(AnimalPen pen)
        {
            Pen = pen;
            var def = pen.Yard.Def;
            MaxHp = def.AnimalHp;
            _sideways = def.AnimalName == "crab";
            _model = Visuals.Spawn(def.AnimalModel, transform, def.AnimalSize, Color.white).transform;
            Visuals.SetShadows(_model.gameObject, true);
            // health bar (billboard): dark back + red fill, only shown once hurt
            _barRoot = new GameObject("hp").transform;
            _barRoot.SetParent(transform, false);
            _barRoot.localPosition = new Vector3(0, def.AnimalSize + 0.1f, 0);
            _barRoot.gameObject.AddComponent<Billboard>();
            Visuals.Primitive(PrimitiveType.Cube, _barRoot, Vector3.zero, new Vector3(1.3f, 0.2f, 0.02f), Mats.Unlit(new Color(0.15f, 0.1f, 0.1f)), "bg");
            var fillRoot = new GameObject("fillRoot").transform;
            fillRoot.SetParent(_barRoot, false);
            fillRoot.localPosition = new Vector3(-0.62f, 0, -0.02f);
            _barFill = Visuals.Primitive(PrimitiveType.Cube, fillRoot, new Vector3(0.5f, 0, 0), new Vector3(1f, 0.14f, 0.02f), Mats.Unlit(new Color(0.95f, 0.25f, 0.25f)), "fill").transform;
            Respawn();
        }

        public void Respawn()
        {
            Hp = MaxHp;
            _spawnT = 0;
            transform.position = Pen.RandomPoint();
            _target = Pen.RandomPoint();
            UpdateBar();
        }

        void UpdateBar()
        {
            bool show = Hp > 0 && Hp < MaxHp;
            _barRoot.gameObject.SetActive(show);
            _barFill.parent.localScale = new Vector3(1.24f * Hp / MaxHp, 1, 1);
        }

        public void Hit(int damage, Vector3 from)
        {
            if (!Alive) return;
            Hp -= damage;
            _flash = 1f;
            var push = transform.position - from;
            push.y = 0;
            transform.position = Pen.Clamp(transform.position + push.normalized * 0.45f);
            Fx.I?.HitSparks(transform.position + Vector3.up * 0.7f - push.normalized * 0.4f);
            Sfx.I?.Play(Sfx.I.Hit, 0.5f, Random.Range(0.9f, 1.1f));
            UpdateBar();
            if (Hp <= 0) Die();
        }

        void Die()
        {
            int n = Pen.Yard.Def.RawPerAnimal;
            for (int i = 0; i < n; i++)
            {
                float a = (i / (float)n) * Mathf.PI * 2f + Random.value;
                var land = Pen.Clamp(transform.position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * Random.Range(0.8f, 1.6f));
                land.y = 0.08f;
                LooseItem.Spawn(Pen.Yard, Pen.Yard.Def.Raw, transform.position + Vector3.up * 0.8f, land);
            }
            Fx.I?.Puff(transform.position + Vector3.up * 0.5f, 18);
            _model.gameObject.SetActive(false);
            _barRoot.gameObject.SetActive(false);
            Pen.QueueRespawn(this);
        }

        public void Revive()
        {
            _model.gameObject.SetActive(true);
            Respawn();
        }

        void Update()
        {
            if (Hp <= 0) return;
            float dt = Time.deltaTime;
            if (_spawnT < 1f) _spawnT = Mathf.Min(1f, _spawnT + dt * 2.5f);
            // lazy wandering (crabs walk sideways)
            var d = _target - transform.position;
            d.y = 0;
            if (_wait > 0) _wait -= dt;
            else if (d.magnitude < 0.2f) { _wait = Random.Range(0.6f, 2.2f); _target = Pen.RandomPoint(); }
            else
            {
                transform.position += d.normalized * Mathf.Min(d.magnitude, 1.1f * dt);
                var face = Quaternion.LookRotation(_sideways ? Vector3.Cross(Vector3.up, d.normalized) : d.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, face, 4f * dt);
            }
            // squash and flash red when hit
            if (_flash > 0 || _tinted)
            {
                Visuals.Tint(_model.gameObject, Color.Lerp(Color.white, new Color(1f, 0.35f, 0.35f), _flash));
                _tinted = _flash > 0;
            }
            _flash = Mathf.MoveTowards(_flash, 0, dt * 5f);
            float sq = 1f + _flash * 0.35f;
            _model.localScale = new Vector3(sq, 1f / sq, sq) * Mathf.SmoothStep(0.05f, 1f, _spawnT);
        }
    }

    /// <summary>A fenced pen in a yard: keeps a number of animals alive and respawns them after they're caught.</summary>
    public class AnimalPen : MonoBehaviour
    {
        public Yard Yard { get; private set; }
        public int Count { get; private set; }
        public float RespawnSeconds = 2.5f;
        readonly List<Animal> _animals = new List<Animal>();
        readonly List<(Animal a, float t)> _respawn = new List<(Animal, float)>();

        public void Init(Yard yard)
        {
            Yard = yard;
            SetCount(4);
        }

        public Vector3 RandomPoint() => Yard.W(Random.Range(Yard.SrcMinX + 1.4f, Yard.SrcMaxX - 1.4f), Random.Range(Yard.SrcMinZ + 1.8f, Yard.SrcMaxZ - 1.4f)) + Vector3.up * 0.05f;

        public Vector3 Clamp(Vector3 p)
        {
            var o = Yard.Origin;
            p.x = Mathf.Clamp(p.x, o.x + Yard.SrcMinX + 0.9f, o.x + Yard.SrcMaxX - 0.9f);
            p.z = Mathf.Clamp(p.z, o.z + Yard.SrcMinZ + 0.9f, o.z + Yard.SrcMaxZ - 0.9f);
            return p;
        }

        public void SetCount(int n)
        {
            Count = n;
            while (_animals.Count < n)
            {
                var go = new GameObject(Yard.Def.AnimalName + " " + _animals.Count);
                go.transform.SetParent(transform, false);
                var a = go.AddComponent<Animal>();
                a.Init(this);
                _animals.Add(a);
            }
        }

        public string Info(int damage)
        {
            int alive = 0;
            foreach (var a in _animals) if (a.Alive) alive++;
            var def = Yard.Def;
            return $"<b>{def.AnimalName.Substring(0, 1).ToUpperInvariant()}{def.AnimalName.Substring(1)} pen</b>\n" +
                   $"{alive}/{Count} ready · {def.RawPerAnimal} {Items.Name(def.Raw)} each\n" +
                   $"Takes {Mathf.CeilToInt(def.AnimalHp / (float)damage)} hits · a new one arrives {RespawnSeconds:0.#}s after one is caught\n" +
                   "Walk in to catch them; walk over what they drop to pick it up";
        }

        public void QueueRespawn(Animal a) => _respawn.Add((a, RespawnSeconds));

        void Update()
        {
            for (int i = _respawn.Count - 1; i >= 0; i--)
            {
                var (a, t) = _respawn[i];
                t -= Time.deltaTime;
                if (t <= 0) { _respawn.RemoveAt(i); a.Revive(); }
                else _respawn[i] = (a, t);
            }
        }

        /// <summary>Nearest animal that can be hit, within <paramref name="range"/> of <paramref name="from"/>.</summary>
        public static Animal NearestAlive(Vector3 from, float range)
        {
            Animal best = null;
            float bd = range;
            foreach (var a in Animal.All)
            {
                if (!a.Alive) continue;
                var d = a.transform.position - from;
                d.y = 0;
                float m = d.magnitude;
                if (m < bd) { bd = m; best = a; }
            }
            return best;
        }
    }
}
