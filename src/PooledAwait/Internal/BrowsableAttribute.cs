#if NETSTANDARD1_3
using System;

namespace PooledAwait.Internal
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    internal sealed class BrowsableAttribute : Attribute
    {
        public BrowsableAttribute(bool _) { }
    }
}
#endif