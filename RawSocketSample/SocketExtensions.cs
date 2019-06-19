using System.Net.Sockets;
using System.Runtime.InteropServices;

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

        // SOL_SOCKET Options
        private const int SO_ATTACH_FILTER = 26;
        private const int SO_DETACH_FILTER = 27;
        private const int SO_LOCK_FILTER = 44;

        private const int SOL_PACKET = 263;

        // SOL_PACKET Options
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
}