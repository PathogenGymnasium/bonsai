﻿using Bonsai.Osc.IO;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;

namespace Bonsai.Osc.Net
{
    class TcpTransport : ITransport
    {
        NetworkStream stream;
        readonly IObservable<Message> messageReceived;

        public TcpTransport(TcpClient client)
        {
            stream = client.GetStream();
            messageReceived = Observable.Using(
                () => new EventLoopScheduler(),
                scheduler => Observable.Create<Message>(observer =>
                {
                    var sizeBuffer = new byte[sizeof(int)];
                    var dispatcher = new Dispatcher(observer, scheduler);
                    return scheduler.Schedule(async recurse =>
                    {
                        try
                        {
                            var bytesToRead = sizeBuffer.Length;
                            while (bytesToRead > 0)
                            {
                                var bytesRead = await stream.ReadAsync(sizeBuffer, sizeBuffer.Length - bytesToRead, bytesToRead);
                                if (bytesRead == 0)
                                {
                                    observer.OnCompleted();
                                    return;
                                }

                                bytesToRead -= bytesRead;
                            }

                            var packetSize = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(sizeBuffer, 0));
                            var packet = new byte[packetSize];
                            bytesToRead = packet.Length;

                            while (bytesToRead > 0)
                            {
                                var bytesRead = await stream.ReadAsync(packet, packet.Length - bytesToRead, bytesToRead);
                                if (bytesRead == 0)
                                {
                                    observer.OnCompleted();
                                    return;
                                }

                                bytesToRead -= bytesRead;
                            }

                            dispatcher.Process(packet);
                            recurse();
                        }
                        catch (Exception e)
                        {
                            if (!client.Connected) observer.OnCompleted();
                            else observer.OnError(e);
                        }
                    });
                }))
                .PublishReconnectable()
                .RefCount();
        }

        public IObservable<Message> MessageReceived
        {
            get { return messageReceived; }
        }

        public void SendPacket(Action<BigEndianWriter> writePacket)
        {
            byte[] buffer;
            using (var memoryStream = new MemoryStream())
            using (var writer = new BigEndianWriter(memoryStream))
            {
                writePacket(writer);
                buffer = memoryStream.ToArray();
            }

            lock (stream)
            {
                using (var writer = new BigEndianWriter(stream, true))
                {
                    writer.Write(buffer.Length);
                    writer.Write(buffer);
                }
            }
        }

        private void Dispose(bool disposing)
        {
            var disposable = Interlocked.Exchange(ref stream, null);
            if (disposable != null && disposing)
            {
                disposable.Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
