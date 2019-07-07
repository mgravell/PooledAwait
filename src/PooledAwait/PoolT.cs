using System;
using System.Threading;

namespace PooledAwait
{
    public static class Pool<T> where T : class
    {
        [ThreadStatic]
        private static T? ts_local;

        private static readonly T?[] s_global = new T[8];

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
