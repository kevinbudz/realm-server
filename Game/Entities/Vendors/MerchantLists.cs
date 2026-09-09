using RotMG.Common;
using System.Collections.Generic;

namespace RotMG.Game.Entities.Vendors
{
    //Shop stock lists, ported from realm-src-master
    //wServer/realm/entities/vendors/MerchantLists.cs.
    public class ShopItem
    {
        public ushort ItemId = ushort.MaxValue;
        public int Price;
        public int Count = -1;
        public string Name;

        public ShopItem(string name, int price, int count = -1)
        {
            Name = name;
            Price = price;
            Count = count;
        }
    }

    public static class MerchantLists
    {
        private static readonly List<ShopItem> Weapons = new List<ShopItem>
        {
            new ShopItem("Dagger of Foul Malevolence", 500),
            new ShopItem("Bow of Covert Havens", 500),
            new ShopItem("Staff of the Cosmic Whole", 500),
            new ShopItem("Wand of Recompense", 500),
            new ShopItem("Sword of Acclaim", 500),
            new ShopItem("Masamune", 500)
        };

        private static readonly List<ShopItem> Abilities = new List<ShopItem>
        {
            new ShopItem("Cloak of Ghostly Concealment", 500),
            new ShopItem("Quiver of Elvish Mastery", 500),
            new ShopItem("Elemental Detonation Spell", 500),
            new ShopItem("Tome of Holy Guidance", 500),
            new ShopItem("Helm of the Great General", 500),
            new ShopItem("Colossus Shield", 500),
            new ShopItem("Seal of the Blessed Champion", 500),
            new ShopItem("Baneserpent Poison", 500),
            new ShopItem("Bloodsucker Skull", 500),
            new ShopItem("Giantcatcher Trap", 500),
            new ShopItem("Planefetter Orb", 500),
            new ShopItem("Prism of Apparitions", 500),
            new ShopItem("Scepter of Storms", 500),
            new ShopItem("Doom Circle", 500)
        };

        private static readonly List<ShopItem> Armor = new List<ShopItem>
        {
            new ShopItem("Robe of the Illusionist", 50),
            new ShopItem("Robe of the Grand Sorcerer", 500),
            new ShopItem("Studded Leather Armor", 50),
            new ShopItem("Hydra Skin Armor", 500),
            new ShopItem("Mithril Armor", 50),
            new ShopItem("Acropolis Armor", 500)
        };

        private static readonly List<ShopItem> Rings = new List<ShopItem>
        {
            new ShopItem("Ring of Paramount Attack", 100),
            new ShopItem("Ring of Paramount Defense", 100),
            new ShopItem("Ring of Paramount Speed", 100),
            new ShopItem("Ring of Paramount Dexterity", 100),
            new ShopItem("Ring of Paramount Vitality", 100),
            new ShopItem("Ring of Paramount Wisdom", 100),
            new ShopItem("Ring of Paramount Health", 100),
            new ShopItem("Ring of Paramount Magic", 100),
            new ShopItem("Ring of Unbound Attack", 750),
            new ShopItem("Ring of Unbound Defense", 750),
            new ShopItem("Ring of Unbound Speed", 750),
            new ShopItem("Ring of Unbound Dexterity", 750),
            new ShopItem("Ring of Unbound Vitality", 750),
            new ShopItem("Ring of Unbound Wisdom", 750),
            new ShopItem("Ring of Unbound Health", 750),
            new ShopItem("Ring of Unbound Magic", 750)
        };

        private static readonly List<ShopItem> Store1 = new List<ShopItem>
        {
            new ShopItem("Pirate Cave Key", 25),
            new ShopItem("Spider Den Key", 25),
            new ShopItem("Undead Lair Key", 50),
            new ShopItem("Sprite World Key", 50),
            new ShopItem("Abyss of Demons Key", 50),
            new ShopItem("Snake Pit Key", 50),
            new ShopItem("Beachzone Key", 50),
            new ShopItem("Lab Key", 50),
            new ShopItem("Totem Key", 50),
            new ShopItem("Manor Key", 80),
            new ShopItem("Candy Key", 100),
            new ShopItem("Cemetery Key", 150),
            new ShopItem("Davy's Key", 200),
            new ShopItem("Ocean Trench Key", 300),
            new ShopItem("Tomb of the Ancients Key", 400)
        };

        private static readonly List<ShopItem> Store2 = new List<ShopItem>
        {
            new ShopItem("Amulet of Resurrection", 11250),
            new ShopItem("Backpack", 2000),
            new ShopItem("Elixir of Health 7", 500),
            new ShopItem("Elixir of Magic 7", 500),
            new ShopItem("Transformation Potion", 500)
        };

        private static readonly List<ShopItem> Store4 = new List<ShopItem>
        {
            new ShopItem("Tincture of Fear", 100),
            new ShopItem("Tincture of Courage", 150),
            new ShopItem("Tincture of Dexterity", 100),
            new ShopItem("Tincture of Defense", 100),
            new ShopItem("Tincture of Life", 150),
            new ShopItem("Tincture of Mana", 150),
            new ShopItem("Effusion of Dexterity", 250),
            new ShopItem("Effusion of Life", 250),
            new ShopItem("Effusion of Mana", 250),
            new ShopItem("Effusion of Defense", 250)
        };

        public static readonly Dictionary<Region, List<ShopItem>> Shops =
            new Dictionary<Region, List<ShopItem>>
            {
                { Region.Store_1, Store1 },
                { Region.Store_2, Store2 },
                { Region.Store_4, Store4 }
            };

        public static readonly Dictionary<Region, CurrencyType> ShopCurrency =
            new Dictionary<Region, CurrencyType>
            {
                { Region.Store_1, CurrencyType.Gold },
                { Region.Store_2, CurrencyType.Fame },
                { Region.Store_4, CurrencyType.Fame }
            };

        private static bool _resolved;

        public static void ResolveItems()
        {
            if (_resolved)
                return;
            _resolved = true;
            foreach (List<ShopItem> shop in Shops.Values)
                foreach (ShopItem shopItem in shop)
                {
                    ItemDesc desc;
                    if (Resources.Id2Item.TryGetValue(shopItem.Name, out desc))
                        shopItem.ItemId = desc.Type;
                }
        }
    }
}
