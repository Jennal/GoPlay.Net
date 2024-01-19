using Google.Protobuf.WellKnownTypes;

namespace GoPlayProj.Extensions;

public static class IntExtensions
{
    public static Timestamp ToTimestamp(this int seconds)
    {
        return Timestamp.FromDateTimeOffset(DateTimeOffset.FromUnixTimeSeconds(seconds));
    }
}