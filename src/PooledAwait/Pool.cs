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
        {
            var obj = Pool<ItemBox<T>>.TryGet() ?? new ItemBox<T>();
            obj.Value = value;
            return obj;
        }

        /// <summary>
        /// Unwraps a value-type from a boxed instance and recycles
        /// the instance, which should not be touched again
        /// </summary>
        public static T UnboxAndRecycle<T>(object obj) where T : struct
        {
            var box = (ItemBox<T>)obj;
            var value = box.Value;
            box.Value = default;
            Pool<ItemBox<T>>.TryPut(box);
            return value;
        }

        private sealed class ItemBox<T> where T : struct
        {
            public T Value;
        }
    }
}
