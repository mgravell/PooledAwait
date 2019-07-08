using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PooledAwait
{
    /// <summary>
    /// A general-purpose pool of object references; it is the caller's responsibility
    /// to ensure that overlapped usage does not occur
    /// </summary>
    public static class Pool<T> where T : class
    {
        [ThreadStatic]
        private static T? ts_local;

        private static readonly T?[] s_global = new T[16];

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
                if (ts_local != null)
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
