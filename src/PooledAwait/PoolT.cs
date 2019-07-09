using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PooledAwait
{
    /// <summary>
    /// A general-purpose pool of object references; it is the caller's responsibility
    /// to ensure that overlapped usage does not occur
    /// </summary>
    internal static class Pool<T> where T : class
    {
        internal static int Size => s_global.Length;

        [ThreadStatic]
        private static T? ts_local;

        private static readonly T?[] s_global = CreatePool();

        static T[] CreatePool()
        {
            const int DefaultSize = 16;
            int size = DefaultSize;

#if !NETSTANDARD1_3
            const int MinSize = 0, MaxSize = 256;

            var type = typeof(T);
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Pool.ItemBox<>))
            {   // if doing a boxed T, tell us about the T - not the box
                type = type.GetGenericArguments()[0];
            }

            var attrib = (PoolSizeAttribute)Attribute.GetCustomAttribute(type, typeof(PoolSizeAttribute), true);
            if (attrib != null)
            {
                if (attrib.Size < MinSize) size = MinSize;
                else if (attrib.Size > MaxSize) size = MaxSize;
                else size = attrib.Size;
            }
#endif

#if !NET45
            if (size == 0) return Array.Empty<T>();
#endif
            return new T[size];
        }

        /// <summary>
        /// Gets an instance from the pool if possible
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? TryGet()
        {
            var tmp = ts_local;
            ts_local = null;
            return tmp ?? FromPool();

            static T? FromPool()
            {
                var pool = s_global;
                for (int i = 0; i < pool.Length; i++)
                {
                    var tmp = Interlocked.Exchange(ref pool[i], null);
                    if (tmp != null) return tmp;
                }
                return null;
            }
        }

        /// <summary>
        /// Puts an instance back into the pool
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TryPut(T value)
        {
            if (value != null)
            {
                if (ts_local == null)
                {
                    ts_local = value;
                    return;
                }
                ToPool(value);
            }
            static void ToPool(T value)
            {
                var pool = s_global;
                for (int i = 0; i < pool.Length; i++)
                {
                    if (Interlocked.CompareExchange(ref pool[i], value, null) == null)
                        return;
                }
            }
        }
    }

}
