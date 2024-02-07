using GoPlay.Core.Protocols;

namespace GoPlayProj;

public static partial class Consts
{
    public static class Id
    {
        public const uint INVALID_CLIENT_ID = IdLoopGenerator.INVALID;
        public const int INVALID_USER_ID = -1;
        public const int INVALID_ADMIN_ID = -1;
    }

    public static class DateTime
    {
        public static readonly System.DateTime Default = System.DateTime.Parse("2000-01-01");
    }
}