using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PooledAwait.Internal
{
    internal static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException() => throw new InvalidOperationException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static T ThrowInvalidOperationException<T>() => throw new InvalidOperationException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static T ThrowNotSupportedException<T>() => throw new NotSupportedException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentNullException(string paramName) => throw new ArgumentNullException(paramName);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowTaskCanceledException() => throw new TaskCanceledException();
    }
}
