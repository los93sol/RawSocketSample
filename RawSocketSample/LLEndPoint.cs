using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;

namespace RawSocketSample
{
    class LLEndPoint : EndPoint
    {
        private readonly NetworkInterface _networkInterface;

        public LLEndPoint(NetworkInterface networkInterface)
        {
            _networkInterface = networkInterface;
        }

        public override SocketAddress Serialize()
        {
            // Based on sockaddr_ll, check linux/if_packet.h for more information
            var socketAddress = new SocketAddress(AddressFamily.Packet, 20);

            var indexProperty = _networkInterface.GetType().GetProperty("Index", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var nicIndex = (int)indexProperty.GetValue(_networkInterface);
            var asBytes = BitConverter.GetBytes(nicIndex);

            socketAddress[4] = asBytes[0];
            socketAddress[5] = asBytes[1];
            socketAddress[6] = asBytes[2];
            socketAddress[7] = asBytes[3];

            if (_networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            {
                var eth_p_all = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)3));   // ETH_P_ALL
                socketAddress[2] = eth_p_all[0];
                socketAddress[3] = eth_p_all[1];
                //socketAddress[10] = 4;  // PACKET_OUTGOING
            }
            return socketAddress;
        }
    }
}