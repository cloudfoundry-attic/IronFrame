using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using IronFoundry.Container.Win32;

namespace IronFoundry.Container.Utilities
{
    internal interface IEffectiveAccessComputer
    {
        ACCESS_MASK ComputeAccess(RawSecurityDescriptor descriptor, IdentityReference identity);
    }

    internal class EffectiveAccessComputer : IEffectiveAccessComputer
    {
        public ACCESS_MASK ComputeAccess(RawSecurityDescriptor descriptor, IdentityReference identity)
        {
            var accessGranted = ACCESS_MASK.NONE;

            // Create the Resource Manager
            using (SafeAuthzRMHandle authzRM = InitializeResourceManager())
            using (SafeAuthzContextHandle userClientCtxt = InitializeContextFromSid(authzRM, identity))
            {
                accessGranted = AccessCheck(userClientCtxt, descriptor);
            }

            return accessGranted;
        }

        private SafeAuthzRMHandle InitializeResourceManager()
        {
            SafeAuthzRMHandle authzRM;
            if (!NativeMethods.AuthzInitializeResourceManager(
                NativeMethods.AuthzResourceManagerFlags.NO_AUDIT,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                "EffectiveAccessCheck",
                out authzRM))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return authzRM;
        }

        private SafeAuthzContextHandle InitializeContextFromSid(SafeAuthzRMHandle authzRM, IdentityReference identity)
        {
            // Create the context for the user
            var securityIdentifier = (SecurityIdentifier) identity.Translate(typeof (SecurityIdentifier));
            var rawSid = new byte[securityIdentifier.BinaryLength];
            securityIdentifier.GetBinaryForm(rawSid, 0);

            SafeAuthzContextHandle userClientCtxt;
            if (!NativeMethods.AuthzInitializeContextFromSid(
                NativeMethods.AuthzInitFlags.Default,
                rawSid,
                authzRM,
                IntPtr.Zero,
                NativeMethods.LUID.NullLuid,
                IntPtr.Zero,
                out userClientCtxt))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return userClientCtxt;
        }

        private ACCESS_MASK AccessCheck(SafeAuthzContextHandle userClientCtxt, RawSecurityDescriptor descriptor)
        {
            ACCESS_MASK accessGranted;

            // Prepare the Access Check request
            var request = new NativeMethods.AUTHZ_ACCESS_REQUEST();
            request.DesiredAccess = ACCESS_MASK.MAXIMUM_ALLOWED;
            request.PrincipalSelfSid = null;
            request.ObjectTypeList = IntPtr.Zero;
            request.ObjectTypeListLength = 0;
            request.OptionalArguments = IntPtr.Zero;


            using (var grantedAccessBuffer = SafeAllocation.Create<ACCESS_MASK>())
            using (var errorBuffer = SafeAllocation.Create<uint>())
            {
                // Prepare the access check reply
                var reply = new NativeMethods.AUTHZ_ACCESS_REPLY();
                reply.ResultListLength = 1;
                reply.SaclEvaluationResults = IntPtr.Zero;
                reply.GrantedAccessMask = grantedAccessBuffer.DangerousGetHandle();
                reply.Error = errorBuffer.DangerousGetHandle();


                var rawSD = new byte[descriptor.BinaryLength];
                descriptor.GetBinaryForm(rawSD, 0);

                if (!NativeMethods.AuthzAccessCheck(
                    NativeMethods.AuthzACFlags.None,
                    userClientCtxt,
                    ref request,
                    IntPtr.Zero,
                    rawSD,
                    null,
                    0,
                    ref reply,
                    IntPtr.Zero))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                accessGranted = grantedAccessBuffer.ToStructure();
            }

            return accessGranted;
        }
    }
}