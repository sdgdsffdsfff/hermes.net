using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;

namespace TestServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 4376);
            listener.Start();
            var socket = listener.AcceptSocket();
            var buffer = new byte[1024];
            int i = socket.Receive(buffer);
            var array = new byte[i];
            Array.Copy(buffer, 0, array, 0, i);
            StringBuilder sb = new StringBuilder();
            foreach (var b in array)
            {
                sb.Append(b.ToString() + "|");
            }

            string s = string.Empty;
            Console.ReadLine();
        }
    }
}
