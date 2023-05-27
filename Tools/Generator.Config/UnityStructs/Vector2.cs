using System.Globalization;

namespace GoPlay.Generators.Config;

#if !UNITY_EDITOR 
public struct Vector2 : IEquatable<Vector2>, IFormattable
  {
    /// <summary>
    ///   <para>X component of the vector.</para>
    /// </summary>
    public float x;
    /// <summary>
    ///   <para>Y component of the vector.</para>
    /// </summary>
    public float y;
    private static readonly Vector2 zeroVector = new Vector2(0.0f, 0.0f);
    private static readonly Vector2 oneVector = new Vector2(1f, 1f);
    private static readonly Vector2 upVector = new Vector2(0.0f, 1f);
    private static readonly Vector2 downVector = new Vector2(0.0f, -1f);
    private static readonly Vector2 leftVector = new Vector2(-1f, 0.0f);
    private static readonly Vector2 rightVector = new Vector2(1f, 0.0f);
    private static readonly Vector2 positiveInfinityVector = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
    private static readonly Vector2 negativeInfinityVector = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
    public const float kEpsilon = 1E-05f;
    public const float kEpsilonNormalSqrt = 1E-15f;

    public float this[int index]
    {
      get
      {
        switch (index)
        {
          case 0:
            return this.x;
          case 1:
            return this.y;
          default:
            throw new IndexOutOfRangeException("Invalid Vector2 index!");
        }
      }
      set
      {
        switch (index)
        {
          case 0:
            this.x = value;
            break;
          case 1:
            this.y = value;
            break;
          default:
            throw new IndexOutOfRangeException("Invalid Vector2 index!");
        }
      }
    }

    /// <summary>
    ///   <para>Constructs a new vector with given x, y components.</para>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    
    public Vector2(float x, float y)
    {
      this.x = x;
      this.y = y;
    }

    /// <summary>
    ///   <para>Set x and y components of an existing Vector2.</para>
    /// </summary>
    /// <param name="newX"></param>
    /// <param name="newY"></param>
    
    public void Set(float newX, float newY)
    {
      this.x = newX;
      this.y = newY;
    }

    /// <summary>
    ///   <para>Linearly interpolates between vectors a and b by t.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    
    public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
    {
      t = Mathf.Clamp01(t);
      return new Vector2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
    }

    /// <summary>
    ///   <para>Linearly interpolates between vectors a and b by t.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    
    public static Vector2 LerpUnclamped(Vector2 a, Vector2 b, float t) => new Vector2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);

    /// <summary>
    ///   <para>Moves a point current towards target.</para>
    /// </summary>
    /// <param name="current"></param>
    /// <param name="target"></param>
    /// <param name="maxDistanceDelta"></param>
    
    public static Vector2 MoveTowards(
      Vector2 current,
      Vector2 target,
      float maxDistanceDelta)
    {
      float num1 = target.x - current.x;
      float num2 = target.y - current.y;
      float d = (float) ((double) num1 * (double) num1 + (double) num2 * (double) num2);
      if ((double) d == 0.0 || (double) maxDistanceDelta >= 0.0 && (double) d <= (double) maxDistanceDelta * (double) maxDistanceDelta)
        return target;
      float num3 = (float) Math.Sqrt((double) d);
      return new Vector2(current.x + num1 / num3 * maxDistanceDelta, current.y + num2 / num3 * maxDistanceDelta);
    }

    /// <summary>
    ///   <para>Multiplies two vectors component-wise.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    
    public static Vector2 Scale(Vector2 a, Vector2 b) => new Vector2(a.x * b.x, a.y * b.y);

    /// <summary>
    ///   <para>Multiplies every component of this vector by the same component of scale.</para>
    /// </summary>
    /// <param name="scale"></param>
    
    public void Scale(Vector2 scale)
    {
      this.x *= scale.x;
      this.y *= scale.y;
    }

    /// <summary>
    ///   <para>Makes this vector have a magnitude of 1.</para>
    /// </summary>
    
    public void Normalize()
    {
      float magnitude = this.magnitude;
      if ((double) magnitude > 9.99999974737875E-06)
        this = this / magnitude;
      else
        this = Vector2.zero;
    }

    /// <summary>
    ///   <para>Returns this vector with a magnitude of 1 (Read Only).</para>
    /// </summary>
    public Vector2 normalized
    {
      get
      {
        Vector2 normalized = new Vector2(this.x, this.y);
        normalized.Normalize();
        return normalized;
      }
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
      if (string.IsNullOrEmpty(format))
        format = "F2";
      if (formatProvider == null)
        formatProvider = (IFormatProvider) CultureInfo.InvariantCulture.NumberFormat;
      return string.Format("({0}, {1})", (object) this.x.ToString(format, formatProvider), (object) this.y.ToString(format, formatProvider));
    }

    
    public override int GetHashCode() => this.x.GetHashCode() ^ this.y.GetHashCode() << 2;

    /// <summary>
    ///   <para>Returns true if the given vector is exactly equal to this vector.</para>
    /// </summary>
    /// <param name="other"></param>
    
    public override bool Equals(object other) => other is Vector2 other1 && this.Equals(other1);

    
    public bool Equals(Vector2 other) => (double) this.x == (double) other.x && (double) this.y == (double) other.y;

    /// <summary>
    ///   <para>Reflects a vector off the vector defined by a normal.</para>
    /// </summary>
    /// <param name="inDirection"></param>
    /// <param name="inNormal"></param>
    
    public static Vector2 Reflect(Vector2 inDirection, Vector2 inNormal)
    {
      float num = -2f * Vector2.Dot(inNormal, inDirection);
      return new Vector2(num * inNormal.x + inDirection.x, num * inNormal.y + inDirection.y);
    }

    /// <summary>
    ///   <para>Returns the 2D vector perpendicular to this 2D vector. The result is always rotated 90-degrees in a counter-clockwise direction for a 2D coordinate system where the positive Y axis goes up.</para>
    /// </summary>
    /// <param name="inDirection">The input direction.</param>
    /// <returns>
    ///   <para>The perpendicular direction.</para>
    /// </returns>
    
    public static Vector2 Perpendicular(Vector2 inDirection) => new Vector2(-inDirection.y, inDirection.x);

    /// <summary>
    ///   <para>Dot Product of two vectors.</para>
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    
    public static float Dot(Vector2 lhs, Vector2 rhs) => (float) ((double) lhs.x * (double) rhs.x + (double) lhs.y * (double) rhs.y);

    /// <summary>
    ///   <para>Returns the length of this vector (Read Only).</para>
    /// </summary>
    public float magnitude
    {
      get => (float) Math.Sqrt((double) this.x * (double) this.x + (double) this.y * (double) this.y);
    }

    /// <summary>
    ///   <para>Returns the squared length of this vector (Read Only).</para>
    /// </summary>
    public float sqrMagnitude
    {
      get => (float) ((double) this.x * (double) this.x + (double) this.y * (double) this.y);
    }

    /// <summary>
    ///   <para>Gets the unsigned angle in degrees between from and to.</para>
    /// </summary>
    /// <param name="from">The vector from which the angular difference is measured.</param>
    /// <param name="to">The vector to which the angular difference is measured.</param>
    /// <returns>
    ///   <para>The unsigned angle in degrees between the two vectors.</para>
    /// </returns>
    
    public static float Angle(Vector2 from, Vector2 to)
    {
      float num = (float) Math.Sqrt((double) from.sqrMagnitude * (double) to.sqrMagnitude);
      return (double) num < 1.00000000362749E-15 ? 0.0f : (float) Math.Acos((double) Mathf.Clamp(Vector2.Dot(from, to) / num, -1f, 1f)) * 57.29578f;
    }

    /// <summary>
    ///   <para>Gets the signed angle in degrees between from and to.</para>
    /// </summary>
    /// <param name="from">The vector from which the angular difference is measured.</param>
    /// <param name="to">The vector to which the angular difference is measured.</param>
    /// <returns>
    ///   <para>The signed angle in degrees between the two vectors.</para>
    /// </returns>
    
    public static float SignedAngle(Vector2 from, Vector2 to) => Vector2.Angle(from, to) * Mathf.Sign((float) ((double) from.x * (double) to.y - (double) from.y * (double) to.x));

    /// <summary>
    ///   <para>Returns the distance between a and b.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    
    public static float Distance(Vector2 a, Vector2 b)
    {
      float num1 = a.x - b.x;
      float num2 = a.y - b.y;
      return (float) Math.Sqrt((double) num1 * (double) num1 + (double) num2 * (double) num2);
    }

    /// <summary>
    ///   <para>Returns a copy of vector with its magnitude clamped to maxLength.</para>
    /// </summary>
    /// <param name="vector"></param>
    /// <param name="maxLength"></param>
    
    public static Vector2 ClampMagnitude(Vector2 vector, float maxLength)
    {
      float sqrMagnitude = vector.sqrMagnitude;
      if ((double) sqrMagnitude <= (double) maxLength * (double) maxLength)
        return vector;
      float num1 = (float) Math.Sqrt((double) sqrMagnitude);
      float num2 = vector.x / num1;
      float num3 = vector.y / num1;
      return new Vector2(num2 * maxLength, num3 * maxLength);
    }

    
    public static float SqrMagnitude(Vector2 a) => (float) ((double) a.x * (double) a.x + (double) a.y * (double) a.y);

    
    public float SqrMagnitude() => (float) ((double) this.x * (double) this.x + (double) this.y * (double) this.y);

    /// <summary>
    ///   <para>Returns a vector that is made from the smallest components of two vectors.</para>
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    
    public static Vector2 Min(Vector2 lhs, Vector2 rhs) => new Vector2(Mathf.Min(lhs.x, rhs.x), Mathf.Min(lhs.y, rhs.y));

    /// <summary>
    ///   <para>Returns a vector that is made from the largest components of two vectors.</para>
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    
    public static Vector2 Max(Vector2 lhs, Vector2 rhs) => new Vector2(Mathf.Max(lhs.x, rhs.x), Mathf.Max(lhs.y, rhs.y));

    public static Vector2 SmoothDamp(
      Vector2 current,
      Vector2 target,
      ref Vector2 currentVelocity,
      float smoothTime,
      float maxSpeed,
      float deltaTime)
    {
      smoothTime = Mathf.Max(0.0001f, smoothTime);
      float num1 = 2f / smoothTime;
      float num2 = num1 * deltaTime;
      float num3 = (float) (1.0 / (1.0 + (double) num2 + 0.479999989271164 * (double) num2 * (double) num2 + 0.234999999403954 * (double) num2 * (double) num2 * (double) num2));
      float num4 = current.x - target.x;
      float num5 = current.y - target.y;
      Vector2 vector2 = target;
      float num6 = maxSpeed * smoothTime;
      float num7 = num6 * num6;
      float d = (float) ((double) num4 * (double) num4 + (double) num5 * (double) num5);
      if ((double) d > (double) num7)
      {
        float num8 = (float) Math.Sqrt((double) d);
        num4 = num4 / num8 * num6;
        num5 = num5 / num8 * num6;
      }
      target.x = current.x - num4;
      target.y = current.y - num5;
      float num9 = (currentVelocity.x + num1 * num4) * deltaTime;
      float num10 = (currentVelocity.y + num1 * num5) * deltaTime;
      currentVelocity.x = (currentVelocity.x - num1 * num9) * num3;
      currentVelocity.y = (currentVelocity.y - num1 * num10) * num3;
      float x = target.x + (num4 + num9) * num3;
      float y = target.y + (num5 + num10) * num3;
      float num11 = vector2.x - current.x;
      float num12 = vector2.y - current.y;
      float num13 = x - vector2.x;
      float num14 = y - vector2.y;
      if ((double) num11 * (double) num13 + (double) num12 * (double) num14 > 0.0)
      {
        x = vector2.x;
        y = vector2.y;
        currentVelocity.x = (x - vector2.x) / deltaTime;
        currentVelocity.y = (y - vector2.y) / deltaTime;
      }
      return new Vector2(x, y);
    }

    
    public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);

    
    public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);

    
    public static Vector2 operator *(Vector2 a, Vector2 b) => new Vector2(a.x * b.x, a.y * b.y);

    
    public static Vector2 operator /(Vector2 a, Vector2 b) => new Vector2(a.x / b.x, a.y / b.y);

    
    public static Vector2 operator -(Vector2 a) => new Vector2(-a.x, -a.y);

    
    public static Vector2 operator *(Vector2 a, float d) => new Vector2(a.x * d, a.y * d);

    
    public static Vector2 operator *(float d, Vector2 a) => new Vector2(a.x * d, a.y * d);

    
    public static Vector2 operator /(Vector2 a, float d) => new Vector2(a.x / d, a.y / d);

    
    public static bool operator ==(Vector2 lhs, Vector2 rhs)
    {
      float num1 = lhs.x - rhs.x;
      float num2 = lhs.y - rhs.y;
      return (double) num1 * (double) num1 + (double) num2 * (double) num2 < 9.99999943962493E-11;
    }

    
    public static bool operator !=(Vector2 lhs, Vector2 rhs) => !(lhs == rhs);

    
    public static implicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);

    
    public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0.0f);

    /// <summary>
    ///   <para>Shorthand for writing Vector2(0, 0).</para>
    /// </summary>
    public static Vector2 zero
    {
      get => Vector2.zeroVector;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector2(1, 1).</para>
    /// </summary>
    public static Vector2 one
    {
      get => Vector2.oneVector;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector2(0, 1).</para>
    /// </summary>
    public static Vector2 up
    {
      get => Vector2.upVector;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector2(0, -1).</para>
    /// </summary>
    public static Vector2 down
    {
      get => Vector2.downVector;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector2(-1, 0).</para>
    /// </summary>
    public static Vector2 left
    {
      get => Vector2.leftVector;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector2(1, 0).</para>
    /// </summary>
    public static Vector2 right
    {
      get => Vector2.rightVector;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector2(float.PositiveInfinity, float.PositiveInfinity).</para>
    /// </summary>
    public static Vector2 positiveInfinity
    {
      get => Vector2.positiveInfinityVector;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector2(float.NegativeInfinity, float.NegativeInfinity).</para>
    /// </summary>
    public static Vector2 negativeInfinity
    {
      get => Vector2.negativeInfinityVector;
    }
  }

#endif