using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace StackIsland
{
    /// <summary>
    /// A buy spot painted on the floor: dashed outline, what it is, how far along it is (0/4) and the price.
    /// Stand still on it and your cash flies in bundle by bundle; walk off and it keeps what you paid.
    /// </summary>
    public class BuyPad : MonoBehaviour
    {
        public static readonly List<BuyPad> All = new List<BuyPad>();

        public string Id;
        public string Kind = "UPGRADE";          // HIRE / UNLOCK / UPGRADE
        public string Title = "";
        public int MaxLevel = 1;
        public Func<int, double> CostAt = l => 10;
        public Func<bool> Available = () => true;
        public Action<int> OnBought;            // called with the new level
        public Func<int, string> Describe = l => "";   // what the next level does (given the current level)
        public float Width = 3.2f, Depth = 2.5f;

        public int Level { get; private set; }
        public double Paid { get; private set; }
        public bool Maxed => Level >= MaxLevel;
        public double Cost => CostAt(Level);
        public double Remaining => Math.Max(0, Cost - Paid);
        public bool Shown => _root != null && _root.activeSelf;

        GameObject _root;
        Transform _fill;
        TextMeshPro _title, _price;
        float _stand, _flyTimer;
        string _lastText;
        Material _fillMat;
        Renderer _tile;
        static Material TileIdle => Mats.Lit(new Color(0.24f, 0.25f, 0.3f));
        static Material TileActive => Mats.Lit(new Color(0.3f, 0.72f, 0.36f));

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void Build()
        {
            _root = new GameObject("visual");
            _root.transform.SetParent(transform, false);
            // dark tile with a dashed white border; it lights up green while you stand on it
            _tile = ProcArt.MeshObject("tile", _root.transform, ProcArt.RoundedSlab(Width, Depth, 0.4f, 0.06f), TileIdle, Vector3.zero).GetComponent<Renderer>();
            _tile.receiveShadows = true;
            ProcArt.MeshObject("dash", _root.transform, ProcArt.DashedOutline(Width - 0.3f, Depth - 0.3f, 0.3f, 0.1f, 0.3f, 0.18f), Mats.Unlit(new Color(1f, 1f, 1f, 1f)), new Vector3(0, 0.075f, 0));
            // fill grows from the left edge as you pay
            var fillPivot = new GameObject("fillPivot").transform;
            fillPivot.SetParent(_root.transform, false);
            fillPivot.localPosition = new Vector3(-(Width - 0.45f) * 0.5f, 0.07f, 0);
            _fillMat = Mats.Unlit(new Color(0.38f, 0.82f, 0.42f));
            ProcArt.MeshObject("fill", fillPivot, ProcArt.RoundedRect(Width - 0.45f, Depth - 0.45f, 0.25f), _fillMat, new Vector3((Width - 0.45f) * 0.5f, 0, 0));
            _fill = fillPivot;
            _fill.localScale = new Vector3(0.0001f, 1, 1);
            // icon for what this is
            var iconRoot = new GameObject("icon").transform;
            iconRoot.SetParent(_root.transform, false);
            // layout: title along the top, icon on the left, price on the right (like the genre's floor tiles)
            iconRoot.localPosition = new Vector3(-Width * 0.25f, 0.08f, -Depth * 0.12f);
            iconRoot.localScale = Vector3.one * 1.5f;
            var ic = Color.white;
            if (Kind == "HIRE") ProcArt.PersonIcon(iconRoot, ic);
            else if (Kind == "UNLOCK") ProcArt.PlusIcon(iconRoot, ic);
            else ProcArt.ArrowIcon(iconRoot, ic);
            _title = ProcArt.FloorText(_root.transform, "", 2.3f, new Color(1f, 1f, 1f, 0.95f), new Vector3(0, 0.06f, Depth * 0.27f));
            _title.rectTransform.sizeDelta = new Vector2(Width - 0.5f, 0.6f);
            _title.enableAutoSizing = true;
            _title.fontSizeMin = 1.2f;
            _title.fontSizeMax = 2.3f;
            _price = ProcArt.FloorText(_root.transform, "", 5.2f, new Color(0.62f, 1f, 0.6f), new Vector3(Width * 0.16f, 0.06f, -Depth * 0.14f));
            Refresh();
        }

        public void Load(int level, double paid)
        {
            Level = Mathf.Clamp(level, 0, MaxLevel);
            Paid = Math.Max(0, paid);
            Refresh();
        }

        public void Refresh()
        {
            if (_root == null) return;
            bool show = !Maxed && Available();
            if (_root.activeSelf != show) _root.SetActive(show);
            if (!show) return;
            string lvl = MaxLevel > 1 ? $"  {Level}/{MaxLevel}" : "";
            string t = $"{Title}<size=80%>{lvl}</size>";
            if (t != _lastText) { _title.text = t; _lastText = t; }
            _price.text = NumberFormatter.Format(Remaining);
            float p = Cost <= 0 ? 1 : Mathf.Clamp01((float)(Paid / Cost));
            _fill.localScale = new Vector3(Mathf.Max(0.0001f, p), 1, 1);
        }

        public bool PlayerOn(Vector3 p)
        {
            var local = transform.InverseTransformPoint(p);
            return Mathf.Abs(local.x) < Width * 0.5f && Mathf.Abs(local.z) < Depth * 0.5f;
        }

        void Update()
        {
            var g = Game.I;
            if (g == null || g.Player == null) return;
            if (!Shown || Maxed) { _stand = 0; return; }
            var player = g.Player;
            bool on = PlayerOn(player.transform.position);
            var mat = on ? TileActive : TileIdle;
            if (_tile.sharedMaterial != mat) _tile.sharedMaterial = mat;
            if (!on || player.IsMoving || g.InputBlocked)
            {
                _stand = 0;
                return;
            }
            _stand += Time.deltaTime;
            if (_stand < 0.25f || g.Cash <= 0) return;

            // pays faster the longer you stand, but always finishes in a couple of seconds
            double rate = Math.Max(Cost / 1.5, 8.0) * (1.0 + _stand);
            double amount = Math.Min(Math.Min(rate * Time.deltaTime, g.Cash), Remaining);
            if (amount <= 0 || !g.Spend(amount)) return;
            Paid += amount;
            _flyTimer -= Time.deltaTime;
            if (_flyTimer <= 0)
            {
                _flyTimer = 0.06f;
                player.FlyCashTo(transform.position + Vector3.up * 0.1f);
                Sfx.I?.Play(Sfx.I.Tick, 0.3f, 0.8f + (float)(Paid / Math.Max(1, Cost)) * 1.0f);
            }
            if (Remaining <= 1e-6) Complete();
            else Refresh();
        }

        void Complete()
        {
            Paid = 0;
            Level++;
            _stand = -0.5f;
            Fx.I?.Puff(transform.position + Vector3.up * 0.3f, 30);
            Fx.I?.Confetti(transform.position, 60);
            Sfx.I?.Play(Sfx.I.Buy, 0.7f, 1f);
            OnBought?.Invoke(Level);
            Game.I?.OnPurchase(this);
            foreach (var p in All) p.Refresh();
        }

        public string Info()
        {
            string lvl = MaxLevel > 1 ? $"  <size=80%>Lv {Level}/{MaxLevel}</size>" : "";
            if (Maxed) return $"<b>{Title}</b>{lvl}\n<color=#9CFF9A>Maxed out</color>";
            string paid = Paid > 0 ? $" ({NumberFormatter.Format(Paid)} paid)" : "";
            return $"<b>{Title}</b>{lvl}\n{Describe(Level)}\nCost: <color=#9CFF9A>{NumberFormatter.Format(Remaining)}</color>{paid}\n<size=80%><color=#B8C8D4>Stand still on it to pay</color></size>";
        }

        /// <summary>Pay the rest in one go if the wallet covers it (the Island Manager does this).</summary>
        public bool TryBuyNow()
        {
            if (Maxed || !Shown || Game.I == null) return false;
            double need = Remaining;
            if (need > 0 && !Game.I.Spend(need)) return false;
            Paid = Cost;
            Complete();
            return true;
        }

        /// <summary>Tests / debug: buy without paying.</summary>
        public void ForceBuy()
        {
            if (Maxed) return;
            Complete();
        }
    }
}
