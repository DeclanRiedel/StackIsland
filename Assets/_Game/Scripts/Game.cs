using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace StackIsland
{
    /// <summary>
    /// Composition root: builds the yard, its stations, the player and camera, the buy pads (all progression
    /// lives here, as data), the UI, and handles cash and saving.
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
        public Counter Counter { get; private set; }
        public CrabPen Pen { get; private set; }
        public Hud Hud { get; private set; }
        public readonly List<Grill> Grills = new List<Grill>();
        readonly List<CatcherWorker> _catchers = new List<CatcherWorker>();
        readonly List<WaiterWorker> _waiters = new List<WaiterWorker>();
        readonly Dictionary<string, BuyPad> _pads = new Dictionary<string, BuyPad>();
        Transform _world;
        float _autosave;

        public double Cash => Save.cash;
        public bool InputBlocked => Hud != null && Hud.IsMenuOpen;
        public bool PointerOverUI => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        public double SellBonus => 1.0 + 0.3 * Level("recipe");

        void Awake()
        {
            I = this;
            PileZone.All.Clear();
            BuyPad.All.Clear();
            LooseItem.All.Clear();
            Crab.All.Clear();
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
            var yard = new GameObject("Yard").AddComponent<Yard>();
            yard.transform.SetParent(_world, false);
            yard.Build();

            Pen = new GameObject("CrabPen").AddComponent<CrabPen>();
            Pen.transform.SetParent(_world, false);
            Pen.SetCount(4);

            AddGrill("grill1", new Vector3(6, 0, 11));

            Counter = new GameObject("Counter").AddComponent<Counter>();
            Counter.transform.SetParent(_world, false);
            Counter.transform.position = new Vector3(4.5f, 0, -10f);
            Counter.Build("counter1");

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
            Player.Teleport(Save.px != 0 || Save.pz != 0 ? new Vector3(Save.px, 0, Save.pz) : new Vector3(0, 0, -3));
            Cam.Target = Player.transform;

            BuildPads();
            LoadState();

            Hud = new GameObject("UI").AddComponent<Hud>();
            Hud.Init(this);
        }

        void Start()
        {
            Cam.Snap();
            if (!TestMode && !SkipMenuOnce) Hud.ShowMainMenu();
            SkipMenuOnce = false;
        }

        // ------------------------------------------------------------------ world pieces

        Grill AddGrill(string id, Vector3 pos)
        {
            var g = new GameObject("Grill " + id).AddComponent<Grill>();
            g.transform.SetParent(_world, false);
            g.transform.position = pos;
            g.Build(id);
            g.CookSeconds = 1.1f * Mathf.Pow(0.78f, Level("hotgrill"));
            Grills.Add(g);
            return g;
        }

        public PileZone BestGrillInput()
        {
            PileZone best = null;
            foreach (var g in Grills)
                if (best == null || g.Input.Free > best.Free) best = g.Input;
            return best != null && best.Free > 0 ? best : null;
        }

        public PileZone BestGrillOutput()
        {
            PileZone best = null;
            foreach (var g in Grills)
                if (g.Output.Count > 0 && (best == null || g.Output.Count > best.Count)) best = g.Output;
            return best;
        }

        // ------------------------------------------------------------------ progression (all pads)

        static double[] Costs(params double[] c) => c;

        void BuildPads()
        {
            Pad("crabs", "UPGRADE", "More Crabs", new Vector3(-8.2f, 0, 1.4f), Costs(30, 90, 220, 500),
                () => true, l => Pen.SetCount(4 + 2 * l));
            Pad("backpack", "UPGRADE", "Bigger Stack", new Vector3(-8.2f, 0, -4.6f), Costs(25, 80, 200, 450),
                () => true, l => Player.Stack.Capacity = 12 + 6 * l);
            Pad("catcher", "HIRE", "Crab Catcher", new Vector3(-2.6f, 0, -1.4f), Costs(50, 150, 400),
                () => Save.totalEarned >= 15, l => SyncWorkers());
            Pad("hotgrill", "UPGRADE", "Hotter Grill", new Vector3(10.9f, 0, 13f), Costs(100, 250, 600, 1200),
                () => Level("catcher") >= 1, l => { foreach (var g in Grills) g.CookSeconds = 1.1f * Mathf.Pow(0.78f, l); });
            Pad("waiter", "HIRE", "Waiter", new Vector3(10.9f, 0, -3.6f), Costs(120, 350),
                () => Level("catcher") >= 1, l => SyncWorkers());
            Pad("grill2", "UNLOCK", "Second Grill", new Vector3(6f, 0, 3.4f), Costs(400),
                () => Level("hotgrill") >= 1 || Level("waiter") >= 1, l => { if (l >= 1 && Grills.Count < 2) AddGrill("grill2", new Vector3(6, 0, 3)); },
                4.2f, 3.2f);
            Pad("recipe", "UPGRADE", "Secret Recipe", new Vector3(-8.2f, 0, -10.6f), Costs(300, 800, 2000),
                () => Level("grill2") >= 1, l => { });
        }

        void Pad(string id, string kind, string title, Vector3 pos, double[] costs, System.Func<bool> available, System.Action<int> apply, float w = 3.2f, float d = 2.5f)
        {
            var go = new GameObject("Pad " + id);
            go.transform.SetParent(_world, false);
            go.transform.position = pos;
            var p = go.AddComponent<BuyPad>();
            p.Id = id;
            p.Kind = kind;
            p.Title = title;
            p.MaxLevel = costs.Length;
            p.CostAt = l => costs[Mathf.Clamp(l, 0, costs.Length - 1)];
            p.Available = available;
            p.OnBought = apply;
            p.Width = w;
            p.Depth = d;
            p.Build();
            _pads[id] = p;
        }

        public int Level(string id) => _pads.TryGetValue(id, out var p) ? p.Level : 0;

        public void OnPurchase(BuyPad pad)
        {
            Counter.MaxWant = Mathf.Min(5, 2 + TotalLevels() / 3);
            Hud?.Toast(pad.Maxed ? $"{pad.Title} maxed!" : $"{pad.Title} {(pad.MaxLevel > 1 ? $"{pad.Level}/{pad.MaxLevel}" : "")}");
            SaveNow();
        }

        int TotalLevels()
        {
            int n = 0;
            foreach (var p in _pads.Values) n += p.Level;
            return n;
        }

        void SyncWorkers()
        {
            int wantCatchers = Level("catcher"), wantWaiters = Level("waiter");
            while (_catchers.Count < wantCatchers)
            {
                var w = new GameObject("Catcher").AddComponent<CatcherWorker>();
                w.transform.SetParent(_world, false);
                w.Init(_catchers.Count % 2 == 0 ? "mini-characters/character-male-f" : "mini-characters/character-male-d", new Color(0.95f, 0.3f, 0.3f), 8, new Vector3(-2.6f, 0, -1.4f));
                _catchers.Add(w);
                Fx.I?.Puff(w.transform.position + Vector3.up, 20);
            }
            while (_waiters.Count < wantWaiters)
            {
                var w = new GameObject("Waiter").AddComponent<WaiterWorker>();
                w.transform.SetParent(_world, false);
                w.Init("mini-characters/character-female-f", new Color(0.25f, 0.6f, 1f), 10, new Vector3(10.9f, 0, -3.6f));
                _waiters.Add(w);
                Fx.I?.Puff(w.transform.position + Vector3.up, 20);
            }
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
            foreach (var p in _pads.Values)
            {
                var s = Save.Pad(p.Id);
                p.Load(s.level, s.paid);
                if (s.level > 0) p.OnBought?.Invoke(s.level);
            }
            foreach (var z in PileZone.All)
            {
                if (string.IsNullOrEmpty(z.Id)) continue;
                var s = Save.Pile(z.Id);
                if (s.count > 0) z.SetCount(s.count, s.cash);
            }
            foreach (var t in Save.stack) Player.Stack.Add((ItemType)t);
            Counter.MaxWant = Mathf.Min(5, 2 + TotalLevels() / 3);
            foreach (var p in BuyPad.All) p.Refresh();
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
