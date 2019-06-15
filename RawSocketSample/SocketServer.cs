using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RawSocketSample
{
    class SocketServer : BackgroundService
    {
        private readonly ILogger _logger;

        public SocketServer(ILogger<SocketServer> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(new IPEndPoint(IPAddress.Any, 8087));
            listenSocket.Listen(120);

            while (!stoppingToken.IsCancellationRequested)
            {
                var socket = await listenSocket.AcceptAsync();
                _ = ChitChat(socket, stoppingToken);
            }
        }

        private async Task ChitChat(Socket socket, CancellationToken stoppingToken)
        {
            var receiveBuffer = new byte[75];

            while (!stoppingToken.IsCancellationRequested)
            {
                var bytesRead = await socket.ReceiveAsync(receiveBuffer, SocketFlags.None, stoppingToken);


                if (bytesRead > 0)
                {
                    var received = Encoding.ASCII.GetString(receiveBuffer.AsSpan().Slice(0, bytesRead));
                    _logger.LogInformation(received);

                    var sendBuffer = Encoding.ASCII.GetBytes("Hi local client, I'm the server");

                    if (!received.ToLower().Contains("local"))
                    {
                        sendBuffer = Encoding.ASCII.GetBytes("Hi remote client, I'm the server");
                    }
                 
                    await socket.SendAsync(new ArraySegment<byte>(sendBuffer, 0, sendBuffer.Length), SocketFlags.None);
                }
                else
                {
                    break;
                }
            }
        }
    }
}