using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
            var dict = new Dictionary<BackgroundWorker,ClientWebSocket>();

            using (DnsServer dnsServer = new DnsServer(IPAddress.Any, 10, 10))
            {
                DNSEncoder.Init();

                for (var i = 0; i < 3; i++) dict.Add(new BackgroundWorker(), new ClientWebSocket());
                foreach (var item in dict)
                    item.Value.ConnectAsync(new Uri("ws://127.0.0.1:3030"), CancellationToken.None).Wait();

                async Task OnDnsServerOnQueryReceived(object sender, QueryReceivedEventArgs e)
                {
                    if (!(e.Query is DnsMessage query)) return;
                    await Task.Run(() =>
                    {
                        try
                        {
                            foreach (var item in dict.Where(item => !item.Key.IsBusy))
                            {
                                var worker = item.Key;
                                worker.DoWork += (o, eventArgs) =>
                                {
                                    var ws = item.Value;
                                    if (ws.State == WebSocketState.Aborted)
                                    {
                                        ws.Dispose();
                                        ws = new ClientWebSocket();
                                        ws.ConnectAsync(new Uri("ws://127.0.0.1:3030"), CancellationToken.None)
                                            .Wait();
                                    }

                                    if (ws.State == WebSocketState.Closed)
                                        ws.ConnectAsync(new Uri("ws://127.0.0.1:3030"), CancellationToken.None)
                                            .Wait();

                                    ws.SendAsync(new ArraySegment<byte>(DNSEncoder.Encode(query)),
                                        WebSocketMessageType.Binary,
                                        true, CancellationToken.None).Wait();
                                    var dnsBytes = new byte[500];
                                    ws.ReceiveAsync(new ArraySegment<byte>(dnsBytes), CancellationToken.None)
                                        .Wait();
                                    var dnsRMsg = DnsMessage.Parse(dnsBytes);
                                    e.Response = dnsRMsg;

                                    using (var bgw = new BackgroundWorker())
                                    {
                                        bgw.DoWork += (i, a) =>
                                        {
                                            dnsRMsg.AnswerRecords.ForEach(Console.WriteLine);
                                            dnsRMsg.AuthorityRecords.ForEach(Console.WriteLine);
                                        };
                                        bgw.RunWorkerAsync();
                                    }
                                };
                                item.Key.RunWorkerAsync();

                                while (true)
                                {
                                    if (item.Key.IsBusy) continue;
                                    item.Key.Dispose();
                                    dict.Remove(item.Key);
                                    dict.Add(new BackgroundWorker(), item.Value);
                                    break;
                                }
                                break;
                            }
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception);
                            throw;
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
                foreach (var item in dict)
                    item.Value.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, CancellationToken.None).Wait();
            }
        }
    }
}
