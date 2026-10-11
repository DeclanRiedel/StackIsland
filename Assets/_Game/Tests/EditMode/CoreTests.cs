using NUnit.Framework;
using UnityEngine;

namespace StackIsland.Tests
{
    public class CoreTests
    {
        [Test]
        public void NumbersFormatShort()
        {
            Assert.AreEqual("0", NumberFormatter.Format(0));
            Assert.AreEqual("999", NumberFormatter.Format(999.9));
            Assert.AreEqual("1.5K", NumberFormatter.Format(1500));
            Assert.AreEqual("2.25M", NumberFormatter.Format(2_250_000));
        }

        [Test]
        public void SaveRoundTrips()
        {
            var s = new SaveData { cash = 123.5, totalEarned = 400 };
            s.Pad("crabs").level = 2;
            s.Pile("grill1:in").count = 7;
            var back = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(s));
            Assert.AreEqual(123.5, back.cash);
            Assert.AreEqual(2, back.Pad("crabs").level);
            Assert.AreEqual(7, back.Pile("grill1:in").count);
        }

        [Test]
        public void YardsAreConsistent()
        {
            var all = YardDefs.All;
            Assert.GreaterOrEqual(all.Count, 3);
            for (int i = 0; i < all.Count; i++)
            {
                var y = all[i];
                Assert.AreEqual(i + 1, y.Index, "yards are numbered 1..n in order");
                Assert.Greater(Items.Price(y.Product), 0, y.Name + " product has a price");
                Assert.AreEqual(0, Items.Price(y.Raw), y.Name + " raw item is not sold directly");
                if (y.Source == SourceKind.Pen) Assert.IsFalse(string.IsNullOrEmpty(y.AnimalModel));
                if (i == 0) continue;
                var prev = all[i - 1];
                Assert.Greater(y.GateCost, prev.GateCost, "gates get more expensive");
                Assert.Greater(Items.Price(y.Product), Items.Price(prev.Product), "later products are worth more");
                // the combo finale is limited by couriers, not by its price, so it's priced on its own
                if (y.Source != SourceKind.None) Assert.Greater(y.Scale, prev.Scale, "later yards cost more");
            }
        }

        [Test]
        public void BalanceSimFinishesIn80To100Hours()
        {
            BalanceModel.Run();
            Assert.That(BalanceModel.LastTotalHours, Is.InRange(80.0, 100.0), "full game length (thoughtful player)");
        }

        [Test]
        public void ItemsHavePricesAndHeights()
        {
            Assert.Greater(Items.Price(ItemType.CrabCake), 0);
            Assert.Greater(Items.Height(ItemType.Meat), 0f);
        }
    }
}
