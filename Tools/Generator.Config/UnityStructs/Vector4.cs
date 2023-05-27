using System.Globalization;
using System.Runtime.CompilerServices;

namespace GoPlay.Generators.Config;

#if !UNITY_EDITOR 
public struct Vector4 : IEquatable<Vector4>, IFormattable
  {
    public const float kEpsilon = 1E-05f;
    /// <summary>
    ///   <para>X component of the vector.</para>
    /// </summary>
    public float x;
    /// <summary>
    ///   <para>Y component of the vector.</para>
    /// </summary>
    public float y;
    /// <summary>
    ///   <para>Z component of the vector.</para>
    /// </summary>
    public float z;
    /// <summary>
    ///   <para>W component of the vector.</para>
    /// </summary>
    public float w;
    private static readonly Vector4 zeroVector = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
    private static readonly Vector4 oneVector = new Vector4(1f, 1f, 1f, 1f);
    private static readonly Vector4 positiveInfinityVector = new Vector4(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
    private static readonly Vector4 negativeInfinityVector = new Vector4(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

    public float this[int index]
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
          case 3:
            return this.w;
          default:
            throw new IndexOutOfRangeException("Invalid Vector4 index!");
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
          case 3:
            this.w = value;
            break;
          default:
            throw new IndexOutOfRangeException("Invalid Vector4 index!");
        }
      }
    }

    /// <summary>
    ///   <para>Creates a new vector with given x, y, z, w components.</para>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="w"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public Vector4(float x, float y, float z, float w)
    {
      this.x = x;
      this.y = y;
      this.z = z;
      this.w = w;
    }

    /// <summary>
    ///   <para>Creates a new vector with given x, y, z components and sets w to zero.</para>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public Vector4(float x, float y, float z)
    {
      this.x = x;
      this.y = y;
      this.z = z;
      this.w = 0.0f;
    }

    /// <summary>
    ///   <para>Creates a new vector with given x, y components and sets z and w to zero.</para>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public Vector4(float x, float y)
    {
      this.x = x;
      this.y = y;
      this.z = 0.0f;
      this.w = 0.0f;
    }

    /// <summary>
    ///   <para>Set x, y, z and w components of an existing Vector4.</para>
    /// </summary>
    /// <param name="newX"></param>
    /// <param name="newY"></param>
    /// <param name="newZ"></param>
    /// <param name="newW"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public void Set(float newX, float newY, float newZ, float newW)
    {
      this.x = newX;
      this.y = newY;
      this.z = newZ;
      this.w = newW;
    }

    /// <summary>
    ///   <para>Linearly interpolates between two vectors.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector4 Lerp(Vector4 a, Vector4 b, float t)
    {
      t = Mathf.Clamp01(t);
      return new Vector4(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t, a.w + (b.w - a.w) * t);
    }

    /// <summary>
    ///   <para>Linearly interpolates between two vectors.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector4 LerpUnclamped(Vector4 a, Vector4 b, float t) => new Vector4(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t, a.w + (b.w - a.w) * t);

    /// <summary>
    ///   <para>Moves a point current towards target.</para>
    /// </summary>
    /// <param name="current"></param>
    /// <param name="target"></param>
    /// <param name="maxDistanceDelta"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector4 MoveTowards(
      Vector4 current,
      Vector4 target,
      float maxDistanceDelta)
    {
      float num1 = target.x - current.x;
      float num2 = target.y - current.y;
      float num3 = target.z - current.z;
      float num4 = target.w - current.w;
      float d = (float) ((double) num1 * (double) num1 + (double) num2 * (double) num2 + (double) num3 * (double) num3 + (double) num4 * (double) num4);
      if ((double) d == 0.0 || (double) maxDistanceDelta >= 0.0 && (double) d <= (double) maxDistanceDelta * (double) maxDistanceDelta)
        return target;
      float num5 = (float) Math.Sqrt((double) d);
      return new Vector4(current.x + num1 / num5 * maxDistanceDelta, current.y + num2 / num5 * maxDistanceDelta, current.z + num3 / num5 * maxDistanceDelta, current.w + num4 / num5 * maxDistanceDelta);
    }

    /// <summary>
    ///   <para>Multiplies two vectors component-wise.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector4 Scale(Vector4 a, Vector4 b) => new Vector4(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w);

    /// <summary>
    ///   <para>Multiplies every component of this vector by the same component of scale.</para>
    /// </summary>
    /// <param name="scale"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public void Scale(Vector4 scale)
    {
      this.x *= scale.x;
      this.y *= scale.y;
      this.z *= scale.z;
      this.w *= scale.w;
    }

    [MethodImpl((MethodImplOptions) 256)]
    public override int GetHashCode() => this.x.GetHashCode() ^ this.y.GetHashCode() << 2 ^ this.z.GetHashCode() >> 2 ^ this.w.GetHashCode() >> 1;

    /// <summary>
    ///   <para>Returns true if the given vector is exactly equal to this vector.</para>
    /// </summary>
    /// <param name="other"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public override bool Equals(object other) => other is Vector4 other1 && this.Equals(other1);

    [MethodImpl((MethodImplOptions) 256)]
    public bool Equals(Vector4 other) => (double) this.x == (double) other.x && (double) this.y == (double) other.y && (double) this.z == (double) other.z && (double) this.w == (double) other.w;

    /// <summary>
    ///   <para></para>
    /// </summary>
    /// <param name="a"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector4 Normalize(Vector4 a)
    {
      float num = Vector4.Magnitude(a);
      return (double) num > 9.99999974737875E-06 ? a / num : Vector4.zero;
    }

    /// <summary>
    ///   <para>Makes this vector have a magnitude of 1.</para>
    /// </summary>
    [MethodImpl((MethodImplOptions) 256)]
    public void Normalize()
    {
      float num = Vector4.Magnitude(this);
      if ((double) num > 9.99999974737875E-06)
        this = this / num;
      else
        this = Vector4.zero;
    }

    /// <summary>
    ///   <para>Returns this vector with a magnitude of 1 (Read Only).</para>
    /// </summary>
    public Vector4 normalized
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector4.Normalize(this);
    }

    /// <summary>
    ///   <para>Dot Product of two vectors.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static float Dot(Vector4 a, Vector4 b) => (float) ((double) a.x * (double) b.x + (double) a.y * (double) b.y + (double) a.z * (double) b.z + (double) a.w * (double) b.w);

    /// <summary>
    ///   <para>Projects a vector onto another vector.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector4 Project(Vector4 a, Vector4 b) => b * (Vector4.Dot(a, b) / Vector4.Dot(b, b));

    /// <summary>
    ///   <para>Returns the distance between a and b.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static float Distance(Vector4 a, Vector4 b) => Vector4.Magnitude(a - b);

    [MethodImpl((MethodImplOptions) 256)]
    public static float Magnitude(Vector4 a) => (float) Math.Sqrt((double) Vector4.Dot(a, a));

    /// <summary>
    ///   <para>Returns the length of this vector (Read Only).</para>
    /// </summary>
    public float magnitude
    {
      [MethodImpl((MethodImplOptions) 256)] get => (float) Math.Sqrt((double) Vector4.Dot(this, this));
    }

    /// <summary>
    ///   <para>Returns the squared length of this vector (Read Only).</para>
    /// </summary>
    public float sqrMagnitude
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector4.Dot(this, this);
    }

    /// <summary>
    ///   <para>Returns a vector that is made from the smallest components of two vectors.</para>
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector4 Min(Vector4 lhs, Vector4 rhs) => new Vector4(Mathf.Min(lhs.x, rhs.x), Mathf.Min(lhs.y, rhs.y), Mathf.Min(lhs.z, rhs.z), Mathf.Min(lhs.w, rhs.w));

    /// <summary>
    ///   <para>Returns a vector that is made from the largest components of two vectors.</para>
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector4 Max(Vector4 lhs, Vector4 rhs) => new Vector4(Mathf.Max(lhs.x, rhs.x), Mathf.Max(lhs.y, rhs.y), Mathf.Max(lhs.z, rhs.z), Mathf.Max(lhs.w, rhs.w));

    /// <summary>
    ///   <para>Shorthand for writing Vector4(0,0,0,0).</para>
    /// </summary>
    public static Vector4 zero
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector4.zeroVector;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector4(1,1,1,1).</para>
    /// </summary>
    public static Vector4 one
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector4.oneVector;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector4(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity).</para>
    /// </summary>
    public static Vector4 positiveInfinity
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector4.positiveInfinityVector;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector4(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity).</para>
    /// </summary>
    public static Vector4 negativeInfinity
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector4.negativeInfinityVector;
    }

    [MethodImpl((MethodImplOptions) 256)]
    public static Vector4 operator +(Vector4 a, Vector4 b) => new Vector4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);

    [MethodImpl((MethodImplOptions) 256)]
    public static Vector4 operator -(Vector4 a, Vector4 b) => new Vector4(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);

    [MethodImpl((MethodImplOptions) 256)]
    public static Vector4 operator -(Vector4 a) => new Vector4(-a.x, -a.y, -a.z, -a.w);

    [MethodImpl((MethodImplOptions) 256)]
    public static Vector4 operator *(Vector4 a, float d) => new Vector4(a.x * d, a.y * d, a.z * d, a.w * d);

    [MethodImpl((MethodImplOptions) 256)]
    public static Vector4 operator *(float d, Vector4 a) => new Vector4(a.x * d, a.y * d, a.z * d, a.w * d);

    [MethodImpl((MethodImplOptions) 256)]
    public static Vector4 operator /(Vector4 a, float d) => new Vector4(a.x / d, a.y / d, a.z / d, a.w / d);

    [MethodImpl((MethodImplOptions) 256)]
    public static bool operator ==(Vector4 lhs, Vector4 rhs)
    {
      float num1 = lhs.x - rhs.x;
      float num2 = lhs.y - rhs.y;
      float num3 = lhs.z - rhs.z;
      float num4 = lhs.w - rhs.w;
      return (double) num1 * (double) num1 + (double) num2 * (double) num2 + (double) num3 * (double) num3 + (double) num4 * (double) num4 < 9.99999943962493E-11;
    }

    [MethodImpl((MethodImplOptions) 256)]
    public static bool operator !=(Vector4 lhs, Vector4 rhs) => !(lhs == rhs);

    [MethodImpl((MethodImplOptions) 256)]
    public static implicit operator Vector4(Vector3 v) => new Vector4(v.x, v.y, v.z, 0.0f);

    [MethodImpl((MethodImplOptions) 256)]
    public static implicit operator Vector3(Vector4 v) => new Vector3(v.x, v.y, v.z);

    [MethodImpl((MethodImplOptions) 256)]
    public static implicit operator Vector4(Vector2 v) => new Vector4(v.x, v.y, 0.0f, 0.0f);

    [MethodImpl((MethodImplOptions) 256)]
    public static implicit operator Vector2(Vector4 v) => new Vector2(v.x, v.y);

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
      if (string.IsNullOrEmpty(format))
        format = "F2";
      if (formatProvider == null)
        formatProvider = (IFormatProvider) CultureInfo.InvariantCulture.NumberFormat;
      return string.Format("({0}, {1}, {2}, {3})", (object) this.x.ToString(format, formatProvider), (object) this.y.ToString(format, formatProvider), (object) this.z.ToString(format, formatProvider), (object) this.w.ToString(format, formatProvider));
    }

    [MethodImpl((MethodImplOptions) 256)]
    public static float SqrMagnitude(Vector4 a) => Vector4.Dot(a, a);

    [MethodImpl((MethodImplOptions) 256)]
    public float SqrMagnitude() => Vector4.Dot(this, this);
  }

#endif