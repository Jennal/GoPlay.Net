namespace GoPlayProj.Utils;

public static class TimeUtils
{
    public static TimeSpan GetTimeSpanToNextDay()
    {
        var now = DateTime.Now;
        var midnight = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59);
        var timeLeft = midnight - now;
        return timeLeft;
    }
}