using System;

namespace GoPlay.Common.Data
{
    public struct DynamicWeightRandItem
    {
        /// <summary>
        /// ID
        /// </summary>
        public int Id;

        /// <summary>
        /// 初始权重
        /// </summary>
        public float WeightDefault;

        /// <summary>
        /// 每次战斗提升权重
        /// </summary>
        public float WeightInc;

        /// <summary>
        /// 权重最大值
        /// </summary>
        public float WeightMax;

        public static bool operator ==(DynamicWeightRandItem lhs, DynamicWeightRandItem rhs)
        {
            if (lhs.Id != rhs.Id) return false;
            if (Math.Abs(lhs.WeightDefault - rhs.WeightDefault) > 0.00001f) return false;
            if (Math.Abs(lhs.WeightInc - rhs.WeightInc) > 0.00001f) return false;
            if (Math.Abs(lhs.WeightMax - rhs.WeightMax) > 0.00001f) return false;

            return true;
        }

        public static bool operator !=(DynamicWeightRandItem lhs, DynamicWeightRandItem rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(DynamicWeightRandItem other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return obj is DynamicWeightRandItem other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, WeightDefault, WeightInc, WeightMax);
        }
    }
}