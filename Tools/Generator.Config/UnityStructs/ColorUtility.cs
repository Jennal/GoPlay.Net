namespace GoPlay.Generators.Config;

#if !UNITY_EDITOR
public class ColorUtility
{
    static bool DoTryParseHtmlColor(string htmlString, out Color32 color)
    {
        var c = new Color();
        color = new Color32();
        if (!ColorParser.TryParseCSSColor(htmlString, out c)) return false;

        color = c;
        return true;
    }

    public static bool TryParseHtmlString(string htmlString, out Color color)
    {
        Color32 color1;
        bool htmlColor = ColorUtility.DoTryParseHtmlColor(htmlString, out color1);
        color = (Color) color1;
        return htmlColor;
    }

    /// <summary>
    ///   <para>Returns the color as a hexadecimal string in the format "RRGGBB".</para>
    /// </summary>
    /// <param name="color">The color to be converted.</param>
    /// <returns>
    ///   <para>Hexadecimal string representing the color.</para>
    /// </returns>
    public static string ToHtmlStringRGB(Color color)
    {
        Color32 color32 = new Color32(
            (byte) Mathf.Clamp(Mathf.RoundToInt(color.r * (float) byte.MaxValue), 0, (int) byte.MaxValue),
            (byte) Mathf.Clamp(Mathf.RoundToInt(color.g * (float) byte.MaxValue), 0, (int) byte.MaxValue),
            (byte) Mathf.Clamp(Mathf.RoundToInt(color.b * (float) byte.MaxValue), 0, (int) byte.MaxValue), (byte) 1);
        return string.Format("{0:X2}{1:X2}{2:X2}", (object) color32.r, (object) color32.g, (object) color32.b);
    }

    /// <summary>
    ///   <para>Returns the color as a hexadecimal string in the format "RRGGBBAA".</para>
    /// </summary>
    /// <param name="color">The color to be converted.</param>
    /// <returns>
    ///   <para>Hexadecimal string representing the color.</para>
    /// </returns>
    public static string ToHtmlStringRGBA(Color color)
    {
        Color32 color32 = new Color32(
            (byte) Mathf.Clamp(Mathf.RoundToInt(color.r * (float) byte.MaxValue), 0, (int) byte.MaxValue),
            (byte) Mathf.Clamp(Mathf.RoundToInt(color.g * (float) byte.MaxValue), 0, (int) byte.MaxValue),
            (byte) Mathf.Clamp(Mathf.RoundToInt(color.b * (float) byte.MaxValue), 0, (int) byte.MaxValue),
            (byte) Mathf.Clamp(Mathf.RoundToInt(color.a * (float) byte.MaxValue), 0, (int) byte.MaxValue));
        return string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", (object) color32.r, (object) color32.g, (object) color32.b,
            (object) color32.a);
    }
}

#endif