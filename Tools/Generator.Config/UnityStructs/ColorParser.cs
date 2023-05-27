using System.Text.RegularExpressions;

namespace GoPlay.Generators.Config {

public class ColorParser {
    
    public static Color HSVToRGB(float hue, float saturation, float value)
    {
        if (saturation == 0f) {
            return new Color(value, value, value);
        } else if (value == 0f) {
            return Color.black;
        } else {
            Color col  = Color.black;
            float Hval = hue * 6f;
            int sel    = Mathf.FloorToInt(Hval);
            float mod  = Hval - sel;
            float v1   = value * (1f - saturation);
            float v2   = value * (1f - saturation * mod);
            float v3   = value * (1f - saturation * (1f - mod));
            switch (sel + 1)
            {
                case 0:
                    col.r = value;
                    col.g = v1;
                    col.b = v2;
                    break;
                case 1:
                    col.r = value;
                    col.g = v3;
                    col.b = v1;
                    break;
                case 2:
                    col.r = v2;
                    col.g = value;
                    col.b = v1;
                    break;
                case 3:
                    col.r = v1;
                    col.g = value;
                    col.b = v3;
                    break;
                case 4:
                    col.r = v1;
                    col.g = v2;
                    col.b = value;
                    break;
                case 5:
                    col.r = v3;
                    col.g = v1;
                    col.b = value;
                    break;
                case 6:
                    col.r = value;
                    col.g = v1;
                    col.b = v2;
                    break;
                case 7:
                    col.r = value;
                    col.g = v3;
                    col.b = v1;
                    break;
            }
            col.r = Mathf.Clamp(col.r, 0f, 1f);
            col.g = Mathf.Clamp(col.g, 0f, 1f);
            col.b = Mathf.Clamp(col.b, 0f, 1f);
            return col;
        }
    }
        
    static public bool TryParseCSSColor(string s, out Color color) {
        // I have no idea what all the color formats are. The ones here are
        // name:         red,              green,            purple
        // #RGB:         #F00,             #0F0,             #F0F
        // #RRGGBB       #FF0000,          #00FF00,          #FF00FF
        // rgb(r,g,b)    rgb(255,0,0)      rgb(0,255,0),     rgb(255,0,255)
        // rgba(r,g,b,a) rgba(255,0,0,1.0) rgba(0,255,0,1.0) rgba(255,0,255,1.0)

        color = new Color();
        Match m;
        m = ColorParser.m_hexrrggbbRE.Match(s);
        if (m.Success) {
            color.r = int.Parse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber) / 255.0f;
            color.g = int.Parse(m.Groups[2].Value, System.Globalization.NumberStyles.HexNumber) / 255.0f;
            color.b = int.Parse(m.Groups[3].Value, System.Globalization.NumberStyles.HexNumber) / 255.0f;
            color.a = 1.0f;
            return true;
        }

        m = ColorParser.m_hexrgbRE.Match(s);
        if (m.Success) {
            color.r = int.Parse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber) / 15.0f;
            color.g = int.Parse(m.Groups[2].Value, System.Globalization.NumberStyles.HexNumber) / 15.0f;
            color.b = int.Parse(m.Groups[3].Value, System.Globalization.NumberStyles.HexNumber) / 15.0f;
            color.a = 1.0f;
            return true;
        }

        m = ColorParser.m_rgbRE.Match(s);
        if (m.Success) {
            color.r = float.Parse(m.Groups[1].Value) / 255.0f;
            color.g = float.Parse(m.Groups[2].Value) / 255.0f;
            color.b = float.Parse(m.Groups[3].Value) / 255.0f;
            color.a = 1.0f;
            return true;
        }

        m = ColorParser.m_rgbaRE.Match(s);
        if (m.Success) {
            color.r = float.Parse(m.Groups[1].Value) / 255.0f;
            color.g = float.Parse(m.Groups[2].Value) / 255.0f;
            color.b = float.Parse(m.Groups[3].Value) / 255.0f;
            color.a = float.Parse(m.Groups[4].Value);
            return true;
        }
        
        m = ColorParser.m_hslaRE.Match(s);
        if (m.Success) {
            float hue   = float.Parse(m.Groups[1].Value) / 360.0f;
            float sat   = float.Parse(m.Groups[2].Value) / 100.0f;
            float value = float.Parse(m.Groups[3].Value) / 100.0f;

            Color rgb = HSVToRGB(hue, sat, value);

            color.r = rgb.r;
            color.g = rgb.g;
            color.b = rgb.b;
            color.a = float.Parse(m.Groups[4].Value);
            return true;
        }

        m = ColorParser.m_nameRE.Match(s);
        if (m.Success) {
            ColorParser.InitColors();
            string name = m.Groups[1].Value;
            string lcName = name.ToLowerInvariant();
            if (!m_colors.TryGetValue(lcName, out color)) {
                ExporterUtils.Error("unknown color: " + name);
                return false;
            }
            return true;
        }
        
        ExporterUtils.Error("unsupported color format: " + s);
        return false;
    }

    private static Regex m_hexrrggbbRE = new Regex(@"\s*#([0-9a-fA-F][0-9a-fA-F])([0-9a-fA-F][0-9a-fA-F])([0-9a-fA-F][0-9a-fA-F])\s*");
    private static Regex m_hexrgbRE = new Regex(@"\s*#([0-9a-fA-F])([0-9a-fA-F])([0-9a-fA-F])\s*");
    private static Regex m_rgbRE = new Regex(@"\s*rgb\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)\s*");
    private static Regex m_rgbaRE = new Regex(@"\s*rgba\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(.+)\)\s*");
    private static Regex m_hslaRE = new Regex(@"\s*hsla\s*\(\s*(\d+)\s*,\s*(\d+)%\s*,\s*(\d+)%\s*,\s*(.+)\)\s*");
    private static Regex m_nameRE = new Regex(@"\s*(\w+)\s*");

    static private Dictionary<string, Color> m_colors;

    private static void InitColors() {
        if (m_colors == null) {
            m_colors = new Dictionary<string, Color>();
            string[] parts = m_colorData.Split(',');
            for (int ii = 0; ii < parts.Length; ii += 2) {
                string name = parts[ii + 0].Trim();
                string colorString = parts[ii + 1].Trim();
                Color color = new Color();
                color.r = int.Parse(colorString.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) / 255.0f;
                color.g = int.Parse(colorString.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) / 255.0f;
                color.b = int.Parse(colorString.Substring(6, 2), System.Globalization.NumberStyles.HexNumber) / 255.0f;
                color.a = int.Parse(colorString.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255.0f;

                m_colors[name] = color;
            }
        }
    }

    // In .NET 2.0 there's no easy way to have static data?
    private static string m_colorData = @"aliceblue,fff0f8ff,
antiquewhite,fffaebd7,
aqua,ff00ffff,
aquamarine,ff7fffd4,
azure,fff0ffff,
beige,fff5f5dc,
bisque,ffffe4c4,
black,ff000000,
blanchedalmond,ffffebcd,
blue,ff0000ff,
blueviolet,ff8a2be2,
brown,ffa52a2a,
burlywood,ffdeb887,
cadetblue,ff5f9ea0,
chartreuse,ff7fff00,
chocolate,ffd2691e,
coral,ffff7f50,
cornflowerblue,ff6495ed,
cornsilk,fffff8dc,
crimson,ffdc143c,
cyan,ff00ffff,
darkblue,ff00008b,
darkcyan,ff008b8b,
darkgoldenrod,ffb8860b,
darkgray,ffa9a9a9,
darkgrey,ffa9a9a9,
darkgreen,ff006400,
darkkhaki,ffbdb76b,
darkmagenta,ff8b008b,
darkolivegreen,ff556b2f,
darkorange,ffff8c00,
darkorchid,ff9932cc,
darkred,ff8b0000,
darksalmon,ffe9967a,
darkseagreen,ff8fbc8f,
darkslateblue,ff483d8b,
darkslategray,ff2f4f4f,
darkslategrey,ff2f4f4f,
darkturquoise,ff00ced1,
darkviolet,ff9400d3,
deeppink,ffff1493,
deepskyblue,ff00bfff,
dimgray,ff696969,
dimgrey,ff696969,
dodgerblue,ff1e90ff,
firebrick,ffb22222,
floralwhite,fffffaf0,
forestgreen,ff228b22,
fuchsia,ffff00ff,
gainsboro,ffdcdcdc,
ghostwhite,fff8f8ff,
gold,ffffd700,
goldenrod,ffdaa520,
gray,ff808080,
grey,ff808080,
green,ff008000,
greenyellow,ffadff2f,
honeydew,fff0fff0,
hotpink,ffff69b4,
indianred,ffcd5c5c,
indigo,ff4b0082,
ivory,fffffff0,
khaki,fff0e68c,
lavender,ffe6e6fa,
lavenderblush,fffff0f5,
lawngreen,ff7cfc00,
lemonchiffon,fffffacd,
lightblue,ffadd8e6,
lightcoral,fff08080,
lightcyan,ffe0ffff,
lightgoldenrodyellow,fffafad2,
lightgray,ffd3d3d3,
lightgrey,ffd3d3d3,
lightgreen,ff90ee90,
lightpink,ffffb6c1,
lightsalmon,ffffa07a,
lightseagreen,ff20b2aa,
lightskyblue,ff87cefa,
lightslateblue,ff8470ff,
lightslategray,ff778899,
lightslategrey,ff778899,
lightsteelblue,ffb0c4de,
lightyellow,ffffffe0,
lime,ff00ff00,
limegreen,ff32cd32,
linen,fffaf0e6,
magenta,ffff00ff,
maroon,ff800000,
mediumaquamarine,ff66cdaa,
mediumblue,ff0000cd,
mediumorchid,ffba55d3,
mediumpurple,ff9370db,
mediumseagreen,ff3cb371,
mediumslateblue,ff7b68ee,
mediumspringgreen,ff00fa9a,
mediumturquoise,ff48d1cc,
mediumvioletred,ffc71585,
midnightblue,ff191970,
mintcream,fff5fffa,
mistyrose,ffffe4e1,
moccasin,ffffe4b5,
navajowhite,ffffdead,
navy,ff000080,
oldlace,fffdf5e6,
olive,ff808000,
olivedrab,ff6b8e23,
orange,ffffa500,
orangered,ffff4500,
orchid,ffda70d6,
palegoldenrod,ffeee8aa,
palegreen,ff98fb98,
paleturquoise,ffafeeee,
palevioletred,ffdb7093,
papayawhip,ffffefd5,
peachpuff,ffffdab9,
peru,ffcd853f,
pink,ffffc0cb,
plum,ffdda0dd,
powderblue,ffb0e0e6,
purple,ff800080,
red,ffff0000,
rosybrown,ffbc8f8f,
royalblue,ff4169e1,
saddlebrown,ff8b4513,
salmon,fffa8072,
sandybrown,fff4a460,
seagreen,ff2e8b57,
seashell,fffff5ee,
sienna,ffa0522d,
silver,ffc0c0c0,
skyblue,ff87ceeb,
slateblue,ff6a5acd,
slategray,ff708090,
slategrey,ff708090,
snow,fffffafa,
springgreen,ff00ff7f,
steelblue,ff4682b4,
tan,ffd2b48c,
teal,ff008080,
thistle,ffd8bfd8,
tomato,ffff6347,
transparent,00000000,
turquoise,ff40e0d0,
violet,ffee82ee,
violetred,ffd02090,
wheat,fff5deb3,
white,ffffffff,
whitesmoke,fff5f5f5,
yellow,ffffff00,
yellowgreen,ff9acd32";
}

/*
        string[] res = {
            "#1af",
            " #1af ",
            "#12abef",
            " #12abef ",
            "rgb(30,100,255)",
            " rgb( 30 , 100 , 255 ) ",
            "rgba(30,100,255,0.5)",
            " rgba( 30 , 100 , 255 , 0.5 ) ",
            "green",
            " purple ",
        };
        for (int ii = 0; ii < res.Length; ++ii) {
            Color x = Style.ParseCSSColor(res[ii]);
            print("" + ii + ": " + x.r + ", " + x.g + ", " + x.b + ", " + x.a);
        }
*/

}  // namespace CSSParse
