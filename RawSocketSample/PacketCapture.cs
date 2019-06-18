using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PacketDotNet;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RawSocketSample
{
    internal static class SocketExtensions
    {
        //[StructLayout(LayoutKind.Sequential)]
        //public struct sock_filter
        //{
        //    public ushort code;
        //    public byte jt;
        //    public byte jf;
        //    public int k;
        //}

        //[StructLayout(LayoutKind.Sequential)]
        //public struct sock_fprog
        //{
        //    public ushort len;
        //    public IntPtr filter;
        //}

        private const int SOL_SOCKET = 1;
        private const int SOL_PACKET = 263;

        private const int PACKET_FANOUT = 18;
        private const int PACKET_FANOUT_HASH = 0;

        public unsafe static int SetFanout(this Socket socket, int group)
        {
            var fanout = (group & 0xffff) | (PACKET_FANOUT_HASH << 16);
            var result = setsockopt((int)socket.Handle, SOL_PACKET, PACKET_FANOUT, &fanout, sizeof(int));

            if (result != 0)
            {
                return Marshal.GetLastWin32Error();
            }

            return result;
        }

        [DllImport("libc", SetLastError = true)]
        static unsafe extern int setsockopt(int sockfd, int level, int optname, void* optval, int optlen);
    }

    class PacketCapture : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly NetworkInterface _networkInterface;
        private readonly int _captureGroup;
        private readonly int _thread;
        private readonly Socket _socket;

        public PacketCapture(ILogger<PacketCapture> logger, NetworkInterface networkInterface, int captureGroup, int thread)
        {
            _logger = logger;
            _networkInterface = networkInterface;
            _captureGroup = captureGroup;
            _thread = thread;

            //short protocol = 0x0003;    // ETH_P_ALL
            short protocol = 0x0800; // IP
            _socket = new Socket(AddressFamily.Packet, SocketType.Raw, (System.Net.Sockets.ProtocolType)IPAddress.HostToNetworkOrder(protocol));
            _socket.Bind(new LLEndPoint(networkInterface));

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

                                _logger.LogInformation($"{_networkInterface.Name} Thread: {_thread} Group: {_captureGroup} Source: {source} Destination: {destination}, Ack: {tcpPacket.AcknowledgmentNumber}, Seq: {tcpPacket.SequenceNumber} {Encoding.ASCII.GetString(tcpPacket.PayloadData)}");
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