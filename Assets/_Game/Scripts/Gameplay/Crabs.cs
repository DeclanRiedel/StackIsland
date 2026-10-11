using System.Collections.Generic;
using UnityEngine;

namespace StackIsland
{
    /// <summary>A meat item lying on the ground, waiting to be scooped up by the player or a helper.</summary>
    public class LooseItem : MonoBehaviour
    {
        public static readonly List<LooseItem> All = new List<LooseItem>();
        public ItemType Type;
        public object ClaimedBy;
        float _age;

        public bool Ready => _age > 0.45f;   // let it land before it can be grabbed

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        void Update() => _age += Time.deltaTime;

        public static LooseItem Spawn(ItemType t, Vector3 from, Vector3 land)
        {
            var go = Items.Get(t, null);
            go.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
            var li = go.GetComponent<LooseItem>() ?? go.AddComponent<LooseItem>();
            li.Type = t;
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

    /// <summary>A big wandering crab. Hit it a few times and it drops crab meat, then a new one scuttles in.</summary>
    public class Crab : MonoBehaviour
    {
        public static readonly List<Crab> All = new List<Crab>();

        public CrabPen Pen;
        public int MaxHp = 3;
        public int Hp;
        public bool Alive => Hp > 0 && _spawnT >= 1f;
        Vector3 _target;
        float _wait, _flash, _spawnT;
        Transform _model, _bar, _barFill, _barRoot;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void Init(CrabPen pen)
        {
            Pen = pen;
            _model = Visuals.Spawn("cube-pets/animal-crab", transform, 1.9f, Color.white).transform;
            Visuals.SetShadows(_model.gameObject, true);
            // health bar (billboard): dark back + red fill, only shown once hurt
            _barRoot = new GameObject("hp").transform;
            _barRoot.SetParent(transform, false);
            _barRoot.localPosition = new Vector3(0, 1.9f, 0);
            _barRoot.gameObject.AddComponent<Billboard>();
            _bar = Visuals.Primitive(PrimitiveType.Cube, _barRoot, Vector3.zero, new Vector3(1.3f, 0.2f, 0.02f), Mats.Unlit(new Color(0.15f, 0.1f, 0.1f)), "bg").transform;
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
            transform.position = Pen.Clamp(transform.position + push.normalized * 0.35f);
            Sfx.I?.Play(Sfx.I.Hit, 0.5f, Random.Range(0.9f, 1.1f));
            UpdateBar();
            if (Hp <= 0) Die();
        }

        void Die()
        {
            int n = Pen.MeatPerCrab;
            for (int i = 0; i < n; i++)
            {
                float a = (i / (float)n) * Mathf.PI * 2f + Random.value;
                var land = Pen.Clamp(transform.position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * Random.Range(0.8f, 1.6f));
                land.y = 0.08f;
                LooseItem.Spawn(ItemType.Meat, transform.position + Vector3.up * 0.8f, land);
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
            if (_spawnT < 1f)
            {
                _spawnT = Mathf.Min(1f, _spawnT + dt * 2.5f);
                _model.localScale = Vector3.one * Mathf.SmoothStep(0.05f, 1f, _spawnT);
            }
            // lazy wandering, sideways like a crab
            var d = _target - transform.position;
            d.y = 0;
            if (_wait > 0) _wait -= dt;
            else if (d.magnitude < 0.2f) { _wait = Random.Range(0.6f, 2.2f); _target = Pen.RandomPoint(); }
            else
            {
                transform.position += d.normalized * Mathf.Min(d.magnitude, 1.1f * dt);
                var face = Quaternion.LookRotation(Vector3.Cross(Vector3.up, d.normalized));
                transform.rotation = Quaternion.Slerp(transform.rotation, face, 4f * dt);
            }
            // squash when hit
            _flash = Mathf.MoveTowards(_flash, 0, dt * 5f);
            float sq = 1f + _flash * 0.25f;
            _model.localScale = new Vector3(sq, 1f / sq, sq) * Mathf.SmoothStep(0.05f, 1f, _spawnT);
        }
    }

    /// <summary>The fenced crab pen: keeps a number of crabs alive and respawns them after they're caught.</summary>
    public class CrabPen : MonoBehaviour
    {
        public int CrabCount = 4;
        public int MeatPerCrab = 3;
        public float RespawnSeconds = 2.5f;
        readonly List<Crab> _crabs = new List<Crab>();
        readonly List<(Crab c, float t)> _respawn = new List<(Crab, float)>();

        public Vector3 RandomPoint()
        {
            return new Vector3(Random.Range(Yard.PenMinX + 1.4f, Yard.PenMaxX - 1.4f), 0.05f, Random.Range(Yard.PenMinZ + 1.8f, Yard.PenMaxZ - 1.4f));
        }

        public Vector3 Clamp(Vector3 p)
        {
            p.x = Mathf.Clamp(p.x, Yard.PenMinX + 0.9f, Yard.PenMaxX - 0.9f);
            p.z = Mathf.Clamp(p.z, Yard.PenMinZ + 0.9f, Yard.PenMaxZ - 0.9f);
            return p;
        }

        public void SetCount(int n)
        {
            CrabCount = n;
            while (_crabs.Count < n)
            {
                var go = new GameObject("Crab " + _crabs.Count);
                go.transform.SetParent(transform, false);
                var c = go.AddComponent<Crab>();
                c.Init(this);
                _crabs.Add(c);
            }
        }

        public void QueueRespawn(Crab c) => _respawn.Add((c, RespawnSeconds));

        void Update()
        {
            for (int i = _respawn.Count - 1; i >= 0; i--)
            {
                var (c, t) = _respawn[i];
                t -= Time.deltaTime;
                if (t <= 0) { _respawn.RemoveAt(i); c.Revive(); }
                else _respawn[i] = (c, t);
            }
        }

        /// <summary>Nearest crab that can be hit, within <paramref name="range"/> of <paramref name="from"/>.</summary>
        public static Crab NearestAlive(Vector3 from, float range)
        {
            Crab best = null;
            float bd = range;
            foreach (var c in Crab.All)
            {
                if (!c.Alive) continue;
                var d = c.transform.position - from;
                d.y = 0;
                float m = d.magnitude;
                if (m < bd) { bd = m; best = c; }
            }
            return best;
        }
    }
}
