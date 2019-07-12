using PooledAwait.Internal;
using PooledAwait.MethodBuilders;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace PooledAwait.Test
{
    public class NullableTypes
    {
        [Theory]
        [InlineData(typeof(FireAndForget))]
        [InlineData(typeof(PooledTask))]
        [InlineData(typeof(PooledTask<>))]
        [InlineData(typeof(PooledValueTask))]
        [InlineData(typeof(PooledValueTask<>))]
        [InlineData(typeof(PooledValueTaskSource))]
        [InlineData(typeof(PooledValueTaskSource<>))]
        [InlineData(typeof(ValueTaskCompletionSource<>))]
        [InlineData(typeof(FireAndForgetMethodBuilder))]
        [InlineData(typeof(PooledTaskMethodBuilder))]
        [InlineData(typeof(PooledTaskMethodBuilder<>))]
        [InlineData(typeof(PooledValueTaskMethodBuilder))]
        [InlineData(typeof(PooledValueTaskMethodBuilder<>))]
        [InlineData(typeof(LazyTaskCompletionSource))]
        [InlineData(typeof(LazyTaskCompletionSource<>))]
        [InlineData(typeof(Nothing))]
        [InlineData(typeof(Counters.Counter))]
        [InlineData(typeof(ConfiguredYieldAwaitable))]
        [InlineData(typeof(ConfiguredYieldAwaitable.ConfiguredYieldAwaiter))]
        public void ValueTypesOverrideAllMethods(Type type)
        {
            Assert.Same(type, type.GetMethod(nameof(ToString), Type.EmptyTypes).DeclaringType);
            Assert.Same(type, type.GetMethod(nameof(GetHashCode), Type.EmptyTypes).DeclaringType);
            Assert.Same(type, type.GetMethod(nameof(Equals), new Type[] { typeof(object) }).DeclaringType);

#if NETCOREAPP3_0
            Assert.Equal(!s_allowedMutable.Contains(type), Attribute.IsDefined(type, typeof(IsReadOnlyAttribute)));
#endif
        }

        static readonly Type[] s_allowedMutable =
        {
            typeof(Counters.Counter),
            typeof(FireAndForgetMethodBuilder),
            typeof(PooledTaskMethodBuilder),
            typeof(PooledTaskMethodBuilder<>),
            typeof(PooledValueTaskMethodBuilder),
            typeof(PooledValueTaskMethodBuilder<>),
        };

    }
}
