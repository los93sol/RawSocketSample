using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PacketDotNet;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RawSocketSample
{
    class PacketCapture : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly Socket _socket;

        public PacketCapture(ILogger<PacketCapture> logger, NetworkInterface networkInterface, int interfaceIndex)
        {
            _logger = logger;

            short protocol = 0x800; // IP
            _socket = new Socket(AddressFamily.Packet, SocketType.Raw, (System.Net.Sockets.ProtocolType)IPAddress.HostToNetworkOrder(protocol));
            _socket.Bind(new LLEndPoint(networkInterface, interfaceIndex));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var buffer = new byte[1500];

            while (!stoppingToken.IsCancellationRequested)
            {
                var bytesRead = await _socket.ReceiveAsync(buffer, SocketFlags.None);

                if (bytesRead > 0)
                {
                    Packet ethernetPacket = null;
                    IPv4Packet ipPacket = null;

                    ethernetPacket = Packet.ParsePacket(LinkLayers.Ethernet, buffer);

                    if (ethernetPacket.PayloadPacket.GetType() == typeof(IPv4Packet))
                    {
                        ipPacket = (IPv4Packet)ethernetPacket.PayloadPacket;

                        if (ipPacket.Protocol == PacketDotNet.ProtocolType.Tcp)
                        {
                            var tcpPacket = (TcpPacket)ipPacket.PayloadPacket;

                            if (tcpPacket.PayloadData.Length > 0 && (tcpPacket.SourcePort == 8087 || tcpPacket.DestinationPort == 8087))
                            {
                                var source = $"{ipPacket.SourceAddress}:{tcpPacket.SourcePort}";
                                var destination = $"{ipPacket.DestinationAddress}:{tcpPacket.DestinationPort}";

                                _logger.LogInformation($"{source} - {destination}, Ack: {tcpPacket.AcknowledgmentNumber}, Seq: {tcpPacket.SequenceNumber} {Encoding.ASCII.GetString(tcpPacket.PayloadData)}");
                            }
                        }
                        else
                        {
                            _logger.LogInformation($"Received {ipPacket.Protocol} packet, discarding...");
                        }
                    }
                }
            }
        }
    }
}