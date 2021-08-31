using System;
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
            using (DnsServer dnsServer = new DnsServer(IPAddress.Any, 10, 10))
            {
                DNSEncoder.Init();

                var ws = new ClientWebSocket();
                ws.ConnectAsync(new Uri("ws://127.0.0.1:3030"), CancellationToken.None).Wait();

                async Task OnDnsServerOnQueryReceived(object sender, QueryReceivedEventArgs e)
                {
                    await Task.Run(() => {
                        try
                        {
                            if (!(e.Query is DnsMessage query)) return;
                            if (ws.State == WebSocketState.Aborted)
                            {
                                ws.Dispose();
                                ws = new ClientWebSocket();
                                ws.ConnectAsync(new Uri("ws://127.0.0.1:3030"), CancellationToken.None)
                                    .Wait();
                            }
                            else if (ws.State == WebSocketState.Closed)
                                ws.ConnectAsync(new Uri("ws://127.0.0.1:3030"), CancellationToken.None)
                                    .Wait();

                            lock (ws)
                            {
                                ws.SendAsync(new ArraySegment<byte>(DNSEncoder.Encode(query)),
                                    WebSocketMessageType.Binary, true, CancellationToken.None).Wait();
                            }
                            var dnsBytes = new byte[500];
                            lock (ws)
                            {
                                ws.ReceiveAsync(new ArraySegment<byte>(dnsBytes), CancellationToken.None).Wait();
                            }
                            var dnsRMsg = DnsMessage.Parse(dnsBytes);
                            e.Response = dnsRMsg;
                            Task.Run(() =>
                            {
                                dnsRMsg.AnswerRecords.ForEach(Console.WriteLine);
                                dnsRMsg.AuthorityRecords.ForEach(Console.WriteLine);
                            });
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception);
                        }
                    });
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
        }
    }
}
