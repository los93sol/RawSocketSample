using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PacketDotNet;
using System;
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
        private readonly NetworkInterface _networkInterface;
        private readonly int _captureGroup;
        private readonly int _thread;
        private readonly Socket _socket;
        private readonly IntPtr? _ring;

        public PacketCapture(ILogger<PacketCapture> logger, NetworkInterface networkInterface, int captureGroup, int thread)
        {
            _logger = logger;
            _networkInterface = networkInterface;
            _captureGroup = captureGroup;
            _thread = thread;

            short protocol = 0x0800; // IP
            _socket = new Socket(AddressFamily.Packet, SocketType.Raw, (System.Net.Sockets.ProtocolType)IPAddress.HostToNetworkOrder(protocol));
            _socket.Bind(new LLEndPoint(networkInterface));

            if (_socket.SetPacketVersion(SocketExtensions.PacketVersions.TPACKET_V3) != 0)
            {
                _logger.LogError("Unable to set packet version");
            }

            if (_socket.SetFilter() != 0)
            {
                _logger.LogError("Unable to set filter");
            }

            var tp3 = new SocketExtensions.tpacket_req3
            {
                tp_block_size = 4096,
                tp_frame_size = 2048,
                tp_block_nr = 4,
                tp_frame_nr = 8
            };

            //if (_socket.SetRxRing(tp3) != 0)
            //{
            //    _logger.LogError("Unable to set RX ring");
            //}
            //else
            //{
            //    _ring = MMap.Create(
            //        IntPtr.Zero,
            //        tp3.tp_block_size * tp3.tp_block_nr,//4096,//4096 * 4,
            //        MMap.MemoryMappedProtections.PROT_READ | MMap.MemoryMappedProtections.PROT_WRITE,
            //        MMap.MemoryMappedFlags.MAP_SHARED | MMap.MemoryMappedFlags.MAP_LOCKED | MMap.MemoryMappedFlags.MAP_NORESERVE,
            //        _socket.Handle,
            //        0);

            //    if (!_ring.HasValue)
            //    {
            //        _logger.LogError("Unable to mmap RX ring");
            //    }
            //}

            if (_socket.SetFanout(_captureGroup) != 0)
            {
                _logger.LogError("Unable to set fanout");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var buffer = new byte[1500];

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_ring.HasValue)
                {
                    // TODO: Poll the ring for data
                }

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

                            var source = $"{ipPacket.SourceAddress}:{tcpPacket.SourcePort}";
                            var destination = $"{ipPacket.DestinationAddress}:{tcpPacket.DestinationPort}";

                            if (tcpPacket.SourcePort == 8087 || tcpPacket.DestinationPort == 8087)
                            {
                                if (tcpPacket.PayloadData.Length > 0)
                                {
                                    _logger.LogInformation($"{_networkInterface.Name} Thread: {_thread} Group: {_captureGroup} Source: {source} Destination: {destination}, Ack: {tcpPacket.AcknowledgmentNumber}, Seq: {tcpPacket.SequenceNumber} {Encoding.ASCII.GetString(tcpPacket.PayloadData)}");
                                }
                            }
                            else
                            {
                                _logger.LogInformation($"{_networkInterface.Name} Thread: {_thread} Group: {_captureGroup} Source: {source} Destination: {destination}, Received packet on wrong port");
                            }
                        }
                        else
                        {
                            _logger.LogInformation($"{_networkInterface.Name} Thread: {_thread} Group: {_captureGroup} Received {ipPacket.Protocol} packet, discarding...");
                        }
                    }
                }
            }
        }
    }
}