using System.Numerics;
using System.Text.RegularExpressions;

namespace GoPlayProj.Extensions;

public static class StringExtensions
{
    private static Regex wordRegex = new(@"\b\w+\b");
    private static Regex nonWordRegex = new(@"[^\w\s]");

    public static (string, uint) ToSI(this string val, string sep = "_")
    {
        var arr = val.Split(sep, StringSplitOptions.RemoveEmptyEntries);
        return (arr[0], uint.Parse(arr[1]));
    }
    
    public static int[] ToInts(this string val, string sep = ",")
    {
        var arr = val.Split(sep, StringSplitOptions.RemoveEmptyEntries);
        var ints = new int[arr.Length];
        for (var i = 0; i < arr.Length; i++)
        {
            ints[i] = int.Parse(arr[i]);
        }

        return ints;
    }

    public static int GetCostCount(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return 0;
        }

        // 匹配英文单词
        var wordMatches = wordRegex.Matches(input);

        // 匹配非英文字符
        var nonWordMatches = nonWordRegex.Matches(input);

        return wordMatches.Count + nonWordMatches.Count;
    }

    public static BigInteger ToBigInteger(this string val)
    {
        if (!BigInteger.TryParse(val, out var bi)) return BigInteger.Zero;
        return bi;
    }
    
    public static long ToLong(this string val)
    {
        if (!long.TryParse(val, out var bi)) return 0;
        return bi;
    }
}