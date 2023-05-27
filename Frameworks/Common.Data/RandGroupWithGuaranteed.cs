using System;

namespace GoPlay.Common.Data
{
    public struct RandGroupWithGuaranteed
    {
        /// <summary>
        /// 抽取组ID
        /// </summary>
        public int Id;

        /// <summary>
        /// 权重
        /// </summary>
        public float Weight;

        /// <summary>
        /// 保底次数（多少次未抽中，则抽它）
        /// </summary>
        public int Guaranteed;

        public static bool operator ==(RandGroupWithGuaranteed lhs, RandGroupWithGuaranteed rhs)
        {
            if (lhs.Id != rhs.Id) return false;
            if (Math.Abs(lhs.Weight - rhs.Weight) > 0.00001f) return false;
            if (lhs.Guaranteed != rhs.Guaranteed) return false;

            return true;
        }

        public static bool operator !=(RandGroupWithGuaranteed lhs, RandGroupWithGuaranteed rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(RandGroupWithGuaranteed other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return obj is RandGroupWithGuaranteed other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Weight, Guaranteed);
        }
    }
}