using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using GoPlay.Services.Core;
using GoPlay.Services.Core.Protocols;
using GoPlay.Services.Core.Utils;
using GoPlay.Services.Exceptions;

namespace GoPlay.Services
{
    public partial class Client<T>
    {
        protected Task m_heartbeatTask;

        protected ConcurrentDictionary<uint, DateTime> m_pingDict = new ConcurrentDictionary<uint, DateTime>();
        protected int m_duration;

        protected int m_pingCount;
        protected TimeSpan m_pingAvg = TimeSpan.Zero;
        protected TimeSpan m_pingMax = TimeSpan.MinValue;
        protected TimeSpan m_pingMin = TimeSpan.MaxValue;

        public Task HeartbeatTask => m_heartbeatTask;
        public int PingCount => m_pingCount;
        public TimeSpan PingAvg => m_pingAvg;
        public TimeSpan PingMax => m_pingMax;
        public TimeSpan PingMin => m_pingMin;
        
        protected virtual void StartHeartbeat()
        {
            m_heartbeatTask = TaskUtil.LongRun(HeartbeatLoop, m_cancelSource.Token);
        }

        protected virtual void HeartbeatLoop()
        {
            while (!m_cancelSource.Token.IsCancellationRequested)
            {
                try
                {
                    var task = Task.Delay(Consts.HeartBeat.Update);
                    task.Wait(m_cancelSource.Token);

                    if (task.IsCanceled) return;
                    if (!IsConnected) continue;

                    m_duration -= (int)Consts.HeartBeat.Update.TotalMilliseconds;

                    //check time out
                    var isTimeOut = false;
                    var dict = m_pingDict.ToList();
                    foreach (var kv in dict)
                    {
                        var ts = DateTime.UtcNow.Subtract(kv.Value);
                        if (ts < Consts.HeartBeat.Timeout) continue;

                        isTimeOut = true;
                        ResetHeartBeatData();
                        OnErrorEvent(new HeartbeatTimeoutException());
                        DisconnectAsync().ConfigureAwait(false);
                        break;
                    }

                    if (isTimeOut) return;

                    if (m_duration > 0) continue;
                    m_duration = (int)m_handshake.HeartBeatInterval;

                    if (!m_pingDict.IsEmpty) continue;

                    //calculate ping timeout
                    var pack = Package.Create(0, PackageType.Ping, EncodingType);
                    m_pingDict.TryAdd(pack.Header.PackageInfo.Id, DateTime.UtcNow);

                    Send(pack);
                }
                catch (OperationCanceledException)
                {
                    //IGNORE ERR
                }
                catch (Exception err)
                {
                    OnErrorEvent(err);
                    DisconnectAsync().ConfigureAwait(false);
                }
            }
        }

        protected virtual void ResetHeartBeatData()
        {
            m_pingDict.Clear();
            
            m_duration = 0;
            m_pingCount = 0;
            
            m_pingAvg = TimeSpan.Zero;
            m_pingMax = TimeSpan.MinValue;
            m_pingMin = TimeSpan.MaxValue;
        }

        protected virtual void ResolvePing(Package pack)
        {
            var resp = new Package
            {
                Header = pack.Header.Clone(),
            };
            resp.Header.PackageInfo.Type = PackageType.Pong;
            Send(resp);
        }
        
        protected virtual void ResolvePong(Package pack)
        {
            if (!m_pingDict.TryRemove(pack.Header.PackageInfo.Id, out var dateTime)) return;

            m_pingCount++;
            var ts = DateTime.UtcNow.Subtract(dateTime);
            if (ts < m_pingMin) m_pingMin = ts;
            if (ts > m_pingMax) m_pingMax = ts;

            var avgTick = (double)(m_pingCount - 1)  / m_pingCount * m_pingAvg.Ticks;
            avgTick += (double)ts.Ticks / m_pingCount;
            m_pingAvg = TimeSpan.FromTicks((long)avgTick);
        }
        
        //TODO: remove test function
        public string NetworkStatus()
        {
            return $"Ping: avg={m_pingAvg.TotalMilliseconds} ms, max={m_pingMax.TotalMilliseconds} ms, min={m_pingMin.TotalMilliseconds} ms";
        }
    }
}