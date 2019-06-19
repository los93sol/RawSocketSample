using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace RawSocketSample
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

    internal static class SocketExtensions
    {
        private const int SOL_SOCKET = 1;

        // SOL_SOCKET Options
        private const int SO_ATTACH_FILTER = 26;
        //private const int SO_DETACH_FILTER = 27;
        //private const int SO_LOCK_FILTER = 44;

        private const int SOL_PACKET = 263;

        // SOL_PACKET Options
        private const int PACKET_FANOUT = 18;
        private const int PACKET_FANOUT_HASH = 0;

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

            var result = setsockopt((int)socket.Handle, SOL_SOCKET, SO_ATTACH_FILTER, &bpf, length);
            return ValidateResult(result);
        }

        public unsafe static int SetFanout(this Socket socket, int group)
        {
            var fanout = (group & 0xffff) | (PACKET_FANOUT_HASH << 16);
            var result = setsockopt((int)socket.Handle, SOL_PACKET, PACKET_FANOUT, &fanout, sizeof(int));
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
        static unsafe extern int setsockopt(int sockfd, int level, int optname, void* optval, int optlen);
    }
}