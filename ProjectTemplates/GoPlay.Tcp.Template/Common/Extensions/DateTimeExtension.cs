using Google.Protobuf.WellKnownTypes;

namespace GoPlayProj.Extensions;

public static class DateTimeExtension
{
    public static DateTime Today(this DateTime dt)
    {
        return dt.Date;
    }
    
    public static DateTime Yesterday(this DateTime dt)
    {
        var today = dt.Today();
        return today.AddDays(-1);
    }
    
    public static bool IsUtcToday(this DateTime dt)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        var ts = dt.Subtract(today);
        var td = ts.TotalDays;
        return td >= 0 && td < 1;
    }

    public static bool IsBefore(this DateTime dt, DateTime cmp, int offset=0)
    {
        var ts = dt.Subtract(cmp);
        return ts.TotalSeconds + offset < 0;
    }
    
    public static bool IsBeforeTime(this DateTime dt, DateTime cmp, int offset=0)
    {
        cmp = new DateTime(dt.Year, dt.Month, dt.Day, cmp.Hour, cmp.Minute, cmp.Second);
        var ts = dt.Subtract(cmp);
        return ts.TotalSeconds + offset < 0;
    }
    
    public static bool IsBeforeUtcToday(this DateTime dt, int offset=0)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        
        var ts = dt.Subtract(today);
        return ts.TotalSeconds + offset < 0;
    }

    public static bool IsBeforeUtcThisWeek(this DateTime dt, int offset=0)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var dayOfWeek = today.DayOfWeek.ToInt();
        var day = today.AddDays(-dayOfWeek);
        
        var ts = dt.Subtract(day);
        return ts.TotalSeconds + offset < 0;
    }

    public static int ToInt(this DayOfWeek dayOfWeek)
    {
        switch (dayOfWeek)
        {
            case DayOfWeek.Sunday:
                return 6;
            case DayOfWeek.Monday:
            case DayOfWeek.Tuesday:
            case DayOfWeek.Wednesday:
            case DayOfWeek.Thursday:
            case DayOfWeek.Friday:
            case DayOfWeek.Saturday:
                return (int)dayOfWeek - 1;
            default:
                throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, null);
        }
    }

    public static bool IsBeforeUtcThisMonth(this DateTime dt, int offset=0)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var day = today.AddDays(1 - today.Day);
        
        var ts = dt.Subtract(day);
        return ts.TotalSeconds + offset < 0;
    }
    
    public static Timestamp AsTimestamp(this DateTime dt)
    {
        return Timestamp.FromDateTime(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
    }
    
    public static int ToUnixTime(this DateTime dt)
    {
        return (int)dt.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
    }
}