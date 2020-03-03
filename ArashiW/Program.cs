using System;
using System.ComponentModel;
using System.Net;
using ARSoft.Tools.Net.Dns;
using Fleck;

namespace ArashiW
{
    class Program
    {
        static void Main(string[] args)
        {
            DNSEncoder.Init();
            var server = new WebSocketServer("ws://0.0.0.0:3030");
            server.ListenerSocket.NoDelay = true;
            server.Start(ws =>
            {
                ws.OnOpen = () => Console.WriteLine(ws.ConnectionInfo.ClientIpAddress + ":Open!");
                ws.OnClose = () => Console.WriteLine(ws.ConnectionInfo.ClientIpAddress + ":Close!");
                ws.OnBinary = msg =>
                {
                    using (var bgWorker = new BackgroundWorker())
                    {
                        bgWorker.DoWork += (sender, eventArgs) =>
                        {
                            var dnsQMsg = DnsMessage.Parse(msg);
                            var dnsRMsg =
                                new DnsClient(IPAddress.Parse("8.8.8.8"), 1000).SendMessage(dnsQMsg);
                            ws.Send(DNSEncoder.Encode(dnsRMsg));

                            Console.ForegroundColor = ConsoleColor.Cyan;
                            dnsQMsg.Questions.ForEach(o => Console.WriteLine("Qu:" + o));
                            Console.ForegroundColor = ConsoleColor.Green;
                            dnsRMsg.AnswerRecords.ForEach(o => Console.WriteLine("An:" + o));
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            dnsRMsg.AuthorityRecords.ForEach(o => Console.WriteLine("Au:" + o));
                            Console.ForegroundColor = ConsoleColor.Green;
                        };
                        bgWorker.RunWorkerAsync();
                    }
                };
            });

            Console.ReadLine();
        }
    }
}
