using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using ARSoft.Tools.Net.Dns;

namespace ArashiDNS
{
    class Program
    {
        static void Main(string[] args)
        {
            using var dnsServer = new DnsServer(IPAddress.Any, 10, 10);
            DNSEncoder.Init();

            var wsPool = new ObjectPool<ClientWebSocket>(() =>
            {
                var ws = new ClientWebSocket();
                ws.ConnectAsync(new Uri("ws://127.0.0.1:22332"), CancellationToken.None).Wait();
                return ws;
            });

            for (var i = 0; i < 3; i++)
            {
                wsPool.Return(wsPool.Get());
            }

            async Task OnDnsServerOnQueryReceived(object sender, QueryReceivedEventArgs e)
            {
                try
                {
                    if (e.Query is not DnsMessage query) return;
                    var ws = wsPool.Get();
                    switch (ws.State)
                    {
                        case WebSocketState.Aborted:
                            ws.Dispose();
                            ws = new ClientWebSocket();
                            ws.ConnectAsync(new Uri("ws://127.0.0.1:3030"), CancellationToken.None)
                                .Wait();
                            break;
                        case WebSocketState.None:
                        case WebSocketState.Closed:
                            ws.ConnectAsync(new Uri("ws://127.0.0.1:3030"), CancellationToken.None)
                                .Wait();
                            break;
                    }

                    var dnsBytes = new byte[512];

                    await ws.SendAsync(new ArraySegment<byte>(DNSEncoder.Encode(query)),
                        WebSocketMessageType.Binary, true, CancellationToken.None);
                    await ws.ReceiveAsync(new ArraySegment<byte>(dnsBytes), CancellationToken.None);
                    wsPool.Return(ws);

                    var dnsRMsg = DnsMessage.Parse(dnsBytes);
                    Task.Run(() =>
                    {
                        dnsRMsg.AnswerRecords.ForEach(Console.WriteLine);
                        dnsRMsg.AuthorityRecords.ForEach(Console.WriteLine);
                    });
                    e.Response = dnsRMsg;
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }

            dnsServer.QueryReceived += OnDnsServerOnQueryReceived;
            dnsServer.Start();
            Console.WriteLine(@"-------ARASHI   DNS------");
            Console.WriteLine(@"-------DOTNET ALPHA------");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(@"ArashiDNS Server Running");

            Console.WriteLine(@"Press any key to stop dns server");
            Console.WriteLine("------------------------");
            Console.ReadLine();
            Console.WriteLine("------------------------");
        }

        public class ObjectPool<T>
        {
            private readonly ConcurrentBag<T> _objects;
            private readonly Func<T> _objectGenerator;

            public ObjectPool(Func<T> objectGenerator)
            {
                _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
                _objects = new ConcurrentBag<T>();
            }

            public T Get() => _objects.TryTake(out T item) ? item : _objectGenerator();

            public void Return(T item) => _objects.Add(item);
        }
    }
}
