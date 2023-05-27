using System.Globalization;
using System.Runtime.InteropServices;

namespace GoPlay.Generators.Config;

#if !UNITY_EDITOR
[StructLayout(LayoutKind.Explicit)]
public struct Color32 : IFormattable
  {
    [FieldOffset(0)]
    private int rgba;
    /// <summary>
    ///   <para>Red component of the color.</para>
    /// </summary>
    [FieldOffset(0)]
    public byte r;
    /// <summary>
    ///   <para>Green component of the color.</para>
    /// </summary>
    [FieldOffset(1)]
    public byte g;
    /// <summary>
    ///   <para>Blue component of the color.</para>
    /// </summary>
    [FieldOffset(2)]
    public byte b;
    /// <summary>
    ///   <para>Alpha component of the color.</para>
    /// </summary>
    [FieldOffset(3)]
    public byte a;

    /// <summary>
    ///   <para>Constructs a new Color32 with given r, g, b, a components.</para>
    /// </summary>
    /// <param name="r"></param>
    /// <param name="g"></param>
    /// <param name="b"></param>
    /// <param name="a"></param>
    public Color32(byte r, byte g, byte b, byte a)
    {
      this.rgba = 0;
      this.r = r;
      this.g = g;
      this.b = b;
      this.a = a;
    }

    public static implicit operator Color32(Color c) => new Color32((byte) Mathf.Round(Mathf.Clamp01(c.r) * (float) byte.MaxValue), (byte) Mathf.Round(Mathf.Clamp01(c.g) * (float) byte.MaxValue), (byte) Mathf.Round(Mathf.Clamp01(c.b) * (float) byte.MaxValue), (byte) Mathf.Round(Mathf.Clamp01(c.a) * (float) byte.MaxValue));

    public static implicit operator Color(Color32 c) => new Color((float) c.r / (float) byte.MaxValue, (float) c.g / (float) byte.MaxValue, (float) c.b / (float) byte.MaxValue, (float) c.a / (float) byte.MaxValue);

    /// <summary>
    ///   <para>Linearly interpolates between colors a and b by t.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    public static Color32 Lerp(Color32 a, Color32 b, float t)
    {
      t = Mathf.Clamp01(t);
      return new Color32((byte) ((double) a.r + (double) ((int) b.r - (int) a.r) * (double) t), (byte) ((double) a.g + (double) ((int) b.g - (int) a.g) * (double) t), (byte) ((double) a.b + (double) ((int) b.b - (int) a.b) * (double) t), (byte) ((double) a.a + (double) ((int) b.a - (int) a.a) * (double) t));
    }

    /// <summary>
    ///   <para>Linearly interpolates between colors a and b by t.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    public static Color32 LerpUnclamped(Color32 a, Color32 b, float t) => new Color32((byte) ((double) a.r + (double) ((int) b.r - (int) a.r) * (double) t), (byte) ((double) a.g + (double) ((int) b.g - (int) a.g) * (double) t), (byte) ((double) a.b + (double) ((int) b.b - (int) a.b) * (double) t), (byte) ((double) a.a + (double) ((int) b.a - (int) a.a) * (double) t));

    public byte this[int index]
    {
      get
      {
        switch (index)
        {
          case 0:
            return this.r;
          case 1:
            return this.g;
          case 2:
            return this.b;
          case 3:
            return this.a;
          default:
            throw new IndexOutOfRangeException("Invalid Color32 index(" + index.ToString() + ")!");
        }
      }
      set
      {
        switch (index)
        {
          case 0:
            this.r = value;
            break;
          case 1:
            this.g = value;
            break;
          case 2:
            this.b = value;
            break;
          case 3:
            this.a = value;
            break;
          default:
            throw new IndexOutOfRangeException("Invalid Color32 index(" + index.ToString() + ")!");
        }
      }
    }

    internal bool InternalEquals(Color32 other) => this.rgba == other.rgba;

    /// <summary>
    ///   <para>Returns a formatted string for this color.</para>
    /// </summary>
    /// <param name="format">A numeric format string.</param>
    /// <param name="formatProvider">An object that specifies culture-specific formatting.</param>
    public override string ToString() => this.ToString((string) null, (IFormatProvider) null);

    /// <summary>
    ///   <para>Returns a formatted string for this color.</para>
    /// </summary>
    /// <param name="format">A numeric format string.</param>
    /// <param name="formatProvider">An object that specifies culture-specific formatting.</param>
    public string ToString(string format) => this.ToString(format, (IFormatProvider) null);

    /// <summary>
    ///   <para>Returns a formatted string for this color.</para>
    /// </summary>
    /// <param name="format">A numeric format string.</param>
    /// <param name="formatProvider">An object that specifies culture-specific formatting.</param>
    public string ToString(string format, IFormatProvider formatProvider)
    {
      if (formatProvider == null)
        formatProvider = (IFormatProvider) CultureInfo.InvariantCulture.NumberFormat;
      return string.Format("RGBA({0}, {1}, {2}, {3})", (object) this.r.ToString(format, formatProvider), (object) this.g.ToString(format, formatProvider), (object) this.b.ToString(format, formatProvider), (object) this.a.ToString(format, formatProvider));
    }
  }
#endif