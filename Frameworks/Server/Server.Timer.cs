using GoPlay.Core;
using GoPlay.Core.Protocols;
using GoPlay.Interfaces;

namespace GoPlay
{
    public partial class Server<T>
    {
        protected void TimerLoop()
        {
            var token = m_cancelSource.Token;
            while (IsStarted && !token.IsCancellationRequested)
            {
                try
                {
                    var delay = Consts.TimeOut.Update;
                    foreach (var processor in m_processors)
                    {
                        //检查Update
                        if (processor is IUpdate)
                        {
                            var targetTime = processor.LastUpdate.Add(processor.UpdateDeltaTime);
                            if (targetTime <= DateTime.UtcNow)
                            {
                                processor.OnUpdateReceived();
                            }
                            else
                            {
                                var delta = targetTime - DateTime.UtcNow;
                                if (delta < delay) delay = delta;
                            }
                        }
                        
                        //检查DelayCall
                        foreach (var (execTime, action) in processor.DelayTasks)
                        {
                            if (execTime <= DateTime.UtcNow)
                            {
                                processor.OnDelayCallReceived(execTime, action);
                            }
                            else
                            {
                                var delta = execTime - DateTime.UtcNow;
                                if (delta < delay) delay = delta;
                            }
                        }
                    }
                    
                    Task.Delay(delay, token).Wait(token);
                }
                catch (OperationCanceledException)
                {
                    //IGNORE ERR
                }
                catch (AggregateException err)
                {
                    if (err.InnerException is OperationCanceledException) continue;
                    if (err.InnerException is TaskCanceledException) continue;
                    
                    OnErrorEvent(IdLoopGenerator.INVALID, err);
                }
                catch (Exception err)
                {
                    OnErrorEvent(IdLoopGenerator.INVALID, err);
                }
            }
        }
    }
}