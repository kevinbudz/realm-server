using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Game.Logic.Loots
{
    public class ItemLoot : Loot
    {
        public readonly ushort Item;
        public float Threshold { get; private set; }
        public readonly float Chance;
        public readonly int Min;

        public ItemLoot(string item, float chance, float threshold = 0, int min = 0)
        {
            Item = Resources.IdLower2Item[item.ToLower()].Type;
            Threshold = threshold;
            Chance = chance;
            Min = min;
        }

        internal override void ApplyThreshold(float threshold)
        {
            Threshold = Math.Max(Threshold, threshold);
        }

        public override int TryObtainItem(Entity host, Player player, int position, float threshold)
        {
            //Minimal guranteed drops (disregarding threshold, but loot 'positions' are sorted by damage anyway)
            if (position < Min)
                return Item;

            //Check if damage exceeded set threshold
            if (threshold < Threshold)
                return -1; //No item

            return MathUtils.Chance(Chance) ? Item : -1;
        }
    }
}
