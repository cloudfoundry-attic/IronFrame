using System;
using System.Runtime.InteropServices;

namespace IronFoundry.Container.Win32
{
    class SafeAllocation : SafeBuffer
    {
        public int Size
        {
            get { return (int) ByteLength; }
        }

        public SafeAllocation(Type type)
            : base(true)
        {
            int size = Marshal.SizeOf(type);
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

    class SafeAllocation<T> : SafeAllocation
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
