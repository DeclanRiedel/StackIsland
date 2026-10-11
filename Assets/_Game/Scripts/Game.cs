using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace StackIsland
{
    /// <summary>
    /// Composition root: builds the island and the yards that are open, the player and camera, every buy tile
    /// (all progression lives here and in <see cref="YardDefs"/>, as data), the UI, and handles cash and saving.
    /// </summary>
    public class Game : MonoBehaviour
    {
        public static Game I { get; private set; }
        public static bool TestMode;        // tests / autoshot: no menu, throwaway save
        public static bool FreshStart;      // wipe the save on the next load
        public static bool SkipMenuOnce;

        public SaveData Save { get; private set; }
        public SettingsData Settings { get; private set; }
        public Player Player { get; private set; }
        public FollowCamera Cam { get; private set; }
        public Hud Hud { get; private set; }
        public readonly Dictionary<int, Yard> Yards = new Dictionary<int, Yard>();
        readonly Dictionary<string, BuyPad> _pads = new Dictionary<string, BuyPad>();
        Transform _world;
        float _autosave;
        bool _loading;

        public double Cash => Save.cash;
        public bool InputBlocked => Hud != null && Hud.IsMenuOpen;
        public bool PointerOverUI => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        public Yard Yard1 => Yards.TryGetValue(1, out var y) ? y : null;

        void Awake()
        {
            I = this;
            PileZone.All.Clear();
            BuyPad.All.Clear();
            LooseItem.All.Clear();
            Animal.All.Clear();
            Items.ResetPool();

            var art = Resources.Load<ArtLibrary>("ArtLibrary");
            Mats.Init(art);
            Visuals.Init(art);
            GameInput.Init();
            Settings = SaveSystem.LoadSettings();
            ApplyWindowMode();
            if (FreshStart) { SaveSystem.Delete(); FreshStart = false; }
            Save = (TestMode ? null : SaveSystem.Load()) ?? new SaveData();

            new GameObject("Tweens").AddComponent<Tweens>();
            new GameObject("Fx").AddComponent<Fx>();
            var sfx = new GameObject("Sfx").AddComponent<Sfx>();
            sfx.Volume = Settings.volume;
            SetupLighting();

            _world = new GameObject("World").transform;
            IslandBuilder.Build(_world, YardDefs.All.Count);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 34f;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 220f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.2f, 0.62f, 0.9f);
            camGo.AddComponent<AudioListener>();
            Cam = camGo.AddComponent<FollowCamera>();

            var pg = new GameObject("Player");
            Player = pg.AddComponent<Player>();
            Player.Init();
            Cam.Target = Player.transform;

            _loading = true;
            OpenYard(1);
            LoadState();
            _loading = false;
            Player.Teleport(Save.px != 0 || Save.pz != 0 ? new Vector3(Save.px, 0, Save.pz) : new Vector3(0, 0, -3));

            Hud = new GameObject("UI").AddComponent<Hud>();
            Hud.Init(this);
        }

        void Start()
        {
            Cam.Snap();
            if (!TestMode && !SkipMenuOnce) Hud.ShowMainMenu();
            SkipMenuOnce = false;
        }

        // ------------------------------------------------------------------ yards

        /// <summary>Build a yard (first time it opens, or on load) with all its tiles.</summary>
        Yard OpenYard(int index)
        {
            if (Yards.TryGetValue(index, out var existing)) return existing;
            var def = YardDefs.Get(index);
            if (def == null) return null;
            var y = new GameObject("Yard " + index + " " + def.Name).AddComponent<Yard>();
            y.transform.SetParent(_world, false);
            y.Build(def);
            Yards[index] = y;
            BuildYardPads(y);
            if (index == 1) BuildGlobalPads(y);
            // apply already-owned levels (load) and saved piles
            // (snapshot: a bought gate opens the next yard from inside this loop, which adds more tiles)
            foreach (var p in new List<BuyPad>(BuyPad.All))
                if (p.Id.StartsWith($"y{index}.") || (index == 1 && p.Id.StartsWith("g.")))
                {
                    var s = Save.Pad(p.Id);
                    p.Load(s.level, s.paid);
                    if (s.level > 0) p.OnBought?.Invoke(s.level);
                }
            foreach (var z in y.GetComponentsInChildren<PileZone>())
            {
                if (string.IsNullOrEmpty(z.Id)) continue;
                var s = Save.Pile(z.Id);
                if (s.count > 0) z.SetCount(s.count, s.cash);
            }
            if (Yards.TryGetValue(index - 1, out var prev)) prev.OpenEastGate();
            if (!_loading)
            {
                Fx.I?.Confetti(y.W(0, 0), 200);
                Hud?.Toast($"Welcome to {def.Name}!");
            }
            foreach (var p in BuyPad.All) p.Refresh();
            return y;
        }

        public Yard YardAt(Vector3 p)
        {
            foreach (var y in Yards.Values) if (y.Contains(p)) return y;
            return null;
        }

        public double PriceBonus(int yard) => 1.0 + 0.2 * Level($"y{yard}.recipe");

        // ------------------------------------------------------------------ progression (all tiles)

        static double Curve(double baseCost, double growth, int level) => baseCost * System.Math.Pow(growth, level);

        void BuildYardPads(Yard y)
        {
            var d = y.Def;
            string k = $"y{d.Index}.";
            double s = d.Scale;
            float baseCook = d.CookSeconds;

            Pad(y, k + "src", "UPGRADE", d.SourceTitle, new Vector3(-8.2f, 0, 1.4f), 8, l => Curve(30, 1.7, l) * s, () => true,
                l => { if (y.Pen != null) { y.Pen.SetCount(4 + 2 * l); y.Pen.RespawnSeconds = 2.5f * Mathf.Pow(0.9f, l); } if (y.Field != null) y.Field.SetLevel(l); },
                l => y.Pen != null ? $"+2 {d.AnimalName}s, and they come back faster ({4 + 2 * l} > {6 + 2 * l})" : "More of the field fills up, and it regrows faster");
            Pad(y, k + "gather", "HIRE", d.GathererName, new Vector3(-2.6f, 0, -1.4f), 4, l => Curve(50, 2.6, l) * s, () => Save.totalEarned >= 15 * s || Level(k + "gather") > 0,
                l => SyncWorkers(y),
                l => $"A helper who gathers {Items.Name(d.Raw)} and brings it to the {d.StationName} ({l} > {l + 1} hired)");
            Pad(y, k + "speed", "UPGRADE", d.SpeedTitle, new Vector3(10.9f, 0, 13f), 12, l => Curve(100, 1.55, l) * s, () => Level(k + "gather") >= 1,
                l => { foreach (var st in y.Stations) st.CookSeconds = baseCook * Mathf.Pow(0.86f, l); },
                l => $"{d.StationName} works 16% faster ({baseCook * Mathf.Pow(0.86f, l):0.00}s > {baseCook * Mathf.Pow(0.86f, l + 1):0.00}s each)");
            Pad(y, k + "waiter", "HIRE", "Waiter", new Vector3(10.9f, 0, -3.6f), 3, l => Curve(120, 2.6, l) * s, () => Level(k + "gather") >= 1,
                l => SyncWorkers(y),
                l => $"A helper who carries {Items.Name(d.Product)} from the {d.StationName} to the counter ({l} > {l + 1} hired)");
            Pad(y, k + "parrot", "HIRE", "Cash Parrot", new Vector3(5.6f, 0, -14.8f), 1, l => 180 * s, () => Level(k + "gather") >= 1,
                l => { if (l >= 1) y.Counter.EnableAutoCash(); },
                l => "Collects every customer's cash for you, straight into your wallet");
            Pad(y, k + "station2", "UNLOCK", "Second " + d.StationName, new Vector3(6f, 0, 3.4f), 1, l => 400 * s, () => Level(k + "speed") >= 1 || Level(k + "waiter") >= 1,
                l => { if (l >= 1 && y.Stations.Count < 2) { var st = y.AddStation(); st.CookSeconds = baseCook * Mathf.Pow(0.86f, Level(k + "speed")); LoadPiles(st.gameObject); } },
                l => $"Builds a second {d.StationName} (twice the output)", 4.2f, 3.2f);
            Pad(y, k + "recipe", "UPGRADE", d.RecipeTitle, new Vector3(-8.2f, 0, -10.6f), 12, l => Curve(300, 1.6, l) * s, () => Level(k + "station2") >= 1,
                l => { },
                l => $"{Items.Name(d.Product)} sells for 20% more ({Items.Price(d.Product) * (1 + 0.2 * l):0.#} > {Items.Price(d.Product) * (1 + 0.2 * (l + 1)):0.#})");
            Pad(y, k + "busy", "UPGRADE", "More Customers", new Vector3(-2.6f, 0, -7.0f), 8, l => Curve(150, 1.65, l) * s, () => Level(k + "waiter") >= 1,
                l => { y.Counter.SpawnEvery = 2.2f * Mathf.Pow(0.88f, l); y.Counter.MaxWant = 2 + (l + 1) / 2; },
                l => $"Customers arrive 12% more often and buy more (up to {2 + (l + 2) / 2} each)");
            Pad(y, k + "crew", "UPGRADE", "Helper Shoes", new Vector3(-8.2f, 0, -7.6f), 6, l => Curve(200, 1.8, l) * s, () => Level(k + "waiter") >= 1,
                l => SyncWorkers(y),
                l => $"Helpers walk 12% faster and carry 2 more ({8 + 2 * l} > {10 + 2 * l})");

            var next = YardDefs.Get(d.Index + 1);
            if (next != null)
                Pad(y, k + "gate", "UNLOCK", "To " + next.Name, new Vector3(10.9f, 0, 6.2f), 1, l => next.GateCost, () => Level(k + "station2") >= 1,
                    l => { if (l >= 1) OpenYard(next.Index); },
                    l => $"Opens the gate to the next yard: {next.Name}. {next.Tagline}");
        }

        void BuildGlobalPads(Yard y)
        {
            Pad(y, "g.backpack", "UPGRADE", "Bigger Stack", new Vector3(-8.2f, 0, -4.6f), 15, l => Curve(25, 1.75, l), () => true,
                l => Player.Stack.Capacity = 12 + 4 * l,
                l => $"Carry 4 more items ({12 + 4 * l} > {16 + 4 * l})");
            Pad(y, "g.shoes", "UPGRADE", "Running Shoes", new Vector3(-2.6f, 0, -12.4f), 8, l => Curve(60, 2.2, l), () => Save.totalEarned >= 100,
                l => Player.Speed = 5.6f * (1 + 0.08f * l),
                l => $"Walk 8% faster");
        }

        void SyncWorkers(Yard y)
        {
            string k = $"y{y.Def.Index}.";
            int crew = Level(k + "crew");
            float speed = 3.6f * (1 + 0.12f * crew);
            int cap = 8 + 2 * crew;
            while (y.Gatherers.Count < Level(k + "gather"))
            {
                var w = new GameObject("Gatherer").AddComponent<CatcherWorker>();
                w.transform.SetParent(y.transform, false);
                w.Init(y, y.Gatherers.Count % 2 == 0 ? "mini-characters/character-male-f" : "mini-characters/character-male-d", new Color(0.95f, 0.3f, 0.3f), cap, y.W(-2.6f, -1.4f));
                y.Gatherers.Add(w);
                if (!_loading) Fx.I?.Puff(w.transform.position + Vector3.up, 20);
            }
            while (y.Waiters.Count < Level(k + "waiter"))
            {
                var w = new GameObject("Waiter").AddComponent<WaiterWorker>();
                w.transform.SetParent(y.transform, false);
                w.Init(y, "mini-characters/character-female-f", new Color(0.25f, 0.6f, 1f), cap + 2, y.W(10.9f, -3.6f));
                y.Waiters.Add(w);
                if (!_loading) Fx.I?.Puff(w.transform.position + Vector3.up, 20);
            }
            foreach (var w in y.Gatherers) { w.Speed = speed; w.SetCapacity(cap); }
            foreach (var w in y.Waiters) { w.Speed = speed; w.SetCapacity(cap + 2); }
        }

        void Pad(Yard y, string id, string kind, string title, Vector3 local, int maxLevel, System.Func<int, double> cost, System.Func<bool> available,
                 System.Action<int> apply, System.Func<int, string> describe, float w = 3.2f, float d = 2.5f)
        {
            var go = new GameObject("Pad " + id);
            go.transform.SetParent(y.transform, false);
            go.transform.position = y.W(local);
            var p = go.AddComponent<BuyPad>();
            p.Id = id;
            p.Kind = kind;
            p.Title = title;
            p.MaxLevel = maxLevel;
            p.CostAt = cost;
            p.Available = available;
            p.OnBought = apply;
            p.Describe = describe;
            p.Width = w;
            p.Depth = d;
            p.Build();
            _pads[id] = p;
        }

        public int Level(string id) => _pads.TryGetValue(id, out var p) ? p.Level : 0;
        public BuyPad PadById(string id) => _pads.TryGetValue(id, out var p) ? p : null;

        public void OnPurchase(BuyPad pad)
        {
            Hud?.Toast(pad.Maxed && pad.MaxLevel > 1 ? $"{pad.Title} maxed!" : $"{pad.Title} {(pad.MaxLevel > 1 ? $"{pad.Level}/{pad.MaxLevel}" : "")}");
            SaveNow();
        }

        /// <summary>Hover info for whatever is at world point <paramref name="p"/> (null = nothing interesting).</summary>
        public string InfoAt(Vector3 p)
        {
            foreach (var pad in BuyPad.All)
                if (pad.Shown && pad.PlayerOn(p)) return pad.Info();
            var y = YardAt(p);
            if (y == null) return null;
            foreach (var st in y.Stations)
            {
                var d = st.transform.position - p;
                d.y = 0;
                if (d.magnitude < 2.4f || st.Output.InReach(p, -0.3f)) return st.Info();
                foreach (var z in st.Inputs) if (z.InReach(p, -0.3f)) return st.Info();
            }
            if (y.InSource(p)) return y.SourceInfo(Player.Damage);
            var c = y.Counter.transform.position - p;
            c.y = 0;
            if ((Mathf.Abs(c.x) < 3.4f && Mathf.Abs(c.z) < 1.2f) || y.Counter.Shelf.InReach(p) || y.Counter.Cash.InReach(p)) return y.Counter.Info();
            return null;
        }

        // ------------------------------------------------------------------ cash

        public void AddCash(double v, Vector3 from)
        {
            if (v <= 0) return;
            Save.cash += v;
            Save.totalEarned += v;
            Player.FlyCashFrom(from);
            Sfx.I?.Play(Sfx.I.Coin, 0.4f, Random.Range(0.95f, 1.15f));
            foreach (var p in BuyPad.All) p.Refresh();
        }

        public bool Spend(double v)
        {
            if (v <= 0 || Save.cash + 1e-9 < v) return false;
            Save.cash = System.Math.Max(0, Save.cash - v);
            return true;
        }

        /// <summary>Debug / autoshot.</summary>
        public void Grant(double v) { Save.cash += v; foreach (var p in BuyPad.All) p.Refresh(); }

        // ------------------------------------------------------------------ save / load

        void LoadState()
        {
            // yards beyond the first open as their gates were bought (in order)
            for (int i = 1; i <= YardDefs.All.Count; i++)
                if (Save.Pad($"y{i}.gate").level > 0) OpenYard(i + 1);
                else break;
            foreach (var t in Save.stack) Player.Stack.Add((ItemType)t);
        }

        void LoadPiles(GameObject root)
        {
            foreach (var z in root.GetComponentsInChildren<PileZone>())
            {
                if (string.IsNullOrEmpty(z.Id)) continue;
                var s = Save.Pile(z.Id);
                if (s.count > 0 && z.Count == 0) z.SetCount(s.count, s.cash);
            }
        }

        public void SaveNow()
        {
            _autosave = 0;
            if (TestMode) return;
            Save.px = Player.transform.position.x;
            Save.pz = Player.transform.position.z;
            Save.stack.Clear();
            foreach (var t in Player.Stack.ItemsHeld) Save.stack.Add((int)t);
            foreach (var p in _pads.Values)
            {
                var s = Save.Pad(p.Id);
                s.level = p.Level;
                s.paid = p.Paid;
            }
            foreach (var z in PileZone.All)
            {
                if (string.IsNullOrEmpty(z.Id)) continue;
                var s = Save.Pile(z.Id);
                s.count = z.Count;
                s.cash = z.CashValue;
            }
            SaveSystem.Save(Save);
        }

        void Update()
        {
            Save.playTime += Time.unscaledDeltaTime;
            _autosave += Time.unscaledDeltaTime;
            if (_autosave > 20f) SaveNow();
        }

        void OnApplicationQuit() { if (Player != null) SaveNow(); }

        public void NewGame()
        {
            FreshStart = true;
            SkipMenuOnce = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // ------------------------------------------------------------------ presentation

        public void ApplyWindowMode()
        {
            if (Application.isEditor || TestMode) return;
            if (Settings.windowed)
            {
                if (Screen.fullScreenMode != FullScreenMode.Windowed) Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
            }
            else
            {
                var r = Screen.currentResolution;
                Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
            }
        }

        void SetupLighting()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.97f, 0.9f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.45f;
            sun.transform.rotation = Quaternion.Euler(55f, -25f, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.78f, 0.82f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.72f, 0.7f, 0.66f);
            RenderSettings.ambientGroundColor = new Color(0.55f, 0.5f, 0.45f);
            RenderSettings.fog = false;
            QualitySettings.shadowDistance = 60f;
        }

        void OnDestroy() { if (I == this) I = null; }
    }
}
