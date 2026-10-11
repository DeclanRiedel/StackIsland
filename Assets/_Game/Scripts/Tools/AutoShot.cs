using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace StackIsland
{
    /// <summary>
    /// Dev tool: run the game with "-autoshot &lt;folder&gt;" to play itself on a throwaway save and capture real
    /// screenshots, then quit (restoring fullscreen so the next normal launch isn't windowed).
    /// </summary>
    public class AutoShot : MonoBehaviour
    {
        public static bool Active { get; private set; }
        static string _dir;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-autoshot");
            if (i < 0) return;
            _dir = i + 1 < args.Length ? args[i + 1] : "Screenshots";
            Active = true;
            Directory.CreateDirectory(_dir);
            SaveSystem.SlotName = "autoshot";
            Game.TestMode = true;
            var go = new GameObject("[AutoShot]");
            DontDestroyOnLoad(go);
            go.AddComponent<AutoShot>();
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(_dir, name + ".png"));
            yield return null;
            yield return null;
        }

        IEnumerator Walk(Vector3 to, float maxSeconds = 6f)
        {
            var g = Game.I;
            float t = 0;
            while (t < maxSeconds)
            {
                var d = to - g.Player.transform.position;
                d.y = 0;
                if (d.magnitude < 0.3f) break;
                g.Player.transform.position += d.normalized * Mathf.Min(d.magnitude, 6f * Time.deltaTime);
                t += Time.deltaTime;
                yield return null;
            }
        }

        IEnumerator Start()
        {
            while (Game.I == null || Game.I.Player == null) yield return null;
            var g = Game.I;
            yield return new WaitForSecondsRealtime(1.5f);
            g.Hud.ShowMainMenu();
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("a00_menu");
            g.Hud.SendMessage("CloseMenus");
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("a01_start");

            // fight crabs in the pen and scoop the meat
            var cc = g.Player.GetComponent<CharacterController>();
            cc.enabled = false;
            yield return Walk(new Vector3(-4.5f, 0, 2.5f));
            yield return Walk(new Vector3(-6f, 0, 9f));
            for (int k = 0; k < 40; k++)
            {
                var crab = Crab.All.Where(c => c.Alive).OrderBy(c => (c.transform.position - g.Player.transform.position).sqrMagnitude).FirstOrDefault();
                if (crab != null) yield return Walk(crab.transform.position + (g.Player.transform.position - crab.transform.position).normalized * 1.2f, 1.5f);
                yield return new WaitForSeconds(0.4f);
                if (k == 12) yield return Shot("a02_fight");
                if (g.Player.Stack.Count >= 12) break;
            }
            yield return Shot("a03_stack");
            // drop at the grill, wait, pick up cakes, sell
            yield return Walk(new Vector3(-4.5f, 0, 5.5f));
            yield return Walk(new Vector3(-4.5f, 0, 2.5f));
            yield return Walk(new Vector3(3.1f, 0, 7.8f));
            yield return new WaitForSeconds(2.5f);
            yield return Walk(new Vector3(8.9f, 0, 7.8f));
            yield return new WaitForSeconds(6f);
            yield return Shot("a04_grill");
            yield return Walk(new Vector3(4.5f, 0, -7.7f));
            yield return new WaitForSeconds(5f);
            yield return Shot("a05_sell");
            yield return Walk(new Vector3(8.8f, 0, -8.1f));
            yield return new WaitForSeconds(2f);
            yield return Shot("a06_cash");

            // buy a few things, let the helpers run
            g.Grant(3000);
            foreach (var id in new[] { "crabs", "backpack", "catcher", "catcher", "waiter", "hotgrill", "grill2" })
            {
                var pad = BuyPad.All.FirstOrDefault(p => p.Id == id && p.Shown);
                if (pad != null) pad.ForceBuy();
                yield return null;
            }
            yield return Walk(new Vector3(-1f, 0, -2f));
            yield return Shot("a07_pads");
            yield return new WaitForSeconds(20f);
            yield return Walk(new Vector3(2f, 0, 2f));
            yield return Shot("a08_busy");
            g.Cam.Distance = 34;
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("a09_overview");

            var native = Screen.currentResolution;
            Screen.SetResolution(native.width, native.height, FullScreenMode.FullScreenWindow);
            yield return null;
            yield return null;
            Application.Quit();
        }
    }
}
