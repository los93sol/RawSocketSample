using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace RawSocketSample
{

    internal static class SocketExtensions
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct sock_filter
        {
            public ushort code;
            public byte jt;
            public byte jf;
            public int k;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct sock_fprog
        {
            public ushort len;
            public unsafe sock_filter* filter;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct tpacket_req3
        {
            /// <summary>
            /// Minimal size of contiguous block
            /// </summary>
            public uint tp_block_size;

            /// <summary>
            /// Number of blocks
            /// </summary>
            public uint tp_block_nr;

            /// <summary>
            /// Size of frame
            /// </summary>
            public uint tp_frame_size;

            /// <summary>
            /// Total number of frames
            /// </summary>
            public uint tp_frame_nr;

            /// <summary>
            /// timeout in msecs
            /// </summary>
            public uint tp_retire_blk_tov;

            /// <summary>
            /// offset to private data area
            /// </summary>
            public uint tp_sizeof_priv;
            public uint tp_feature_req_word;
        }

        private static class SocketOptionLevels
        {
            public const int SOL_SOCKET = 1;
            public const int SOL_PACKET = 263;
        }

        private static class SocketOptions
        {
            // SOL_SOCKET Options
            public const int SO_ATTACH_FILTER = 26;
            //public const int SO_DETACH_FILTER = 27;
            //public const int SO_LOCK_FILTER = 44;

            // SOL_PACKET Options
            public const int PACKET_FANOUT = 18;

            public const int PACKET_VERSION = 10;

            public const int PACKET_RX_RING = 5;
        }

        public const int PACKET_FANOUT_HASH = 0;

        public enum PacketVersions
        {
            TPACKET_V2 = 1,
            TPACKET_V3 = 2
        }

        public unsafe static int SetFilter(this Socket socket)
        {
            // Hardcoded for now until I can figure out how we can have some sort of API surface to generate the pseudo asm on demand
            // tcp port 8087
            var filters = new sock_filter[]
            {
                new sock_filter { code = 0x28, jt = 0, jf = 0, k = 0x0000000c },
                new sock_filter { code = 0x15, jt = 0, jf = 6, k = 0x000086dd },
                new sock_filter { code = 0x30, jt = 0, jf = 0, k = 0x00000014 },
                new sock_filter { code = 0x15, jt = 0, jf = 15, k = 0x00000006 },
                new sock_filter { code = 0x28, jt = 0, jf = 0, k = 0x00000036 },
                new sock_filter { code = 0x15, jt = 12, jf = 0, k = 0x00001f97 },
                new sock_filter { code = 0x28, jt = 0, jf = 0, k = 0x00000038 },
                new sock_filter { code = 0x15, jt = 10, jf = 11, k = 0x00001f97 },
                new sock_filter { code = 0x15, jt = 0, jf = 10, k = 0x00000800 },
                new sock_filter { code = 0x30, jt = 0, jf = 0, k = 0x00000017 },
                new sock_filter { code = 0x15, jt = 0, jf = 8, k = 0x00000006 },
                new sock_filter { code = 0x28, jt = 0, jf = 0, k = 0x00000014 },
                new sock_filter { code = 0x45, jt = 6, jf = 0, k = 0x00001fff },
                new sock_filter { code = 0xb1, jt = 0, jf = 0, k = 0x0000000e },
                new sock_filter { code = 0x48, jt = 0, jf = 0, k = 0x0000000e },
                new sock_filter { code = 0x15, jt = 2, jf = 0, k = 0x00001f97 },
                new sock_filter { code = 0x48, jt = 0, jf = 0, k = 0x00000010 },
                new sock_filter { code = 0x15, jt = 0, jf = 1, k = 0x00001f97 },
                new sock_filter { code = 0x6, jt = 0, jf = 0, k = 0x00040000 },
                new sock_filter { code = 0x6, jt = 0, jf = 0, k = 0x00000000 }
            };

            sock_fprog bpf;
            bpf.len = (ushort)filters.Length;

            fixed (sock_filter* f = filters)
            {
                bpf.filter = f;
            }

            var length = Marshal.SizeOf(bpf);

            var result = setsockopt((int)socket.Handle, SocketOptionLevels.SOL_SOCKET, SocketOptions.SO_ATTACH_FILTER, &bpf, length);
            return ValidateResult(result);
        }

        public unsafe static int SetFanout(this Socket socket, int group)
        {
            var fanout = (group & 0xffff) | (PACKET_FANOUT_HASH << 16);
            var result = setsockopt((int)socket.Handle, SocketOptionLevels.SOL_PACKET, SocketOptions.PACKET_FANOUT, &fanout, sizeof(int));
            return ValidateResult(result);
        }

        public unsafe static int SetPacketVersion(this Socket socket, PacketVersions version)
        {
            var v = (int)version;
            var result = setsockopt((int)socket.Handle, SocketOptionLevels.SOL_PACKET, SocketOptions.PACKET_VERSION, &v, sizeof(int));
            return ValidateResult(result);
        }

        public unsafe static int SetRxRing(this Socket socket, tpacket_req3 request)
        {
            var result = setsockopt((int)socket.Handle, SocketOptionLevels.SOL_PACKET, SocketOptions.PACKET_RX_RING, &request, sizeof(tpacket_req3));
            return ValidateResult(result);
        }

        private static int ValidateResult(int result)
        {
            if (result != 0)
            {
                return Marshal.GetLastWin32Error();
            }

            return result;
        }

        [DllImport("libc", SetLastError = true)]
        private static unsafe extern int setsockopt(int sockfd, int level, int optname, void* optval, int optlen);
    }
}