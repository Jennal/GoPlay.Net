using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GoPlay.Core.Protocols;
using GoPlay.Core.Utils;

namespace GoPlay.Core.Transports.TCP
{
    public class TcpServer : TransportServerBase
    {
        protected TcpListener m_tcpListener;
        public ConcurrentDictionary<uint, System.Net.Sockets.TcpClient> m_clientDict = new ConcurrentDictionary<uint, System.Net.Sockets.TcpClient>();

        protected ConcurrentDictionary<uint, MemoryStream> m_readBufferDict = new ConcurrentDictionary<uint, MemoryStream>();

        protected BlockingCollection<(uint, byte[])> m_readChannel = new BlockingCollection<(uint, byte[])>(ushort.MaxValue);
        protected BlockingCollection<(uint, byte[])> m_writeChannel = new BlockingCollection<(uint, byte[])>(ushort.MaxValue);
        
        protected IdLoopGenerator m_idGen = new IdLoopGenerator(uint.MaxValue);

        protected CancellationTokenSource m_cancelSource;
        protected Task m_acceptTask;
        protected Task m_readTask;
        protected Task m_writeTask;

        public override void Start(string host, int port, CancellationTokenSource cancelSource=null)
        {
            if (m_tcpListener != null) Stop();

            var address = host == "*" ? IPAddress.Any : IPAddress.Parse(host);
            m_tcpListener = new TcpListener(address, port);
            m_tcpListener.Start();

            m_cancelSource = cancelSource ?? new CancellationTokenSource();
            m_acceptTask = TaskUtil.LongRun(AcceptLoop, m_cancelSource.Token);
            m_readTask   = TaskUtil.LongRun(ReadLoop, m_cancelSource.Token);
            m_writeTask  = TaskUtil.LongRun(WriteLoop, m_cancelSource.Token);
        }

        public override void Stop()
        {
            if (!m_cancelSource.Token.IsCancellationRequested) m_cancelSource.Cancel();
            Task.WaitAll(m_acceptTask, m_readTask, m_writeTask);

            m_tcpListener.Stop();
            m_tcpListener = null;
            m_clientDict.Clear();
            m_idGen.Reset();
        }

        private void AcceptLoop()
        {
            while (!m_cancelSource.Token.IsCancellationRequested)
            {
                try
                {
                    var task = Task.Run(m_tcpListener.AcceptTcpClientAsync, m_cancelSource.Token);
                    task.Wait(m_cancelSource.Token);
                    if (task.IsCanceled) return;
                    
                    var client = task.Result;
                    if (client.Connected == false) continue;

                    var clientId = m_idGen.Next();

                    client.ReceiveBufferSize = Consts.Buffer.ReadSize;
                    client.SendBufferSize = Consts.Buffer.WriteSize;
                    client.ReceiveTimeout = (int)Consts.TimeOut.Recv.TotalMilliseconds;
                    client.SendTimeout = (int)Consts.TimeOut.Send.TotalMilliseconds;
                    // client.NoDelay = true;

                    if (!m_clientDict.TryAdd(clientId, client))
                        throw new Exception($"TcpServer: Client count exceed: {m_clientDict.Count}");
                    if (!m_readBufferDict.TryAdd(clientId, new MemoryStream()))
                        throw new Exception($"TcpServer: Client count exceed: {m_readBufferDict.Count}");

                    InvokeOnClientConnected(clientId);
                }
                catch
                {
                    break;
                }
            }
        }

        private void ReadLoop()
        {
            while (!m_cancelSource.Token.IsCancellationRequested)
            {
                try
                {
                    if (!m_clientDict.Any())
                    {
                        Task.Delay(Consts.TimeOut.Server).Wait(m_cancelSource.Token);
                        continue;
                    }

                    var clientDict = m_clientDict.ToArray();
                    var reads = clientDict.Select(o => o.Value.Client).ToList();
                    // var writes = reads.ToList();
                    // var errors = reads.ToList();

                    Socket.Select(reads, null, null, (int)Consts.TimeOut.Server.TotalMilliseconds * 1000);
                    if (reads.Count <= 0) continue;

                    // Console.WriteLine($"reads={reads.Count}, writes={writes.Count}, errors={errors.Count}");
                    var taskList = new List<Task>();
                    foreach (var socket in reads)
                    {
                        var kv = clientDict.FirstOrDefault(o => o.Value.Client == socket);
                        var task = TaskUtil.Run(ReadFromClient, kv, m_cancelSource.Token);
                        taskList.Add(task);
                    }

                    Task.WaitAll(taskList.ToArray(), m_cancelSource.Token);
                }
                catch
                {
                    /* IGNORE ERR */
                }
            }
        }

        private void ReadFromClient(object state)
        {
            var kv = (KeyValuePair<uint, System.Net.Sockets.TcpClient>)state;
            var clientId = kv.Key;
            var socket = kv.Value.Client;
            try
            {
                if (!m_readBufferDict.TryGetValue(clientId, out var ms)) return;
                ms.Seek(0, SeekOrigin.End);

                var ns = kv.Value.GetStream();
                if (socket.Poll(1000, SelectMode.SelectRead) && !ns.DataAvailable)
                {
                    //发送心跳包，检查客户端是否还在
                    Send(clientId, Package.Create(0, PackageType.Ping, EncodingType.Protobuf).GetBytes());

                    //Socket.Select放行，却无有效数据，说明客户端可能已经断开连接
                    // DisconnectClient(clientId, null);
                    
                    return;
                }

                var buffer = new byte[Consts.Buffer.ReadSize];
                var len = ns.Read(buffer, 0, buffer.Length);
                ms.Write(buffer, 0, len);

                if (ms.Length <= sizeof(ushort)) return;
                ms.Seek(0, SeekOrigin.Begin);
                using (var br = new BinaryReader(ms))
                {
                    len = br.ReadUInt16();
                    if (ms.Length - ms.Position < len)
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        return;
                    }

                    var packBuffer = new byte[len];
                    ms.Read(packBuffer, 0, len);

                    var nms = new MemoryStream();
                    ms.CopyTo(nms);
                    if (!m_readBufferDict.TryUpdate(clientId, nms, ms)) return;
                        
                    m_readChannel.Add((clientId, packBuffer));
                }
            }
            catch (Exception err)
            {
                DisconnectClient(clientId, err);
            }
        }

        private void WriteLoop()
        {
            while (!m_cancelSource.Token.IsCancellationRequested)
            {
                var clientId = IdLoopGenerator.INVALID;
                try
                {
                    if (!m_writeChannel.TryTake(
                               out var data,
                               (int)Consts.TimeOut.Server.TotalMilliseconds,
                               m_cancelSource.Token)) continue;

                    clientId = data.Item1;
                    WriteToClient(data);
                }
                catch (TaskCanceledException)
                {
                    /* IGNORE */
                }
                catch (Exception err)
                {
                    DisconnectClient(clientId, err);
                }
            }
        }

        private void WriteToClient(object state)
        {
            var (clientId, data) = ((uint, byte[]))state;
            if (!m_clientDict.TryGetValue(clientId, out var client)) return;

            
                var ns = client.GetStream();
                using (var ms = new MemoryStream())
                {
                    using (var bw = new BinaryWriter(ms))
                    {
                        //write length
                        bw.Write((ushort)data.Length);
                        var len = ms.ToArray();
                        ns.Write(len, 0, len.Length);
                    }
                }

                //write bytes
                var start = 0;
                while (start < data.Length)
                {
                    var size = Math.Min(data.Length - start, client.SendBufferSize);
                    ns.Write(data, start, size);
                    start += size;
                }

                ns.Flush();
                // Console.ForegroundColor = ConsoleColor.Cyan;
                // var pack = Package.ParseRaw(data);
                // Console.WriteLine($"Send({clientId}): pack={pack}");
                // Console.ResetColor();
            
        }

        public override (uint, byte[]) Recv()
        {
            return m_readChannel.Take(m_cancelSource.Token);
        }

        public override void Send(uint clientId, byte[] data)
        {
            m_writeChannel.Add((clientId, data), m_cancelSource.Token);
        }

        public override string GetClientIp(uint clientId)
        {
            if (!m_clientDict.TryGetValue(clientId, out var client)) return string.Empty;
            
            var ep = client.Client.RemoteEndPoint as IPEndPoint;
            if (ep == null) return "unknown";
            return ep.Address.ToString();
        }

        public override bool IsOnline(uint clientId)
        {
            return m_clientDict.ContainsKey(clientId);
        }

        public override void DisconnectClient(uint clientId, Exception err)
        {
            if (!m_clientDict.TryRemove(clientId, out var client)) return;
            if (!m_readBufferDict.TryRemove(clientId, out _)) return;

            if (err != null) InvokeOnError(clientId, err);
            
            client.Close();
            InvokeOnClientDisconnected(clientId);
        }
    }
}