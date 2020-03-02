using System;
using System.Net;
using System.Net.WebSockets;
using ARSoft.Tools.Net.Dns;
using Fleck;

namespace ArashiW
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new WebSocketServer("ws://0.0.0.0:3030");
            server.Start(ws =>
            {
                ws.OnOpen = () => Console.WriteLine(ws.ConnectionInfo.ClientIpAddress + " Open!");
                ws.OnClose = () => Console.WriteLine(ws.ConnectionInfo.ClientIpAddress + " Close!");
                ws.OnBinary = msg =>
                {
                    var dnsQMsg = DnsMessage.Parse(msg);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    dnsQMsg.Questions.ForEach(o => Console.WriteLine("Qu:" + o));
                    var dnsMsg =
                        new DnsClient(IPAddress.Parse("8.8.8.8"), 1000).SendMessage(dnsQMsg);
                    Console.ForegroundColor = ConsoleColor.Green;
                    dnsMsg.AnswerRecords.ForEach(o => Console.WriteLine("An:" + o));
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    dnsMsg.AuthorityRecords.ForEach(o => Console.WriteLine("Au:" + o));
                    Console.ForegroundColor = ConsoleColor.Green;
                };
            });

            Console.ReadLine();
        }
    }
}
