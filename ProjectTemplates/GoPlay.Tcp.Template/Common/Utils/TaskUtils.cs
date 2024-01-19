namespace GoPlayProj.Utils;

public static class TaskUtils
{
    public static async void RunTaskInterval(TimeSpan intervalTime, Func<Task> func, Action<Exception> onErr, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                await func();
                var ts = DateTime.UtcNow.Subtract(startTime);
                var gap = intervalTime - ts;
                if (gap.TotalMilliseconds > 0) await Task.Delay(gap, token);
            }
            catch (OperationCanceledException)
            {
                /* DO NOTHING */
            }
            catch (Exception err)
            {
                onErr(err);
            }
        }
    }
}