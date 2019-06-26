using System;
using System.Runtime.InteropServices;

namespace RawSocketSample
{
    internal static class MMap
    {
        [Flags]
        public enum MemoryMappedProtections
        {
            //PROT_NONE = 0,
            PROT_READ = 1,
            PROT_WRITE = 2,
            //PROT_EXEC = 4
        }

        [Flags]
        public enum MemoryMappedFlags
        {
            MAP_SHARED = 1,
            //MAP_PRIVATE = 2,
            //MAP_ANONYMOUS = 32,
            MAP_LOCKED = 8192,
            MAP_NORESERVE = 16384
        }

        private const int MAP_FAILED = -1;

        public static unsafe IntPtr? Create(IntPtr addr, uint length, MemoryMappedProtections protections, MemoryMappedFlags flags, IntPtr fd, int offset)
        {
            var result = mmap(addr, length, (int)protections, (int)flags, (int)fd, offset);

            if ((long)result == MAP_FAILED)
            {
                return null;
            }

            return result;
        }

        [DllImport("libc", SetLastError = true)]
        private static unsafe extern IntPtr mmap(IntPtr addr, uint length, int prot, int flags, int fd, int offset);
    }
}