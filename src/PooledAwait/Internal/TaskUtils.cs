using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PooledAwait.Internal
{
    // NET45 lacks some useful Task APIs; shim over them
    internal static class TaskUtils
    {
#if NET45
        public static readonly Task CompletedTask = Task.FromResult(true);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<T> FromException<T>(Exception exception)
        {
            var source = ValueTaskCompletionSource<T>.Create();
            source.TrySetException(exception);
            return source.Task;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task FromException(Exception exception) => FromException<bool>(exception);
#else
        public static readonly Task CompletedTask = Task.CompletedTask;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<T> FromException<T>(Exception exception) => Task.FromException<T>(exception);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task FromException(Exception exception) => Task.FromException(exception);
#endif
    }
}
