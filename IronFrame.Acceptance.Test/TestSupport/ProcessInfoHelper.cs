using System;
using System.Diagnostics;
using System.Net;
using System.Collections;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Management;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;

namespace IronFrame
{
    public class ProcessInfoHelper
    {
        public const int TOKEN_QUERY = 0X00000008;

        const int ERROR_NO_MORE_ITEMS = 259;

        enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId
        }

        [StructLayout(LayoutKind.Sequential)]
        struct TOKEN_USER
        {
            public _SID_AND_ATTRIBUTES User;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct _SID_AND_ATTRIBUTES
        {
            public IntPtr Sid;
            public int Attributes;
        }

        [DllImport("advapi32")]
        static extern bool OpenProcessToken(
            IntPtr ProcessHandle, // handle to process
            int DesiredAccess, // desired access to process
            ref IntPtr TokenHandle // handle to open access token
        );

        [DllImport("kernel32")]
        static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32", CharSet = CharSet.Auto)]
        static extern bool GetTokenInformation(
            IntPtr hToken,
            TOKEN_INFORMATION_CLASS tokenInfoClass,
            IntPtr TokenInformation,
            int tokeInfoLength,
            ref int reqLength
        );

        [DllImport("kernel32")]
        static extern bool CloseHandle(IntPtr handle);

        [DllImport("advapi32", CharSet = CharSet.Auto)]
        static extern bool LookupAccountSid
        (
            [In, MarshalAs(UnmanagedType.LPTStr)] string lpSystemName, // name of local or remote computer
            IntPtr pSid, // security identifier
            StringBuilder Account, // account name buffer
            ref int cbName, // size of account name buffer
            StringBuilder DomainName, // domain name
            ref int cbDomainName, // size of domain name buffer
            ref int peUse // SID type
            // ref _SID_NAME_USE peUse // SID type
        );

        [DllImport("advapi32", CharSet = CharSet.Auto)]
        static extern bool ConvertSidToStringSid(
            IntPtr pSID,
            [In, Out, MarshalAs(UnmanagedType.LPTStr)] ref string pStringSid
        );

        [DllImport("advapi32", CharSet = CharSet.Auto)]
        static extern bool ConvertStringSidToSid(
            [In, MarshalAs(UnmanagedType.LPTStr)] string pStringSid,
            ref IntPtr pSID
        );

        /// <summary>
        /// Collect User Info
        /// </summary>
        /// <param name="pToken">Process Handle</param>
        public static bool DumpUserInfo(IntPtr pToken, out IntPtr SID)
        {
            int Access = TOKEN_QUERY;
            IntPtr procToken = IntPtr.Zero;
            bool ret = false;
            SID = IntPtr.Zero;
            try
            {
                if (OpenProcessToken(pToken, Access, ref procToken))
                {
                    ret = ProcessTokenToSid(procToken, out SID);
                    CloseHandle(procToken);
                }
                return ret;
            }
            catch (Exception err)
            {
//                log.Error("Method [" + new StackFrame(0).GetMethod().Name + "]. Error " + err.Message);
                return false;
            }
        }

        private static bool ProcessTokenToSid(IntPtr token, out IntPtr SID)
        {
            TOKEN_USER tokUser;
            const int bufLength = 256;
            IntPtr tu = Marshal.AllocHGlobal(bufLength);
            bool ret = false;
            SID = IntPtr.Zero;
            try
            {
                int cb = bufLength;
                ret = GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenUser, tu, cb, ref cb);
                if (ret)
                {
                    tokUser = (TOKEN_USER)Marshal.PtrToStructure(tu, typeof(TOKEN_USER));
                    SID = tokUser.User.Sid;
                }
                return ret;
            }
            catch (Exception err)
            {
//                log.Error("Method [" + new StackFrame(0).GetMethod().Name + "]. Error " + err.Message);
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(tu);
            }
        }

        public static string ExGetProcessInfoByPID(int PID, out string SID)//, out string OwnerSID)
        {
            IntPtr _SID = IntPtr.Zero;
            SID = String.Empty;
            try
            {
                Process process = Process.GetProcessById(PID);
                if (DumpUserInfo(process.Handle, out _SID))
                {
                    ConvertSidToStringSid(_SID, ref SID);
                }
                return process.ProcessName;
            }
            catch
            {
                return "Unknown";
            }
        }



        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);


        public static void SuspendProcess(int pid)
        {
            var process = Process.GetProcessById(pid);

            if (process.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                SuspendThread(pOpenThread);

                CloseHandle(pOpenThread);
            }
        }
    }
}