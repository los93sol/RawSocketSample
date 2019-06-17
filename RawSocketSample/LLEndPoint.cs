﻿using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace RawSocketSample
{
    class LLEndPoint : EndPoint
    {
        private readonly NetworkInterface _networkInterface;
        private readonly int _ifIndex;

        public LLEndPoint(NetworkInterface networkInterface, int interfaceIndex)
        {
            _networkInterface = networkInterface;
            _ifIndex = interfaceIndex;
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
            var asBytes = BitConverter.GetBytes(_ifIndex);
            socketAddress[4] = asBytes[0];
            socketAddress[5] = asBytes[1];
            socketAddress[6] = asBytes[2];
            socketAddress[7] = asBytes[3];

            if (_networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            {
                socketAddress[3] = 3;   // ETH_P_ALL
                socketAddress[10] = 4;  // PACKET_OUTGOING
            }

            return socketAddress;
        }
    }
}