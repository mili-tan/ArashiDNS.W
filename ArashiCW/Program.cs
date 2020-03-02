using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArashiCW
{
    class Program
    {
        static void Main(string[] args)
        {
            var ws = new ClientWebSocket();
            ws.ConnectAsync(new Uri("ws://127.0.0.1:3030"), CancellationToken.None);
            Console.ReadLine();
        }
    }
}
