namespace IronFoundry.Warden.PInvoke
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Win32.SafeHandles;

    public partial class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern SafeFileHandle CreateIoCompletionPort(IntPtr fileHandle, IntPtr existingCompletionPort, IntPtr completionKey, uint numberOfConcurrentThreads);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetQueuedCompletionStatus(SafeFileHandle handle, ref uint numberOfBytes, ref IntPtr completionKey, ref IntPtr overlapped, uint timeoutInMS);
    }
}
