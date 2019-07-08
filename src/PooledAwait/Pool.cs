using PooledAwait.Internal;

namespace PooledAwait
{
    /// <summary>
    /// Utility methods for boxing value types efficiently, in particular for
    /// avoid boxes and capture contexts in callbacks
    /// </summary>
    public static class Pool
    {
        /// <summary>
        /// Wraps a value-type into a boxed instance, using an object pool;
        /// consider using value-tuples in particular
        /// </summary>
        public static object Box<T>(in T value) where T : struct
            => ItemBox<T>.Create(in value);

        /// <summary>
        /// Unwraps a value-type from a boxed instance and recycles
        /// the instance, which should not be touched again
        /// </summary>
        public static T UnboxAndRecycle<T>(object obj) where T : struct
            => ItemBox<T>.UnboxAndRecycle(obj);

        private sealed class ItemBox<T> where T : struct
        {
            private ItemBox() => Counters.ItemBoxAllocated.Increment();
            public static ItemBox<T> Create(in T value)
            {
                var box = Pool<ItemBox<T>>.TryGet() ?? new ItemBox<T>();
                box._value = value;
                return box;
            }
            public static T UnboxAndRecycle(object obj)
            {
                var box = (ItemBox<T>)obj;
                var value = box._value;
                box._value = default;
                Pool<ItemBox<T>>.TryPut(box);
                return value;
            }
            private T _value;
        }
    }
}
