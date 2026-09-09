using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RotMG.Game.Logic.Loots
{
    public class TierLoot : Loot
    {
        public enum LootType
        {
            Weapon,
            Ability,
            Armor,
            Ring,
            Potion
        }

        private static readonly int[] WeaponSlots = { 1, 2, 3, 8, 17, 24 };
        private static readonly int[] AbilitySlots = { 4, 5, 11, 12, 13, 15, 16, 18, 19, 20, 21, 22, 23, 25 };
        private static readonly int[] ArmorSlots = { 6, 7, 14 };
        private static readonly int[] RingSlots = { 9 };
        private static readonly int[] PotionSlots = { 10 };

        private static readonly Dictionary<string, ushort[]> Cache = new Dictionary<string, ushort[]>();

        public readonly int Tier;
        public readonly LootType Type;
        public readonly float Chance;
        public float Threshold { get; private set; }
        public readonly int Min;

        private readonly ushort[] items;

        public TierLoot(int tier, LootType type, float chance = 1, float threshold = 0, int min = 0)
        {
            Tier = tier;
            Type = type;
            Chance = chance;
            Threshold = threshold;
            Min = min;
            items = ResolveItems(tier, type);
        }

        internal override void ApplyThreshold(float threshold)
        {
            Threshold = Math.Max(Threshold, threshold);
        }

        private static ushort[] ResolveItems(int tier, LootType type)
        {
            string key = $"{type}:{tier}";
            if (Cache.TryGetValue(key, out ushort[] cached))
                return cached;

            int[] slots;
            switch (type)
            {
                case LootType.Weapon: slots = WeaponSlots; break;
                case LootType.Ability: slots = AbilitySlots; break;
                case LootType.Armor: slots = ArmorSlots; break;
                case LootType.Ring: slots = RingSlots; break;
                default: slots = PotionSlots; break;
            }

            ushort[] result = Resources.Type2Item.Values
                .Where(d => Array.IndexOf(slots, d.SlotType) != -1 && d.Tier == tier)
                .Select(d => d.Type)
                .ToArray();
            Cache[key] = result;
            return result;
        }

        public override int TryObtainItem(Entity host, Player player, int position, float threshold)
        {
            if (items.Length == 0)
                return -1;

            //Minimal guranteed drops (disregarding threshold, but loot 'positions' are sorted by damage anyway)
            if (position < Min)
                return items[MathUtils.Next(items.Length)];

            //Check if damage exceeded set threshold
            if (threshold < Threshold)
                return -1; //No item

            return MathUtils.Chance(Chance) ? items[MathUtils.Next(items.Length)] : -1;
        }
    }
}
