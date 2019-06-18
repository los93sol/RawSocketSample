using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace RawSocketSample
{
    class PacketCaptureManager : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private List<PacketCapture> _sniffers = new List<PacketCapture>();

        public PacketCaptureManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<PacketCaptureManager>>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var version = Environment.OSVersion.Version;
            var group = 1;
            var threads = 1;

            if (version.Major > 3 || (version.Major == 3 && version.Minor >= 1))
            {
                threads = 1;
                _logger.LogInformation($"Using packet fanout with {threads} per interface");
            }

            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces().Where(n => n.NetworkInterfaceType == NetworkInterfaceType.Loopback || n.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
            {
                for (var thread = 1; thread <= threads; thread++)
                {
                    var sniffer = ActivatorUtilities.CreateInstance<PacketCapture>(_serviceProvider, nic, group, thread);
                    _ = sniffer.StartAsync(stoppingToken);
                    _sniffers.Add(sniffer);
                }

                group++;
            }

            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var sniffer in _sniffers)
            {
                sniffer.StopAsync(cancellationToken);
            }

            return Task.CompletedTask;
        }
    }
}