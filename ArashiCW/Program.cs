using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;

namespace ArashiCW
{
    class Program
    {
        static void Main(string[] args)
        {
            var ws = new ClientWebSocket();
            var msg = new DnsMessage();
            msg.Questions.Add(new DnsQuestion(DomainName.Parse("baidu.com"), RecordType.A, RecordClass.INet));
            DNSEncoder.Init();
            ws.ConnectAsync(new Uri("ws://127.0.0.1:3030"), CancellationToken.None).Wait();
            ws.SendAsync(new ArraySegment<byte>(DNSEncoder.Encode(msg)), WebSocketMessageType.Binary, true,
                CancellationToken.None).Wait();
            Console.WriteLine("Done!");
            Console.ReadLine();
        }
    }
}
