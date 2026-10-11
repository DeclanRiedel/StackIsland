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
        public void RoutingDetoursThroughPenGate()
        {
            var pts = new System.Collections.Generic.List<Vector3>();
            Yard.Route(new Vector3(5, 0, -5), new Vector3(-6, 0, 10), pts);
            Assert.AreEqual(3, pts.Count, "outside -> gate outside -> gate inside -> target");
            Yard.Route(new Vector3(5, 0, -5), new Vector3(8, 0, 8), pts);
            Assert.AreEqual(1, pts.Count);
        }

        [Test]
        public void ItemsHavePricesAndHeights()
        {
            Assert.Greater(Items.Price(ItemType.CrabCake), 0);
            Assert.Greater(Items.Height(ItemType.Meat), 0f);
        }
    }
}
