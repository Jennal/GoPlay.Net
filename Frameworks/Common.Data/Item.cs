using System;

namespace GoPlay.Common.Data
{
    public struct Item
    {
        public int Id;
        public int Count;
        
        public static bool operator ==(Item lhs, Item rhs)
        {
            if (lhs.Id != rhs.Id) return false;
            if (lhs.Count != rhs.Count) return false;

            return true;
        }
        
        public static bool operator !=(Item lhs, Item rhs)
        {
            return !(lhs == rhs);
        }
        
        public bool Equals(Item other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return obj is Item other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Count);
        }
    }
}
