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
    public class JunkClient : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly ClientOptions _clientOptions;
        private readonly Socket _socket;

        public JunkClient(ILogger<JunkClient> logger, IOptionsMonitor<ClientOptions> clientOptions)
        {
            _logger = logger;
            _clientOptions = clientOptions.CurrentValue;
            _socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var hostAddresses = Dns.GetHostAddresses(_clientOptions.ServerHostname);

            if (hostAddresses.Length == 0)
            {
                _logger.LogInformation("Unable to resolve socket server, exiting");
                return;
            }

            var remoteEndpoint = new IPEndPoint(hostAddresses[0], _clientOptions.ServerPort);
            var sendBuffer = Encoding.ASCII.GetBytes(_clientOptions.Message);

            while (!stoppingToken.IsCancellationRequested)
            {
                await _socket.SendToAsync(new ArraySegment<byte>(sendBuffer, 0, sendBuffer.Length), SocketFlags.None, remoteEndpoint);
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