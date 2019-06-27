using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PacketDotNet;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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
        private readonly Ring _ring;

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
                tp_block_nr = 16,
                tp_frame_nr = 32
            };

            if (_socket.SetRxRing(tp3) != 0)
            {
                _logger.LogError("Unable to set RX ring");
            }
            else
            {
                var bufferAddress = MMap.Create(
                    IntPtr.Zero,
                    tp3.tp_block_size * tp3.tp_block_nr,//4096,//4096 * 4,
                    MMap.MemoryMappedProtections.PROT_READ | MMap.MemoryMappedProtections.PROT_WRITE,
                    MMap.MemoryMappedFlags.MAP_SHARED, // | MMap.MemoryMappedFlags.MAP_LOCKED | MMap.MemoryMappedFlags.MAP_NORESERVE,
                    _socket.Handle,
                    0);

                if (!bufferAddress.HasValue)
                {
                    _logger.LogError("Unable to mmap RX ring");
                }
                else
                {
                    _ring = new Ring(bufferAddress.Value, tp3);
                }
            }

            if (_socket.SetFanout(_captureGroup) != 0)
            {
                _logger.LogError("Unable to set fanout");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //var buffer = new byte[1500];

            //while (!stoppingToken.IsCancellationRequested)
            //{
                if (_ring != null)
                {
                    _ = Task.Run(() =>
                    {
                        var totalPackets = 0;

                        var currentBlock = 0;
                        //var previousBlockSequenceNumber = (ulong)0;

                        var TP_STATUS_USER = 1;

                        while (!stoppingToken.IsCancellationRequested)
                        {
                            var blockDescriptionPtr = _ring.Blocks[currentBlock].Offset;

                            var block_status = 0;
                            var offset_to_priv = 0;
                            var number_of_packets = 0;

                            while ((block_status & TP_STATUS_USER) == 0)
                            {
                                _socket.Poll(_ring.BufferAddress, 500);

                                unsafe
                                {
                                    byte* blockDescription = (byte*)blockDescriptionPtr;

                                    Span<byte> bytes = new byte[12];

                                    for (var i = 0; i < bytes.Length; i++)
                                    {
                                        bytes[i] = blockDescription[i + 4];
                                    }

                                    offset_to_priv = BitConverter.ToInt32(bytes.Slice(0, 4));   // NOTE: This is supposed to be uint
                                    block_status = BitConverter.ToInt32(bytes.Slice(4, 4));
                                    number_of_packets = BitConverter.ToInt32(bytes.Slice(8, 4));
                                }
                            }

                            //_logger.LogInformation($"{number_of_packets} packets available in block {currentBlock} on {_networkInterface.Name}, group {_captureGroup}, thread {_thread}");

                            // At this point we have a completed block description, iterate the tpackets...
                            var tpacket3Ptr = IntPtr.Add(blockDescriptionPtr, offset_to_priv);

                            for (var i = 0; i < number_of_packets; i++)
                            {
                                unsafe
                                {
                                    byte* tpacket3 = (byte*)tpacket3Ptr;

                                    Span<byte> bytes = new byte[28];

                                    for (var b = 0; b < bytes.Length; b++)
                                    {
                                        bytes[b] = tpacket3[b];
                                    }

                                    var tp_next_offset = BitConverter.ToInt32(bytes.Slice(0, 4)); // TODO: This is supposed to be uint
                                    var tp_sec = BitConverter.ToUInt32(bytes.Slice(4, 4));
                                    var tp_nsec = BitConverter.ToUInt32(bytes.Slice(8, 4));
                                    var tp_snaplen = BitConverter.ToUInt32(bytes.Slice(12, 4));
                                    var tp_len = BitConverter.ToUInt32(bytes.Slice(16, 4));
                                    var tp_status = BitConverter.ToUInt32(bytes.Slice(20, 4));
                                    var tp_mac = BitConverter.ToUInt16(bytes.Slice(24, 2));
                                    var tp_net = BitConverter.ToUInt16(bytes.Slice(26, 2));

                                    var dataBytes = new byte[tp_snaplen];

                                    for (var b = 0; b < dataBytes.Length; b++)
                                    {
                                        dataBytes[b] = tpacket3[b + tp_mac];
                                    }

                                    var ethernetPacket = Packet.ParsePacket(LinkLayers.Ethernet, dataBytes);

                                    if (ethernetPacket.PayloadPacket?.GetType() == typeof(IPv4Packet))
                                    {
                                        var ipPacket = (IPv4Packet)ethernetPacket.PayloadPacket;

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

                                    tpacket3Ptr = IntPtr.Add(tpacket3Ptr, tp_next_offset);
                                    totalPackets++;
                                }
                            }

                            // Release the block back to the kernel
                            unsafe
                            {
                                byte* blockDescription = (byte*)blockDescriptionPtr;

                                for (var i = 0; i < _ring.Blocks[currentBlock].Length; i++)
                                {
                                    blockDescription[i] = 0;
                                }
                            }

                            _logger.LogInformation($"Total Packets Captured: {totalPackets}");

                            currentBlock = (currentBlock + 1) % _ring.Blocks.Count;
                        }
                    });
                }

                //var bytesRead = await _socket.ReceiveAsync(buffer, SocketFlags.None);

                //if (bytesRead > 0)
                //{
                //    Packet ethernetPacket = null;
                //    IPv4Packet ipPacket = null;

                //    ethernetPacket = Packet.ParsePacket(LinkLayers.Ethernet, buffer);

                //    if (ethernetPacket.PayloadPacket.GetType() == typeof(IPv4Packet))
                //    {
                //        ipPacket = (IPv4Packet)ethernetPacket.PayloadPacket;

                //        if (ipPacket.Protocol == PacketDotNet.ProtocolType.Tcp)
                //        {
                //            var tcpPacket = (TcpPacket)ipPacket.PayloadPacket;

                //            var source = $"{ipPacket.SourceAddress}:{tcpPacket.SourcePort}";
                //            var destination = $"{ipPacket.DestinationAddress}:{tcpPacket.DestinationPort}";

                //            if (tcpPacket.SourcePort == 8087 || tcpPacket.DestinationPort == 8087)
                //            {
                //                if (tcpPacket.PayloadData.Length > 0)
                //                {
                //                    _logger.LogInformation($"{_networkInterface.Name} Thread: {_thread} Group: {_captureGroup} Source: {source} Destination: {destination}, Ack: {tcpPacket.AcknowledgmentNumber}, Seq: {tcpPacket.SequenceNumber} {Encoding.ASCII.GetString(tcpPacket.PayloadData)}");
                //                }
                //            }
                //            else
                //            {
                //                _logger.LogInformation($"{_networkInterface.Name} Thread: {_thread} Group: {_captureGroup} Source: {source} Destination: {destination}, Received packet on wrong port");
                //            }
                //        }
                //        else
                //        {
                //            _logger.LogInformation($"{_networkInterface.Name} Thread: {_thread} Group: {_captureGroup} Received {ipPacket.Protocol} packet, discarding...");
                //        }
                //    }
                //}
            //}
        }
    }
}