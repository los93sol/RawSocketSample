using System;
using System.Net;
using System.Net.Sockets;

namespace RawSocketSample
{
    class LLEndPoint : EndPoint
    {
        private int _ifIndex;

        public LLEndPoint(int interfaceIndex)
        {
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

            var a = new SocketAddress(AddressFamily.Packet, 20);
            byte[] asBytes = BitConverter.GetBytes(_ifIndex);
            a[4] = asBytes[0];
            a[5] = asBytes[1];
            a[6] = asBytes[2];
            a[7] = asBytes[3];

            return a;
        }
    }
}