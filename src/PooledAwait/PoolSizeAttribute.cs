using System;

#if !NETSTANDARD1_3
namespace PooledAwait
{
    /// <summary>
    /// Controls the number of elements to store in the pool
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class PoolSizeAttribute : Attribute
    {
        /// <summary>
        /// The number of elements to store in the pool
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Create a new PoolSizeAttribute instance
        /// </summary>
        public PoolSizeAttribute(int size) => Size = size;
    }
}
#endif
