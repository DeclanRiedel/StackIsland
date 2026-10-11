using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace StackIsland
{
    /// <summary>
    /// Minimal UI in the genre's style: just a cash counter (top right) while playing, short toasts, a start
    /// menu and an Esc menu.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        Game _g;
        RectTransform _root;
        TextMeshProUGUI _cash;
        RectTransform _cashBox;
        double _shown;
        float _pulse;
        RectTransform _toasts;
        readonly List<(RectTransform rt, CanvasGroup cg, float t)> _toastList = new List<(RectTransform, CanvasGroup, float)>();
        GameObject _menu, _pause, _hudLayer;
        Button _continueBtn;
        Toggle _windowed;

        public bool IsMenuOpen => (_menu != null && _menu.activeSelf) || (_pause != null && _pause.activeSelf);

        public void Init(Game g)
        {
            _g = g;
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }
            var cgo = new GameObject("Canvas", typeof(RectTransform));
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            cgo.AddComponent<GraphicRaycaster>();
            _root = (RectTransform)cgo.transform;

            _hudLayer = UIKit.Rect("hud", _root).gameObject;
            UIKit.Stretch((RectTransform)_hudLayer.transform);
            BuildCash();
            BuildToasts();
            BuildPause();
            BuildMenu();
            _shown = g.Cash;
        }

        // ------------------------------------------------------------------ cash counter

        void BuildCash()
        {
            var box = UIKit.Panel(_hudLayer.transform, new Color(0.08f, 0.1f, 0.14f, 0.55f), "cash");
            _cashBox = box.rectTransform;
            UIKit.Place(_cashBox, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -24), new Vector2(330, 92));
            // a green bill icon
            var bill = UIKit.Panel(_cashBox, new Color(0.33f, 0.78f, 0.36f), "bill");
            UIKit.Place(bill.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(78, 50));
            bill.rectTransform.localRotation = Quaternion.Euler(0, 0, 12);
            var band = UIKit.Panel(bill.rectTransform, new Color(0.95f, 0.97f, 0.9f), "band");
            UIKit.Place(band.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14, 50));
            _cash = UIKit.Text(_cashBox, "0", 56, TextAlignmentOptions.MidlineRight, Color.white, "value");
            _cash.fontStyle = FontStyles.Bold;
            _cash.outlineWidth = 0.18f;
            _cash.outlineColor = new Color32(0, 0, 0, 160);
            UIKit.Place(_cash.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(220, 80));
        }

        // ------------------------------------------------------------------ toasts

        void BuildToasts()
        {
            _toasts = UIKit.Rect("toasts", _hudLayer.transform);
            UIKit.Place(_toasts, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(800, 200));
            var v = UIKit.VLayout(_toasts.gameObject, 6, 0);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childForceExpandWidth = false;
        }

        public void Toast(string msg)
        {
            var bg = UIKit.Panel(_toasts, new Color(0.08f, 0.1f, 0.14f, 0.75f), "toast");
            var t = UIKit.Text(bg.rectTransform, msg, 28, TextAlignmentOptions.Center);
            t.fontStyle = FontStyles.Bold;
            UIKit.Stretch(t.rectTransform, 8);
            var le = bg.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 52;
            le.preferredWidth = Mathf.Min(780, 50 + t.GetPreferredValues(msg, 2000, 50).x);
            var cg = bg.gameObject.AddComponent<CanvasGroup>();
            _toastList.Add((bg.rectTransform, cg, 0f));
            while (_toastList.Count > 3) { Destroy(_toastList[0].rt.gameObject); _toastList.RemoveAt(0); }
        }

        // ------------------------------------------------------------------ menus

        GameObject Overlay(string name, float alpha)
        {
            var o = UIKit.Rect(name, _root);
            UIKit.Stretch(o);
            o.gameObject.AddComponent<Image>().color = new Color(0.03f, 0.08f, 0.14f, alpha);
            return o.gameObject;
        }

        void BuildMenu()
        {
            _menu = Overlay("menu", 0.35f);
            var title = UIKit.Text(_menu.transform, "STACK ISLAND", 120, TextAlignmentOptions.Center, Color.white, "title");
            title.fontStyle = FontStyles.Bold;
            title.outlineWidth = 0.25f;
            title.outlineColor = new Color32(20, 40, 70, 255);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(1400, 160));
            var sub = UIKit.Text(_menu.transform, "Catch it. Cook it. Stack it high.", 36, TextAlignmentOptions.Center, new Color(1f, 0.92f, 0.7f), "sub");
            sub.fontStyle = FontStyles.Italic;
            UIKit.Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(1200, 60));
            var col = UIKit.Rect("buttons", _menu.transform);
            UIKit.Place(col, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0, 60), new Vector2(420, 320));
            UIKit.VLayout(col.gameObject, 16, 0);
            _continueBtn = BigButton(col, "Continue", new Color(0.3f, 0.75f, 0.35f), () => CloseMenus());
            BigButton(col, "New Game", new Color(0.95f, 0.55f, 0.25f), () => _g.NewGame());
            BigButton(col, "Quit", new Color(0.35f, 0.38f, 0.45f), () => { _g.SaveNow(); Application.Quit(); });
            _menu.SetActive(false);
        }

        void BuildPause()
        {
            _pause = Overlay("pause", 0.5f);
            var title = UIKit.Text(_pause.transform, "Paused", 72, TextAlignmentOptions.Center, Color.white, "title");
            title.fontStyle = FontStyles.Bold;
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(800, 100));
            var col = UIKit.Rect("buttons", _pause.transform);
            UIKit.Place(col, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0, 190), new Vector2(520, 520));
            UIKit.VLayout(col.gameObject, 14, 0);
            BigButton(col, "Resume", new Color(0.3f, 0.75f, 0.35f), () => CloseMenus());
            var s = _g.Settings;
            UIKit.Slider(col, "Volume", s.volume, v => { s.volume = v; if (Sfx.I != null) Sfx.I.Volume = v; SaveSystem.SaveSettings(s); });
            _windowed = UIKit.Toggle(col, "Windowed", s.windowed, v => { s.windowed = v; _g.ApplyWindowMode(); SaveSystem.SaveSettings(s); });
            BigButton(col, "Main menu", new Color(0.95f, 0.55f, 0.25f), () => { _g.SaveNow(); ShowMainMenu(); });
            BigButton(col, "Quit", new Color(0.35f, 0.38f, 0.45f), () => { _g.SaveNow(); Application.Quit(); });
            _pause.SetActive(false);
        }

        static Button BigButton(Transform parent, string label, Color c, UnityEngine.Events.UnityAction a)
        {
            var b = UIKit.Button(parent, label, a, 420, 76, c, 34);
            return b;
        }

        public void ShowMainMenu()
        {
            _pause.SetActive(false);
            _menu.SetActive(true);
            _menu.transform.SetAsLastSibling();
            _hudLayer.SetActive(false);
            _continueBtn.gameObject.SetActive(_g.Save.totalEarned > 0 || SaveSystem.HasSave());
            UIKit.SetLabel(_continueBtn, _g.Save.totalEarned > 0 ? "Continue" : "Play");
            _continueBtn.gameObject.SetActive(true);
            _g.Cam.Orbit = true;
            Time.timeScale = 0f;
        }

        void CloseMenus()
        {
            _menu.SetActive(false);
            _pause.SetActive(false);
            _hudLayer.SetActive(true);
            _g.Cam.Orbit = false;
            _g.Cam.Yaw = 32f;
            Time.timeScale = 1f;
        }

        void Update()
        {
            if (_g == null) return;
            if (GameInput.Pause.WasPressedThisFrame() && !_menu.activeSelf)
            {
                if (_pause.activeSelf) CloseMenus();
                else
                {
                    _pause.SetActive(true);
                    _pause.transform.SetAsLastSibling();
                    _windowed.SetIsOnWithoutNotify(_g.Settings.windowed);
                    Time.timeScale = 0f;
                }
            }

            // count up/down smoothly, and pop when money comes in
            double target = _g.Cash;
            if (target > _shown + 0.5) _pulse = 1f;
            _shown = Mathf.Abs((float)(target - _shown)) < 1 ? target : _shown + (target - _shown) * (1 - Mathf.Exp(-12f * Time.unscaledDeltaTime));
            _cash.text = NumberFormatter.Format(_shown);
            _pulse = Mathf.MoveTowards(_pulse, 0, Time.unscaledDeltaTime * 4f);
            _cashBox.localScale = Vector3.one * (1f + _pulse * 0.08f);

            for (int i = _toastList.Count - 1; i >= 0; i--)
            {
                var (rt, cg, t) = _toastList[i];
                t += Time.unscaledDeltaTime;
                cg.alpha = t < 0.15f ? t / 0.15f : t > 2.2f ? Mathf.Clamp01(1 - (t - 2.2f) / 0.4f) : 1f;
                if (t > 2.6f) { Destroy(rt.gameObject); _toastList.RemoveAt(i); continue; }
                _toastList[i] = (rt, cg, t);
            }
        }
    }
}
