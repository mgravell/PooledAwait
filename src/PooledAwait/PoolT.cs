using PooledAwait.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        internal static readonly int Size = CalculateSize();
        private static readonly Queue<T> _queue = new Queue<T>(Size);
        private volatile static int _countEstimate;

        [ThreadStatic]
        private static T? ts_local;

        static int CalculateSize()
        {
            const int DefaultSize = 16;
            int size = DefaultSize;

            var type = typeof(T);
            if (type == typeof(object)) ThrowHelper.ThrowInvalidOperationException("Pool<Object> is not supported; please use a more specific type");

#if !NETSTANDARD1_3
            const int MinSize = 0, MaxSize = 256;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Pool.ItemBox<>))
            {   // if doing a boxed T, tell us about the T - not the box
                type = type.GetGenericArguments()[0];
            }

            var attrib = (PoolSizeAttribute?)Attribute.GetCustomAttribute(type, typeof(PoolSizeAttribute), true);
            if (attrib != null)
            {
                if (attrib.Size < MinSize) size = MinSize;
                else if (attrib.Size > MaxSize) size = MaxSize;
                else size = attrib.Size;
            }
#endif
            return size;
        }

        internal static int Count()
        {
            lock (_queue)
            {
                return _queue.Count;
            }
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
                if (_countEstimate != 0)
                {
                    lock (_queue)
                    {
                        int count = _queue.Count;
                        if (count != 0)
                        {
                            _countEstimate = count - 1;
                            return _queue.Dequeue();
                        }
                    }
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
                if (_countEstimate < Size)
                {
                    lock (_queue)
                    {
                        int count = _queue.Count;
                        if (count < Size)
                        {
                            _countEstimate = count + 1;
                            _queue.Enqueue(value);
                        }
                    }
                }
            }
        }
    }

}
