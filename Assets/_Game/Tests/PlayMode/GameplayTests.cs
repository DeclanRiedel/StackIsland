using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StackIsland.Tests
{
    /// <summary>Runs the real game scene on a throwaway save and checks the core loop end to end.</summary>
    public class GameplayTests
    {
        Game G => Game.I;

        [UnitySetUp]
        public IEnumerator Load()
        {
            Game.TestMode = true;
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;
            Assert.NotNull(Game.I, "game booted");
        }

        [UnityTest]
        public IEnumerator CrabDropsMeatThatCanBeCarried()
        {
            var crab = Crab.All.First(c => c.Alive || c.Hp > 0);
            yield return new WaitForSeconds(0.5f);   // spawn pop-in
            for (int i = 0; i < 5 && crab.Hp > 0; i++) crab.Hit(1, crab.transform.position + Vector3.right);
            Assert.AreEqual(0, crab.Hp);
            yield return new WaitForSeconds(0.6f);
            int meat = LooseItem.All.Count(l => l.Type == ItemType.Meat);
            Assert.GreaterOrEqual(meat, 1, "meat dropped");
            G.Player.Teleport(LooseItem.All[0].transform.position);
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(G.Player.Stack.CountOf(ItemType.Meat), 0, "player scooped meat");
        }

        [UnityTest]
        public IEnumerator GrillCooksAndCustomersPay()
        {
            var grill = G.Grills[0];
            for (int i = 0; i < 4; i++) grill.Input.Add();
            yield return new WaitForSeconds(grill.CookSeconds * 4 + 0.6f);
            Assert.AreEqual(4, grill.Output.Count, "4 cakes cooked");
            for (int i = 0; i < 12; i++) G.Counter.Shelf.Add();
            // wait for a customer to walk up, take and pay
            float t = 0;
            while (G.Counter.Cash.Count == 0 && t < 30f) { t += Time.deltaTime; yield return null; }
            Assert.Greater(G.Counter.Cash.Count, 0, "cash on the tray");
            Assert.Greater(G.Counter.Cash.CashValue, 0);
            double before = G.Cash;
            G.Player.Teleport(G.Counter.Cash.transform.position);
            yield return new WaitForSeconds(1f);
            Assert.Greater(G.Cash, before, "collected cash");
        }

        [UnityTest]
        public IEnumerator StandingStillOnPadBuysIt()
        {
            var pad = BuyPad.All.First(p => p.Id == "crabs");
            G.Grant(pad.Cost + 5);
            G.Player.Teleport(pad.transform.position);
            float t = 0;
            while (pad.Level == 0 && t < 6f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(1, pad.Level, "bought");
            Assert.AreEqual(6, Crab.All.Count, "two more crabs");
        }
    }
}
