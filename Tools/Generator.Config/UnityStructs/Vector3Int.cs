using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace GoPlay.Generators.Config;
#if !UNITY_EDITOR 
public struct Vector3Int : IEquatable<Vector3Int>, IFormattable
  {
    private int m_X;
    private int m_Y;
    private int m_Z;
    private static readonly Vector3Int s_Zero = new Vector3Int(0, 0, 0);
    private static readonly Vector3Int s_One = new Vector3Int(1, 1, 1);
    private static readonly Vector3Int s_Up = new Vector3Int(0, 1, 0);
    private static readonly Vector3Int s_Down = new Vector3Int(0, -1, 0);
    private static readonly Vector3Int s_Left = new Vector3Int(-1, 0, 0);
    private static readonly Vector3Int s_Right = new Vector3Int(1, 0, 0);
    private static readonly Vector3Int s_Forward = new Vector3Int(0, 0, 1);
    private static readonly Vector3Int s_Back = new Vector3Int(0, 0, -1);

    /// <summary>
    ///   <para>X component of the vector.</para>
    /// </summary>
    public int x
    {
      [MethodImpl((MethodImplOptions) 256)] get => this.m_X;
      [MethodImpl((MethodImplOptions) 256)] set => this.m_X = value;
    }

    /// <summary>
    ///   <para>Y component of the vector.</para>
    /// </summary>
    public int y
    {
      [MethodImpl((MethodImplOptions) 256)] get => this.m_Y;
      [MethodImpl((MethodImplOptions) 256)] set => this.m_Y = value;
    }

    /// <summary>
    ///   <para>Z component of the vector.</para>
    /// </summary>
    public int z
    {
      [MethodImpl((MethodImplOptions) 256)] get => this.m_Z;
      [MethodImpl((MethodImplOptions) 256)] set => this.m_Z = value;
    }

    /// <summary>
    ///   <para>Initializes and returns an instance of a new Vector3Int with x and y components and sets z to zero.</para>
    /// </summary>
    /// <param name="x">The X component of the Vector3Int.</param>
    /// <param name="y">The Y component of the Vector3Int.</param>
    [MethodImpl((MethodImplOptions) 256)]
    public Vector3Int(int x, int y)
    {
      this.m_X = x;
      this.m_Y = y;
      this.m_Z = 0;
    }

    /// <summary>
    ///   <para>Initializes and returns an instance of a new Vector3Int with x, y, z components.</para>
    /// </summary>
    /// <param name="x">The X component of the Vector3Int.</param>
    /// <param name="y">The Y component of the Vector3Int.</param>
    /// <param name="z">The Z component of the Vector3Int.</param>
    [MethodImpl((MethodImplOptions) 256)]
    public Vector3Int(int x, int y, int z)
    {
      this.m_X = x;
      this.m_Y = y;
      this.m_Z = z;
    }

    /// <summary>
    ///   <para>Set x, y and z components of an existing Vector3Int.</para>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public void Set(int x, int y, int z)
    {
      this.m_X = x;
      this.m_Y = y;
      this.m_Z = z;
    }

    public int this[int index]
    {
      [MethodImpl((MethodImplOptions) 256)] get
      {
        switch (index)
        {
          case 0:
            return this.x;
          case 1:
            return this.y;
          case 2:
            return this.z;
          default:
            throw new IndexOutOfRangeException(string.Format("Invalid Vector3Int index addressed: {0}!", (object) index));
        }
      }
      [MethodImpl((MethodImplOptions) 256)] set
      {
        switch (index)
        {
          case 0:
            this.x = value;
            break;
          case 1:
            this.y = value;
            break;
          case 2:
            this.z = value;
            break;
          default:
            throw new IndexOutOfRangeException(string.Format("Invalid Vector3Int index addressed: {0}!", (object) index));
        }
      }
    }

    /// <summary>
    ///   <para>Returns the length of this vector (Read Only).</para>
    /// </summary>
    public float magnitude
    {
      [MethodImpl((MethodImplOptions) 256)] get => Mathf.Sqrt((float) (this.x * this.x + this.y * this.y + this.z * this.z));
    }

    /// <summary>
    ///   <para>Returns the squared length of this vector (Read Only).</para>
    /// </summary>
    public int sqrMagnitude
    {
      [MethodImpl((MethodImplOptions) 256)] get => this.x * this.x + this.y * this.y + this.z * this.z;
    }

    /// <summary>
    ///   <para>Returns the distance between a and b.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static float Distance(Vector3Int a, Vector3Int b) => (a - b).magnitude;

    /// <summary>
    ///   <para>Returns a vector that is made from the smallest components of two vectors.</para>
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3Int Min(Vector3Int lhs, Vector3Int rhs) => new Vector3Int(Mathf.Min(lhs.x, rhs.x), Mathf.Min(lhs.y, rhs.y), Mathf.Min(lhs.z, rhs.z));

    /// <summary>
    ///   <para>Returns a vector that is made from the largest components of two vectors.</para>
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3Int Max(Vector3Int lhs, Vector3Int rhs) => new Vector3Int(Mathf.Max(lhs.x, rhs.x), Mathf.Max(lhs.y, rhs.y), Mathf.Max(lhs.z, rhs.z));

    /// <summary>
    ///   <para>Multiplies two vectors component-wise.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3Int Scale(Vector3Int a, Vector3Int b) => new Vector3Int(a.x * b.x, a.y * b.y, a.z * b.z);

    /// <summary>
    ///   <para>Multiplies every component of this vector by the same component of scale.</para>
    /// </summary>
    /// <param name="scale"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public void Scale(Vector3Int scale)
    {
      this.x *= scale.x;
      this.y *= scale.y;
      this.z *= scale.z;
    }

    /// <summary>
    ///   <para>Clamps the Vector3Int to the bounds given by min and max.</para>
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public void Clamp(Vector3Int min, Vector3Int max)
    {
      this.x = Math.Max(min.x, this.x);
      this.x = Math.Min(max.x, this.x);
      this.y = Math.Max(min.y, this.y);
      this.y = Math.Min(max.y, this.y);
      this.z = Math.Max(min.z, this.z);
      this.z = Math.Min(max.z, this.z);
    }

    [MethodImpl((MethodImplOptions) 256)]
    public static implicit operator Vector3(Vector3Int v) => new Vector3((float) v.x, (float) v.y, (float) v.z);

    [MethodImpl((MethodImplOptions) 256)]
    public static explicit operator Vector2Int(Vector3Int v) => new Vector2Int(v.x, v.y);

    /// <summary>
    ///   <para>Converts a  Vector3 to a Vector3Int by doing a Floor to each value.</para>
    /// </summary>
    /// <param name="v"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3Int FloorToInt(Vector3 v) => new Vector3Int(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y), Mathf.FloorToInt(v.z));

    /// <summary>
    ///   <para>Converts a  Vector3 to a Vector3Int by doing a Ceiling to each value.</para>
    /// </summary>
    /// <param name="v"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3Int CeilToInt(Vector3 v) => new Vector3Int(Mathf.CeilToInt(v.x), Mathf.CeilToInt(v.y), Mathf.CeilToInt(v.z));

    /// <summary>
    ///   <para>Converts a  Vector3 to a Vector3Int by doing a Round to each value.</para>
    /// </summary>
    /// <param name="v"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3Int RoundToInt(Vector3 v) => new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));

    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3Int operator +(Vector3Int a, Vector3Int b) => new Vector3Int(a.x + b.x, a.y + b.y, a.z + b.z);

    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3Int operator -(Vector3Int a, Vector3Int b) => new Vector3Int(a.x - b.x, a.y - b.y, a.z - b.z);

    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3Int operator *(Vector3Int a, Vector3Int b) => new Vector3Int(a.x * b.x, a.y * b.y, a.z * b.z);

    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3Int operator -(Vector3Int a) => new Vector3Int(-a.x, -a.y, -a.z);

    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3Int operator *(Vector3Int a, int b) => new Vector3Int(a.x * b, a.y * b, a.z * b);

    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3Int operator *(int a, Vector3Int b) => new Vector3Int(a * b.x, a * b.y, a * b.z);

    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3Int operator /(Vector3Int a, int b) => new Vector3Int(a.x / b, a.y / b, a.z / b);

    [MethodImpl((MethodImplOptions) 256)]
    public static bool operator ==(Vector3Int lhs, Vector3Int rhs) => lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z;

    [MethodImpl((MethodImplOptions) 256)]
    public static bool operator !=(Vector3Int lhs, Vector3Int rhs) => !(lhs == rhs);

    /// <summary>
    ///   <para>Returns true if the objects are equal.</para>
    /// </summary>
    /// <param name="other"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public override bool Equals(object other) => other is Vector3Int other1 && this.Equals(other1);

    [MethodImpl((MethodImplOptions) 256)]
    public bool Equals(Vector3Int other) => this == other;

    /// <summary>
    ///   <para>Gets the hash code for the Vector3Int.</para>
    /// </summary>
    /// <returns>
    ///   <para>The hash code of the Vector3Int.</para>
    /// </returns>
    [MethodImpl((MethodImplOptions) 256)]
    public override int GetHashCode()
    {
      int hashCode1 = this.y.GetHashCode();
      int hashCode2 = this.z.GetHashCode();
      return this.x.GetHashCode() ^ hashCode1 << 4 ^ hashCode1 >> 28 ^ hashCode2 >> 4 ^ hashCode2 << 28;
    }

    /// <summary>
    ///   <para>Returns a formatted string for this vector.</para>
    /// </summary>
    /// <param name="format">A numeric format string.</param>
    /// <param name="formatProvider">An object that specifies culture-specific formatting.</param>
    public override string ToString() => this.ToString((string) null, (IFormatProvider) null);

    /// <summary>
    ///   <para>Returns a formatted string for this vector.</para>
    /// </summary>
    /// <param name="format">A numeric format string.</param>
    /// <param name="formatProvider">An object that specifies culture-specific formatting.</param>
    public string ToString(string format) => this.ToString(format, (IFormatProvider) null);

    /// <summary>
    ///   <para>Returns a formatted string for this vector.</para>
    /// </summary>
    /// <param name="format">A numeric format string.</param>
    /// <param name="formatProvider">An object that specifies culture-specific formatting.</param>
    public string ToString(string format, IFormatProvider formatProvider)
    {
      if (formatProvider == null)
        formatProvider = (IFormatProvider) CultureInfo.InvariantCulture.NumberFormat;
      return string.Format("({0}, {1}, {2})", (object) this.x.ToString(format, formatProvider), (object) this.y.ToString(format, formatProvider), (object) this.z.ToString(format, formatProvider));
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector3Int(0, 0, 0).</para>
    /// </summary>
    public static Vector3Int zero
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector3Int.s_Zero;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector3Int(1, 1, 1).</para>
    /// </summary>
    public static Vector3Int one
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector3Int.s_One;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector3Int(0, 1, 0).</para>
    /// </summary>
    public static Vector3Int up
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector3Int.s_Up;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector3Int(0, -1, 0).</para>
    /// </summary>
    public static Vector3Int down
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector3Int.s_Down;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector3Int(-1, 0, 0).</para>
    /// </summary>
    public static Vector3Int left
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector3Int.s_Left;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector3Int(1, 0, 0).</para>
    /// </summary>
    public static Vector3Int right
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector3Int.s_Right;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector3Int(0, 0, 1).</para>
    /// </summary>
    public static Vector3Int forward
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector3Int.s_Forward;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector3Int(0, 0, -1).</para>
    /// </summary>
    public static Vector3Int back
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector3Int.s_Back;
    }
  }
  #endif