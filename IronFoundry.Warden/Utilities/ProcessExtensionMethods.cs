namespace IronFoundry.Warden.Utilities
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Text;
    using PInvoke;

    public static class ProcessExtensionMethods
    {
        public static string GetUserName(this Process targetProcess)
        {
            string userName = String.Empty;

            var name = new StringBuilder();
            uint cchName = (uint)name.Capacity;

            StringBuilder referencedDomainName = new StringBuilder();
            uint cchReferencedDomainName = (uint)referencedDomainName.Capacity;

            NativeMethods.SidNameUse sidUse;
            byte[] sid = GetProcessSidBytes(targetProcess);
            if (sid == null)
            {
                userName = "unknown";
            }
            else
            {
                if (!NativeMethods.LookupAccountSid(null, sid, name, ref cchName, referencedDomainName, ref cchReferencedDomainName, out sidUse))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    if (errorCode == NativeMethods.Constants.ERROR_INSUFFICIENT_BUFFER)
                    {
                        name.EnsureCapacity((int)cchName);
                        referencedDomainName.EnsureCapacity((int)cchReferencedDomainName);
                        if (!NativeMethods.LookupAccountSid(null, sid, name, ref cchName, referencedDomainName, ref cchReferencedDomainName, out sidUse))
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                userName = name.ToString();
            }

            return userName;
        }

        [System.Diagnostics.DebuggerStepThrough]
        private static byte[] GetProcessSidBytes(Process targetProcess)
        {
            IntPtr tokenHandle = IntPtr.Zero;
            IntPtr processHandle = IntPtr.Zero;
            IntPtr tokenInfo = IntPtr.Zero;
            try
            {
                try
                {
                    processHandle = targetProcess.Handle;
                }
                catch (InvalidOperationException ex)
                {
                    string lcMessage = ex.Message.ToLowerInvariant();
                    if (lcMessage.Contains("exited"))
                    {
                        return null;
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Win32Exception ex)
                {
                    if (ex.NativeErrorCode == NativeMethods.Constants.ERROR_ACCESS_DENIED || targetProcess.HasExited)
                    {
                        return null;
                    }
                    else
                    {
                        throw;
                    }
                }

                if (!NativeMethods.OpenProcessToken(processHandle, NativeMethods.Constants.TOKEN_READ, out tokenHandle))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    if (errorCode == NativeMethods.Constants.ERROR_ACCESS_DENIED)
                    {
                        return null;
                    }
                    else
                    {
                        throw new Win32Exception();
                    }
                }

                var tokenInformationClass = NativeMethods.TokenInformationClass.TokenUser;
                uint tokenInfoLength = 0;
                NativeMethods.GetTokenInformation(tokenHandle, tokenInformationClass, IntPtr.Zero, tokenInfoLength, out tokenInfoLength);
                tokenInfo = Marshal.AllocHGlobal((int)tokenInfoLength);
                if (!NativeMethods.GetTokenInformation(tokenHandle, tokenInformationClass, tokenInfo, tokenInfoLength, out tokenInfoLength))
                {
                    throw new Win32Exception();
                }

                var tokenUser = (NativeMethods.TokenUser)Marshal.PtrToStructure(tokenInfo, typeof(NativeMethods.TokenUser));

                uint sidLength = NativeMethods.GetLengthSid(tokenUser.User.Sid);
                var sidBytes = new byte[sidLength];
                Marshal.Copy(tokenUser.User.Sid, sidBytes, 0, (int)sidLength);

                return sidBytes;
            }
            finally
            {
                if (tokenHandle != IntPtr.Zero)
                {
                    NativeMethods.CloseHandle(tokenHandle);
                }
                if (tokenInfo != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(tokenInfo);
                }
            }
        }

        public static void SafeKill(this Process process)
        {
            try
            {
                process.Kill();
            }
            catch (Win32Exception)
            {
                // If the process is terminating and kill is invoked again you will get a Win32Exception for AccessDenied.
                // http://msdn.microsoft.com/en-us/library/system.diagnostics.process.kill(v=vs.110).aspx
            }
        }
    }
}
