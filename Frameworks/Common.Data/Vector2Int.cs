using System;

namespace GoPlay.Common.Data
{
    public struct Vector2Int
    {
        public int x;

        public int y;

        public Vector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static bool operator ==(Vector2Int lhs, Vector2Int rhs)
        {
            if (lhs.x != rhs.x) return false;
            if (lhs.y != rhs.y) return false;

            return true;
        }

        public static bool operator !=(Vector2Int lhs, Vector2Int rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(Vector2Int other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return obj is Vector2Int other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, y);
        }
    }
}