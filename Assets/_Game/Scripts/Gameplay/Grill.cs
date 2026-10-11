using UnityEngine;

namespace StackIsland
{
    /// <summary>
    /// Clay dome grill: drop raw crab meat on the left tray, grilled crab cakes pile up on the right tray.
    /// It works by itself whenever it has meat and room - nothing is ever lost.
    /// </summary>
    public class Grill : MonoBehaviour
    {
        public string Id;
        public PileZone Input, Output;
        public float CookSeconds = 1.1f;
        float _t;
        Transform _mouth, _chimney;
        ParticleSystem _smoke;
        Transform _fire;

        public bool Working { get; private set; }

        public void Build(string id)
        {
            Id = id;
            var oven = ProcArt.DomeOven(transform, out _mouth, out _chimney);
            _fire = oven.transform.Find("fire");
            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 1.1f, 0);
            col.size = new Vector3(3.2f, 2.2f, 2.6f);

            Input = MakeTray("in", new Vector3(-2.9f, 0, -2.6f), ItemType.Meat, true, false, new Color(0.96f, 0.55f, 0.5f));
            Output = MakeTray("out", new Vector3(2.9f, 0, -2.6f), ItemType.CrabCake, false, true, new Color(1f, 0.82f, 0.45f));
            _smoke = Fx.Smoke(_chimney);
        }

        PileZone MakeTray(string name, Vector3 local, ItemType type, bool drop, bool take, Color rim)
        {
            var go = new GameObject("tray_" + name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = local;
            ProcArt.Tray(go.transform, 1.9f, 1.6f, rim);
            var disp = new GameObject("display").transform;
            disp.SetParent(go.transform, false);
            disp.localPosition = new Vector3(0, 0.14f, 0);
            var z = go.AddComponent<PileZone>();
            z.Id = Id + ":" + name;
            z.Type = type;
            z.Capacity = 40;
            z.Drop = drop;
            z.Take = take;
            z.Radius = 1.5f;
            z.Display = disp;
            z.Columns = 3;
            z.Rows = 2;
            z.Spacing = new Vector2(0.55f, 0.5f);
            return z;
        }

        void Update()
        {
            Working = Input.Count > 0 && !Output.IsFull;
            if (_smoke != null)
            {
                var em = _smoke.emission;
                em.rateOverTime = Working ? 7f : 0.6f;
            }
            if (_fire != null) _fire.gameObject.SetActive(Working);
            if (!Working) { _t = 0; return; }
            _t += Time.deltaTime;
            if (_t < CookSeconds) return;
            _t = 0;
            Input.Remove(out _, out _);
            Output.Add(_mouth.position);
        }
    }
}
