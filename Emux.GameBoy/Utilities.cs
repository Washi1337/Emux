using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Emux.GameBoy
{
    internal static class Utilities
    {
        private static readonly Action<IntPtr, byte, int> MemsetDelegate;

        static Utilities()
        {
            var method = new DynamicMethod("__initblk",
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                typeof(void),
                new[]
                {
                    typeof(IntPtr), typeof(byte), typeof(int)
                },
                typeof(Utilities), true);

            var generator = method.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Initblk);
            generator.Emit(OpCodes.Ret);

            MemsetDelegate = (Action<IntPtr, byte, int>) method.CreateDelegate(typeof(Action<IntPtr, byte, int>));
        }

        public static void Memset(byte[] array, byte value, int length)
        {
            var handle = default(GCHandle);
            try
            {
                handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                MemsetDelegate(handle.AddrOfPinnedObject(), value, length);
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }
        }
    }
}
