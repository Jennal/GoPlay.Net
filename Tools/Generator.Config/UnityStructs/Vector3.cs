using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace GoPlay.Generators.Config;

#if !UNITY_EDITOR 
public struct Vector3 : IEquatable<Vector3>, IFormattable
  {
    public const float kEpsilon = 1E-05f;
    public const float kEpsilonNormalSqrt = 1E-15f;
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
    private static readonly Vector3 zeroVector = new Vector3(0.0f, 0.0f, 0.0f);
    private static readonly Vector3 oneVector = new Vector3(1f, 1f, 1f);
    private static readonly Vector3 upVector = new Vector3(0.0f, 1f, 0.0f);
    private static readonly Vector3 downVector = new Vector3(0.0f, -1f, 0.0f);
    private static readonly Vector3 leftVector = new Vector3(-1f, 0.0f, 0.0f);
    private static readonly Vector3 rightVector = new Vector3(1f, 0.0f, 0.0f);
    private static readonly Vector3 forwardVector = new Vector3(0.0f, 0.0f, 1f);
    private static readonly Vector3 backVector = new Vector3(0.0f, 0.0f, -1f);
    private static readonly Vector3 positiveInfinityVector = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
    private static readonly Vector3 negativeInfinityVector = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

    /// <summary>
    ///   <para>Linearly interpolates between two points.</para>
    /// </summary>
    /// <param name="a">Start value, returned when t = 0.</param>
    /// <param name="b">End value, returned when t = 1.</param>
    /// <param name="t">Value used to interpolate between a and b.</param>
    /// <returns>
    ///   <para>Interpolated value, equals to a + (b - a) * t.</para>
    /// </returns>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
    {
      t = Mathf.Clamp01(t);
      return new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
    }

    /// <summary>
    ///   <para>Linearly interpolates between two vectors.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t) => new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);

    /// <summary>
    ///   <para>Calculate a position between the points specified by current and target, moving no farther than the distance specified by maxDistanceDelta.</para>
    /// </summary>
    /// <param name="current">The position to move from.</param>
    /// <param name="target">The position to move towards.</param>
    /// <param name="maxDistanceDelta">Distance to move current per call.</param>
    /// <returns>
    ///   <para>The new position.</para>
    /// </returns>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3 MoveTowards(
      Vector3 current,
      Vector3 target,
      float maxDistanceDelta)
    {
      float num1 = target.x - current.x;
      float num2 = target.y - current.y;
      float num3 = target.z - current.z;
      float d = (float) ((double) num1 * (double) num1 + (double) num2 * (double) num2 + (double) num3 * (double) num3);
      if ((double) d == 0.0 || (double) maxDistanceDelta >= 0.0 && (double) d <= (double) maxDistanceDelta * (double) maxDistanceDelta)
        return target;
      float num4 = (float) Math.Sqrt((double) d);
      return new Vector3(current.x + num1 / num4 * maxDistanceDelta, current.y + num2 / num4 * maxDistanceDelta, current.z + num3 / num4 * maxDistanceDelta);
    }

    public static Vector3 SmoothDamp(
      Vector3 current,
      Vector3 target,
      ref Vector3 currentVelocity,
      float smoothTime,
      [DefaultValue("Mathf.Infinity")] float maxSpeed,
      [DefaultValue("Time.deltaTime")] float deltaTime)
    {
      smoothTime = Mathf.Max(0.0001f, smoothTime);
      float num1 = 2f / smoothTime;
      float num2 = num1 * deltaTime;
      float num3 = (float) (1.0 / (1.0 + (double) num2 + 0.479999989271164 * (double) num2 * (double) num2 + 0.234999999403954 * (double) num2 * (double) num2 * (double) num2));
      float num4 = current.x - target.x;
      float num5 = current.y - target.y;
      float num6 = current.z - target.z;
      Vector3 vector3 = target;
      float num7 = maxSpeed * smoothTime;
      float num8 = num7 * num7;
      float d = (float) ((double) num4 * (double) num4 + (double) num5 * (double) num5 + (double) num6 * (double) num6);
      if ((double) d > (double) num8)
      {
        float num9 = (float) Math.Sqrt((double) d);
        num4 = num4 / num9 * num7;
        num5 = num5 / num9 * num7;
        num6 = num6 / num9 * num7;
      }
      target.x = current.x - num4;
      target.y = current.y - num5;
      target.z = current.z - num6;
      float num10 = (currentVelocity.x + num1 * num4) * deltaTime;
      float num11 = (currentVelocity.y + num1 * num5) * deltaTime;
      float num12 = (currentVelocity.z + num1 * num6) * deltaTime;
      currentVelocity.x = (currentVelocity.x - num1 * num10) * num3;
      currentVelocity.y = (currentVelocity.y - num1 * num11) * num3;
      currentVelocity.z = (currentVelocity.z - num1 * num12) * num3;
      float x = target.x + (num4 + num10) * num3;
      float y = target.y + (num5 + num11) * num3;
      float z = target.z + (num6 + num12) * num3;
      float num13 = vector3.x - current.x;
      float num14 = vector3.y - current.y;
      float num15 = vector3.z - current.z;
      float num16 = x - vector3.x;
      float num17 = y - vector3.y;
      float num18 = z - vector3.z;
      if ((double) num13 * (double) num16 + (double) num14 * (double) num17 + (double) num15 * (double) num18 > 0.0)
      {
        x = vector3.x;
        y = vector3.y;
        z = vector3.z;
        currentVelocity.x = (x - vector3.x) / deltaTime;
        currentVelocity.y = (y - vector3.y) / deltaTime;
        currentVelocity.z = (z - vector3.z) / deltaTime;
      }
      return new Vector3(x, y, z);
    }

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
          default:
            throw new IndexOutOfRangeException("Invalid Vector3 index!");
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
            throw new IndexOutOfRangeException("Invalid Vector3 index!");
        }
      }
    }

    /// <summary>
    ///   <para>Creates a new vector with given x, y, z components.</para>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public Vector3(float x, float y, float z)
    {
      this.x = x;
      this.y = y;
      this.z = z;
    }

    /// <summary>
    ///   <para>Creates a new vector with given x, y components and sets z to zero.</para>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public Vector3(float x, float y)
    {
      this.x = x;
      this.y = y;
      this.z = 0.0f;
    }

    /// <summary>
    ///   <para>Set x, y and z components of an existing Vector3.</para>
    /// </summary>
    /// <param name="newX"></param>
    /// <param name="newY"></param>
    /// <param name="newZ"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public void Set(float newX, float newY, float newZ)
    {
      this.x = newX;
      this.y = newY;
      this.z = newZ;
    }

    /// <summary>
    ///   <para>Multiplies two vectors component-wise.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3 Scale(Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);

    /// <summary>
    ///   <para>Multiplies every component of this vector by the same component of scale.</para>
    /// </summary>
    /// <param name="scale"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public void Scale(Vector3 scale)
    {
      this.x *= scale.x;
      this.y *= scale.y;
      this.z *= scale.z;
    }

    /// <summary>
    ///   <para>Cross Product of two vectors.</para>
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3 Cross(Vector3 lhs, Vector3 rhs) => new Vector3((float) ((double) lhs.y * (double) rhs.z - (double) lhs.z * (double) rhs.y), (float) ((double) lhs.z * (double) rhs.x - (double) lhs.x * (double) rhs.z), (float) ((double) lhs.x * (double) rhs.y - (double) lhs.y * (double) rhs.x));

    [MethodImpl((MethodImplOptions) 256)]
    public override int GetHashCode() => this.x.GetHashCode() ^ this.y.GetHashCode() << 2 ^ this.z.GetHashCode() >> 2;

    /// <summary>
    ///   <para>Returns true if the given vector is exactly equal to this vector.</para>
    /// </summary>
    /// <param name="other"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public override bool Equals(object other) => other is Vector3 other1 && this.Equals(other1);

    [MethodImpl((MethodImplOptions) 256)]
    public bool Equals(Vector3 other) => (double) this.x == (double) other.x && (double) this.y == (double) other.y && (double) this.z == (double) other.z;

    /// <summary>
    ///   <para>Reflects a vector off the plane defined by a normal.</para>
    /// </summary>
    /// <param name="inDirection"></param>
    /// <param name="inNormal"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3 Reflect(Vector3 inDirection, Vector3 inNormal)
    {
      float num = -2f * Vector3.Dot(inNormal, inDirection);
      return new Vector3(num * inNormal.x + inDirection.x, num * inNormal.y + inDirection.y, num * inNormal.z + inDirection.z);
    }

    /// <summary>
    ///   <para>Makes this vector have a magnitude of 1.</para>
    /// </summary>
    /// <param name="value"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3 Normalize(Vector3 value)
    {
      float num = Vector3.Magnitude(value);
      return (double) num > 9.99999974737875E-06 ? value / num : Vector3.zero;
    }

    [MethodImpl((MethodImplOptions) 256)]
    public void Normalize()
    {
      float num = Vector3.Magnitude(this);
      if ((double) num > 9.99999974737875E-06)
        this = this / num;
      else
        this = Vector3.zero;
    }

    /// <summary>
    ///   <para>Returns this vector with a magnitude of 1 (Read Only).</para>
    /// </summary>
    public Vector3 normalized
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector3.Normalize(this);
    }

    /// <summary>
    ///   <para>Dot Product of two vectors.</para>
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static float Dot(Vector3 lhs, Vector3 rhs) => (float) ((double) lhs.x * (double) rhs.x + (double) lhs.y * (double) rhs.y + (double) lhs.z * (double) rhs.z);

    /// <summary>
    ///   <para>Projects a vector onto another vector.</para>
    /// </summary>
    /// <param name="vector"></param>
    /// <param name="onNormal"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3 Project(Vector3 vector, Vector3 onNormal)
    {
      float num1 = Vector3.Dot(onNormal, onNormal);
      if ((double) num1 < (double) Mathf.Epsilon)
        return Vector3.zero;
      float num2 = Vector3.Dot(vector, onNormal);
      return new Vector3(onNormal.x * num2 / num1, onNormal.y * num2 / num1, onNormal.z * num2 / num1);
    }

    /// <summary>
    ///   <para>Projects a vector onto a plane defined by a normal orthogonal to the plane.</para>
    /// </summary>
    /// <param name="planeNormal">The direction from the vector towards the plane.</param>
    /// <param name="vector">The location of the vector above the plane.</param>
    /// <returns>
    ///   <para>The location of the vector on the plane.</para>
    /// </returns>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal)
    {
      float num1 = Vector3.Dot(planeNormal, planeNormal);
      if ((double) num1 < (double) Mathf.Epsilon)
        return vector;
      float num2 = Vector3.Dot(vector, planeNormal);
      return new Vector3(vector.x - planeNormal.x * num2 / num1, vector.y - planeNormal.y * num2 / num1, vector.z - planeNormal.z * num2 / num1);
    }

    /// <summary>
    ///   <para>Calculates the angle between vectors from and.</para>
    /// </summary>
    /// <param name="from">The vector from which the angular difference is measured.</param>
    /// <param name="to">The vector to which the angular difference is measured.</param>
    /// <returns>
    ///   <para>The angle in degrees between the two vectors.</para>
    /// </returns>
    [MethodImpl((MethodImplOptions) 256)]
    public static float Angle(Vector3 from, Vector3 to)
    {
      float num = (float) Math.Sqrt((double) from.sqrMagnitude * (double) to.sqrMagnitude);
      return (double) num < 1.00000000362749E-15 ? 0.0f : (float) Math.Acos((double) Mathf.Clamp(Vector3.Dot(from, to) / num, -1f, 1f)) * 57.29578f;
    }

    /// <summary>
    ///   <para>Calculates the signed angle between vectors from and to in relation to axis.</para>
    /// </summary>
    /// <param name="from">The vector from which the angular difference is measured.</param>
    /// <param name="to">The vector to which the angular difference is measured.</param>
    /// <param name="axis">A vector around which the other vectors are rotated.</param>
    /// <returns>
    ///   <para>Returns the signed angle between from and to in degrees.</para>
    /// </returns>
    [MethodImpl((MethodImplOptions) 256)]
    public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis)
    {
      float num1 = Vector3.Angle(from, to);
      float num2 = (float) ((double) from.y * (double) to.z - (double) from.z * (double) to.y);
      float num3 = (float) ((double) from.z * (double) to.x - (double) from.x * (double) to.z);
      float num4 = (float) ((double) from.x * (double) to.y - (double) from.y * (double) to.x);
      float num5 = Mathf.Sign((float) ((double) axis.x * (double) num2 + (double) axis.y * (double) num3 + (double) axis.z * (double) num4));
      return num1 * num5;
    }

    /// <summary>
    ///   <para>Returns the distance between a and b.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static float Distance(Vector3 a, Vector3 b)
    {
      float num1 = a.x - b.x;
      float num2 = a.y - b.y;
      float num3 = a.z - b.z;
      return (float) Math.Sqrt((double) num1 * (double) num1 + (double) num2 * (double) num2 + (double) num3 * (double) num3);
    }

    /// <summary>
    ///   <para>Returns a copy of vector with its magnitude clamped to maxLength.</para>
    /// </summary>
    /// <param name="vector"></param>
    /// <param name="maxLength"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3 ClampMagnitude(Vector3 vector, float maxLength)
    {
      float sqrMagnitude = vector.sqrMagnitude;
      if ((double) sqrMagnitude <= (double) maxLength * (double) maxLength)
        return vector;
      float num1 = (float) Math.Sqrt((double) sqrMagnitude);
      float num2 = vector.x / num1;
      float num3 = vector.y / num1;
      float num4 = vector.z / num1;
      return new Vector3(num2 * maxLength, num3 * maxLength, num4 * maxLength);
    }

    [MethodImpl((MethodImplOptions) 256)]
    public static float Magnitude(Vector3 vector) => (float) Math.Sqrt((double) vector.x * (double) vector.x + (double) vector.y * (double) vector.y + (double) vector.z * (double) vector.z);

    /// <summary>
    ///   <para>Returns the length of this vector (Read Only).</para>
    /// </summary>
    public float magnitude
    {
      [MethodImpl((MethodImplOptions) 256)] get => (float) Math.Sqrt((double) this.x * (double) this.x + (double) this.y * (double) this.y + (double) this.z * (double) this.z);
    }

    [MethodImpl((MethodImplOptions) 256)]
    public static float SqrMagnitude(Vector3 vector) => (float) ((double) vector.x * (double) vector.x + (double) vector.y * (double) vector.y + (double) vector.z * (double) vector.z);

    /// <summary>
    ///   <para>Returns the squared length of this vector (Read Only).</para>
    /// </summary>
    public float sqrMagnitude
    {
      [MethodImpl((MethodImplOptions) 256)] get => (float) ((double) this.x * (double) this.x + (double) this.y * (double) this.y + (double) this.z * (double) this.z);
    }

    /// <summary>
    ///   <para>Returns a vector that is made from the smallest components of two vectors.</para>
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3 Min(Vector3 lhs, Vector3 rhs) => new Vector3(Mathf.Min(lhs.x, rhs.x), Mathf.Min(lhs.y, rhs.y), Mathf.Min(lhs.z, rhs.z));

    /// <summary>
    ///   <para>Returns a vector that is made from the largest components of two vectors.</para>
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3 Max(Vector3 lhs, Vector3 rhs) => new Vector3(Mathf.Max(lhs.x, rhs.x), Mathf.Max(lhs.y, rhs.y), Mathf.Max(lhs.z, rhs.z));

    /// <summary>
    ///   <para>Shorthand for writing Vector3(0, 0, 0).</para>
    /// </summary>
    public static Vector3 zero
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector3.zeroVector;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector3(1, 1, 1).</para>
    /// </summary>
    public static Vector3 one
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector3.oneVector;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector3(0, 0, 1).</para>
    /// </summary>
    public static Vector3 forward
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector3.forwardVector;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector3(0, 0, -1).</para>
    /// </summary>
    public static Vector3 back
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector3.backVector;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector3(0, 1, 0).</para>
    /// </summary>
    public static Vector3 up
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector3.upVector;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector3(0, -1, 0).</para>
    /// </summary>
    public static Vector3 down
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector3.downVector;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector3(-1, 0, 0).</para>
    /// </summary>
    public static Vector3 left
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector3.leftVector;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector3(1, 0, 0).</para>
    /// </summary>
    public static Vector3 right
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector3.rightVector;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity).</para>
    /// </summary>
    public static Vector3 positiveInfinity
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector3.positiveInfinityVector;
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity).</para>
    /// </summary>
    public static Vector3 negativeInfinity
    {
      [MethodImpl((MethodImplOptions) 256)] get => Vector3.negativeInfinityVector;
    }

    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);

    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);

    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);

    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d);

    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3 operator *(float d, Vector3 a) => new Vector3(a.x * d, a.y * d, a.z * d);

    [MethodImpl((MethodImplOptions) 256)]
    public static Vector3 operator /(Vector3 a, float d) => new Vector3(a.x / d, a.y / d, a.z / d);

    [MethodImpl((MethodImplOptions) 256)]
    public static bool operator ==(Vector3 lhs, Vector3 rhs)
    {
      float num1 = lhs.x - rhs.x;
      float num2 = lhs.y - rhs.y;
      float num3 = lhs.z - rhs.z;
      return (double) num1 * (double) num1 + (double) num2 * (double) num2 + (double) num3 * (double) num3 < 9.99999943962493E-11;
    }

    [MethodImpl((MethodImplOptions) 256)]
    public static bool operator !=(Vector3 lhs, Vector3 rhs) => !(lhs == rhs);

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
      return string.Format("({0}, {1}, {2})", (object) this.x.ToString(format, formatProvider), (object) this.y.ToString(format, formatProvider), (object) this.z.ToString(format, formatProvider));
    }

    [Obsolete("Use Vector3.forward instead.")]
    public static Vector3 fwd => new Vector3(0.0f, 0.0f, 1f);

    [Obsolete("Use Vector3.Angle instead. AngleBetween uses radians instead of degrees and was deprecated for this reason")]
    public static float AngleBetween(Vector3 from, Vector3 to) => (float) Math.Acos((double) Mathf.Clamp(Vector3.Dot(from.normalized, to.normalized), -1f, 1f));

    [Obsolete("Use Vector3.ProjectOnPlane instead.")]
    public static Vector3 Exclude(Vector3 excludeThis, Vector3 fromThat) => Vector3.ProjectOnPlane(fromThat, excludeThis);

  }

#endif