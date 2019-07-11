using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PooledAwait.Internal
{
    internal static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException(string? message = null)
        {
            if (string.IsNullOrWhiteSpace(message)) throw new InvalidOperationException();
            else throw new InvalidOperationException(message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static T ThrowInvalidOperationException<T>(string? message = null)
        {
            ThrowInvalidOperationException(message);
            return default!;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static T ThrowNotSupportedException<T>() => throw new NotSupportedException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentNullException(string paramName) => throw new ArgumentNullException(paramName);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowTaskCanceledException() => throw new TaskCanceledException();
    }
}
