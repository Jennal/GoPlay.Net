using System.Numerics;

namespace GoPlayProj.Extensions;

public static class BigIntegerExtensions
{
    public static long ToLong(this BigInteger bi)
    {
        return unchecked((long)(ulong)(bi & ulong.MaxValue));
    }
    
    public static ulong ToULong(this BigInteger bi)
    {
        return (ulong)(bi & ulong.MaxValue);
    }

    public static BigInteger ToBigInteger(this long val)
    {
        return new BigInteger(val);
    }
    
    public static BigInteger ToBigInteger(this ulong val)
    {
        return new BigInteger(val);
    }
}