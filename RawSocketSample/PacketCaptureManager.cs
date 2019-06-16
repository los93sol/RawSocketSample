using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RawSocketSample
{
    class PacketCaptureManager : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private List<PacketCapture> _sniffers = new List<PacketCapture>();

        public PacketCaptureManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback || nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    var indexProperty = nic.GetType().GetProperty("Index", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    var nicIndex = (int)indexProperty.GetValue(nic);

                    var sniffer = ActivatorUtilities.CreateInstance<PacketCapture>(_serviceProvider, nic, nicIndex);
                    _ = sniffer.StartAsync(stoppingToken);
                    _sniffers.Add(sniffer);
                }
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