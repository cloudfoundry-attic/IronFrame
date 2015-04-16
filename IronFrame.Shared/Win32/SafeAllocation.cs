using System;
using System.Runtime.InteropServices;

namespace IronFrame.Win32
{
    // TODO - Make all of this work well with enums. Some of these won't work with enums 
    // because their layour is Auto, not sequential.  In practice the layout for enums doesn't matter
    // and we should be able to just copy the data
    internal class SafeAllocation : SafeBuffer
    {
        public int Size
        {
            get { return (int) ByteLength; }
        }

        public SafeAllocation(Type type)
            : base(true)
        {
            Type outputType = type.IsEnum ? Enum.GetUnderlyingType(type) : type;

            int size = Marshal.SizeOf(outputType);
            this.SetHandle(Marshal.AllocHGlobal(size));
            this.Initialize((ulong)size);
        }

        public SafeAllocation(Type type, object value)
            : this(type)
        {
            Marshal.StructureToPtr(value, this.handle, false);
        }

        public static SafeAllocation<T> Create<T>(T value)
            where T : struct
        {
            return new SafeAllocation<T>(value);
        }

        public static SafeAllocation<T> Create<T>()
            where T : struct
        {
            return new SafeAllocation<T>();
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(this.handle);
            return true;
        }
    }

    internal class SafeAllocation<T> : SafeAllocation
        where T : struct
    {
        public SafeAllocation()
            : base(typeof(T))
        {
        }

        public SafeAllocation(T value)
            : base(typeof(T), value)
        {
        }

        public T ToStructure()
        {
            return this.Read<T>(0);
        }
    }
}
