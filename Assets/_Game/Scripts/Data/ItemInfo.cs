using System.Collections.Generic;

namespace StackIsland
{
    /// <summary>Every item in the game. Raw items come from a yard's pen or field; products are made at stations.</summary>
    public enum ItemType
    {
        None = 0, Meat = 1, CrabCake = 2, Cash = 3, Coconut = 4, Juice = 5,
        Fish = 6, Sushi = 7, Banana = 8, Smoothie = 9, Pork = 10, Bacon = 11, Egg = 12, Pancakes = 13,
        Pineapple = 14, Pizza = 15, Strawberry = 16, IceCream = 17, Beef = 18, Burger = 19, Feast = 20,
    }

    /// <summary>Plain data about each item (no Unity types, so the balance simulator can use it outside the engine).</summary>
    public static class ItemInfo
    {
        public struct Info
        {
            public string name;
            public double price;     // what a customer pays (before upgrades); raw items aren't sold
            public float height;     // vertical space in a stack
            public string model;     // Kenney key, or "" for models drawn in code
            public float size;
            public Info(string n, double p, float h, string m = "", float s = 0.45f) { name = n; price = p; height = h; model = m; size = s; }
        }

        public static readonly Dictionary<ItemType, Info> Table = new Dictionary<ItemType, Info>
        {
            { ItemType.Meat, new Info("Crab Meat", 0, 0.11f) },
            { ItemType.CrabCake, new Info("Crab Cake", 5, 0.14f) },
            { ItemType.Cash, new Info("Cash", 0, 0.125f) },
            { ItemType.Coconut, new Info("Coconut", 0, 0.34f, "food-kit/coconut", 0.36f) },
            { ItemType.Juice, new Info("Coconut Juice", 40, 0.3f, "food-kit/coconut-half", 0.42f) },
            { ItemType.Fish, new Info("Fish", 0, 0.16f, "food-kit/fish", 0.5f) },
            { ItemType.Sushi, new Info("Sushi", 260, 0.2f, "food-kit/maki-salmon", 0.36f) },
            { ItemType.Banana, new Info("Banana", 0, 0.16f, "food-kit/banana", 0.48f) },
            { ItemType.Smoothie, new Info("Smoothie", 1600, 0.38f, "food-kit/frappe", 0.34f) },
            { ItemType.Pork, new Info("Pork", 0, 0.14f, "food-kit/meat-raw", 0.46f) },
            { ItemType.Bacon, new Info("Crispy Bacon", 10000, 0.1f, "food-kit/bacon", 0.48f) },
            { ItemType.Egg, new Info("Egg", 0, 0.24f, "food-kit/egg", 0.24f) },
            { ItemType.Pancakes, new Info("Pancakes", 62000, 0.24f, "food-kit/pancakes", 0.46f) },
            { ItemType.Pineapple, new Info("Pineapple", 0, 0.42f, "food-kit/pineapple", 0.44f) },
            { ItemType.Pizza, new Info("Pineapple Pizza", 380000, 0.12f, "food-kit/pizza", 0.52f) },
            { ItemType.Strawberry, new Info("Strawberry", 0, 0.16f, "food-kit/strawberry", 0.26f) },
            { ItemType.IceCream, new Info("Ice Cream", 2.4e6, 0.3f, "food-kit/ice-cream-cup", 0.32f) },
            { ItemType.Beef, new Info("Beef Patty", 0, 0.1f, "food-kit/meat-patty", 0.44f) },
            { ItemType.Burger, new Info("Burger", 1.5e7, 0.3f, "food-kit/burger-cheese", 0.44f) },
            { ItemType.Feast, new Info("Volcano Feast", 2.2e8, 0.36f, "food-kit/turkey", 0.52f) },
        };

        public static string Name(ItemType t) => Table.TryGetValue(t, out var i) ? i.name : "?";
        public static double Price(ItemType t) => Table.TryGetValue(t, out var i) ? i.price : 0;
        public static float Height(ItemType t) => Table.TryGetValue(t, out var i) ? i.height : 0.15f;
    }
}
