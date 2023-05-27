using System;

namespace GoPlay.Common.Data
{
    public struct Vector3Int
    {
        public int x;

        public int y;

        public int z;

        public Vector3Int(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static bool operator ==(Vector3Int lhs, Vector3Int rhs)
        {
            if (lhs.x != rhs.x) return false;
            if (lhs.y != rhs.y) return false;
            if (lhs.z != rhs.z) return false;

            return true;
        }

        public static bool operator !=(Vector3Int lhs, Vector3Int rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(Vector3Int other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return obj is Vector3Int other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, y, z);
        }
    }
}