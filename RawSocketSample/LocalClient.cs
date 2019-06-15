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
    class LocalClient : BackgroundService
    {
        private readonly ILogger _logger;

        public LocalClient(ILogger<LocalClient> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            _logger.LogInformation("Connecting to localhost on port 8087");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    clientSocket.Connect(new IPEndPoint(IPAddress.Loopback, 8087));
                    break;
                }
                catch
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                }
            }

            var sendBuffer = Encoding.ASCII.GetBytes($"I'm the local client");
            var receiveBuffer = new byte[75];

            while (!stoppingToken.IsCancellationRequested)
            {
                await clientSocket.SendAsync(new ArraySegment<byte>(sendBuffer, 0, sendBuffer.Length), SocketFlags.None);

                var bytesRead = await clientSocket.ReceiveAsync(receiveBuffer, SocketFlags.None, stoppingToken);

                if (bytesRead > 0)
                {
                    _logger.LogInformation(Encoding.ASCII.GetString(receiveBuffer.AsSpan().Slice(0, bytesRead)));
                }
                else
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }
}