using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteClient
{
    class RemoteClient : BackgroundService
    {
        private readonly ILogger _logger;

        public RemoteClient(ILogger<RemoteClient> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Connecting to port 8087");
            var clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            var hostAddresses = Dns.GetHostAddresses("rawsocketsample");

            if (hostAddresses.Length == 0)
            {
                _logger.LogInformation("Unable to resolve socket server, exiting");
                return;
            }

            _logger.LogInformation($"Connecting to {hostAddresses[0]} on port 8087");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    clientSocket.Connect(hostAddresses[0], 8087);
                    break;
                }
                catch
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                }
            }

            var sendBuffer = Encoding.ASCII.GetBytes($"I'm the remote client");
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

                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }
}