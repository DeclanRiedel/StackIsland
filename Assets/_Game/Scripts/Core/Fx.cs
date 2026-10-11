using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace StackIsland
{
    /// <summary>Visual-only juice: puffs, confetti, sword slashes, chimney smoke, floating text.</summary>
    public class Fx : MonoBehaviour
    {
        public static Fx I;
        ParticleSystem _confetti, _puff;

        class Slasher { public Transform t; public Material m; public float life; }
        readonly List<Slasher> _slashes = new List<Slasher>();
        readonly Stack<Slasher> _slashPool = new Stack<Slasher>();

        class Floater { public TextMeshPro text; public float t; public Vector3 start; }
        readonly List<Floater> _floaters = new List<Floater>();

        void Awake()
        {
            I = this;
            _confetti = MakeParticles("confetti", new[] { new Color(1f, .8f, .2f), new Color(.3f, .8f, 1f), new Color(1f, .4f, .6f), new Color(.5f, 1f, .5f) }, 0.22f, 8f, 1.8f, 1.2f);
            _puff = MakeParticles("puff", new[] { new Color(1, 1, 1, .9f) }, 0.55f, 2.5f, 0.6f, 0.2f);
        }

        ParticleSystem MakeParticles(string n, Color[] colors, float size, float speed, float life, float gravity)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = life;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.gravityModifier = gravity;
            main.maxParticles = 600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var grad = new Gradient();
            var keys = new GradientColorKey[colors.Length];
            for (int i = 0; i < colors.Length; i++) keys[i] = new GradientColorKey(colors[i], colors.Length == 1 ? 0 : i / (float)(colors.Length - 1));
            grad.SetKeys(keys, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            main.startColor = new ParticleSystem.MinMaxGradient(grad) { mode = ParticleSystemGradientMode.RandomColor };
            var em = ps.emission;
            em.rateOverTime = 0;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.4f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.Unlit(Color.white);
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-6, 6);
            return ps;
        }

        public void Confetti(Vector3 pos, int count = 80)
        {
            _confetti.transform.position = pos + Vector3.up;
            _confetti.Emit(count);
        }

        public void Puff(Vector3 pos, int count = 20)
        {
            _puff.transform.position = pos;
            _puff.Emit(count);
        }

        /// <summary>Soft looping smoke from a chimney. Returns the system so the owner can change its rate.</summary>
        public static ParticleSystem Smoke(Transform at)
        {
            var go = new GameObject("smoke");
            go.transform.SetParent(at, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 2.2f;
            main.startSpeed = 0.9f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.3f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(0.95f, 0.95f, 0.95f, 0.5f);
            main.gravityModifier = -0.05f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 10;
            shape.radius = 0.12f;
            shape.rotation = new Vector3(-90, 0, 0);
            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 1.7f));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0.5f, 0), new GradientAlphaKey(0, 1) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.UnlitTransparent(Color.white);
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            var em = ps.emission;
            em.rateOverTime = 2;
            ps.Play();
            return ps;
        }

        /// <summary>A quick white arc in front of an attack.</summary>
        public void Slash(Vector3 pos, Vector3 dir)
        {
            var s = _slashPool.Count > 0 ? _slashPool.Pop() : NewSlash();
            s.t.gameObject.SetActive(true);
            s.t.position = pos;
            s.t.rotation = Quaternion.LookRotation(dir.sqrMagnitude > 0.001f ? dir : Vector3.forward) * Quaternion.Euler(0, 0, Random.Range(-25f, 25f));
            s.life = 0;
            _slashes.Add(s);
        }

        Slasher NewSlash()
        {
            var go = new GameObject("slash");
            go.transform.SetParent(transform);
            var m = Mats.NewFadeable(new Color(1f, 1f, 1f, 0.9f));
            go.AddComponent<MeshFilter>().sharedMesh = ArcMesh();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return new Slasher { t = go.transform, m = m };
        }

        static Mesh _arc;
        static Mesh ArcMesh()
        {
            if (_arc != null) return _arc;
            var v = new List<Vector3>();
            var tris = new List<int>();
            const int seg = 12;
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.Lerp(-70, 70, i / (float)seg) * Mathf.Deg2Rad;
                float w = Mathf.Sin(i / (float)seg * Mathf.PI) * 0.22f;
                var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                v.Add(dir * 0.75f);
                v.Add(dir * (0.75f - w));
            }
            for (int i = 0; i < seg; i++)
            {
                int k = i * 2;
                tris.AddRange(new[] { k, k + 2, k + 1, k + 1, k + 2, k + 3, k, k + 1, k + 2, k + 1, k + 3, k + 2 });
            }
            _arc = new Mesh();
            _arc.SetVertices(v);
            _arc.SetTriangles(tris, 0);
            _arc.RecalculateNormals();
            return _arc;
        }

        public void FloatText(Vector3 pos, string text, Color color, float size = 3f)
        {
            var t = Billboard.Label(transform, Vector3.zero, size, color, text);
            t.transform.position = pos;
            _floaters.Add(new Floater { text = t, start = pos });
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _slashes.Count - 1; i >= 0; i--)
            {
                var s = _slashes[i];
                s.life += dt / 0.16f;
                s.t.localScale = Vector3.one * (0.8f + s.life * 0.6f);
                var c = s.m.GetColor("_BaseColor");
                c.a = Mathf.Clamp01(1 - s.life) * 0.9f;
                s.m.SetColor("_BaseColor", c);
                if (s.life >= 1) { s.t.gameObject.SetActive(false); _slashes.RemoveAt(i); _slashPool.Push(s); }
            }
            for (int i = _floaters.Count - 1; i >= 0; i--)
            {
                var f = _floaters[i];
                f.t += dt;
                f.text.transform.position = f.start + Vector3.up * f.t * 1.4f;
                f.text.alpha = Mathf.Clamp01(1.4f - f.t);
                if (f.t > 1.4f) { Destroy(f.text.gameObject); _floaters.RemoveAt(i); }
            }
        }
    }
}
