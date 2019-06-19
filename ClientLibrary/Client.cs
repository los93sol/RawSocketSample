using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClientLibrary
{
    public class Client : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly ClientOptions _clientOptions;
        private readonly Socket _socket;

        public Client(ILogger<Client> logger, IOptionsMonitor<ClientOptions> clientOptions)
        {
            _logger = logger;
            _clientOptions = clientOptions.CurrentValue;
            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var hostAddresses = Dns.GetHostAddresses(_clientOptions.ServerHostname);

            if (hostAddresses.Length == 0)
            {
                _logger.LogInformation("Unable to resolve socket server, exiting");
                return;
            }

            _logger.LogInformation($"Connecting to {hostAddresses[0]} on port {_clientOptions.ServerPort}");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _socket.Connect(hostAddresses[0], _clientOptions.ServerPort);
                    break;
                }
                catch
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                }
            }

            var sendBuffer = Encoding.ASCII.GetBytes(_clientOptions.Message);
            var receiveBuffer = new byte[75];

            while (!stoppingToken.IsCancellationRequested)
            {
                await _socket.SendAsync(new ArraySegment<byte>(sendBuffer, 0, sendBuffer.Length), SocketFlags.None);

                var bytesRead = await _socket.ReceiveAsync(receiveBuffer, SocketFlags.None, stoppingToken);

                if (bytesRead > 0)
                {
                    _logger.LogTrace(Encoding.ASCII.GetString(receiveBuffer.AsSpan().Slice(0, bytesRead)));
                }
                else
                {
                    break;
                }

                await Task.Delay(_clientOptions.Delay);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            if (_socket != null)
            {
                _socket.Close();
            }

            return Task.CompletedTask;
        }
    }
}