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
            /*
            // from linux/if_packet.h
            struct sockaddr_ll {
                Int16 family;
                Int16 protocol;
                Int32 ifindex;
                Int32 pad1;
                Int32 pad2;
                Int32 pad3;
            }
            */

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
                socketAddress[3] = 3;   // ETH_P_ALL
                //socketAddress[10] = 4;  // PACKET_OUTGOING
            }

            return socketAddress;
        }
    }
}