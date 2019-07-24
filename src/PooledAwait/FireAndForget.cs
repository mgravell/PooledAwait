using PooledAwait.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PooledAwait
{
    /// <summary>
    /// Represents an operation that completes at the first incomplete await,
    /// with the remainder continuing in the background
    /// </summary>
    [AsyncMethodBuilder(typeof(MethodBuilders.FireAndForgetMethodBuilder))]
    public readonly struct FireAndForget
    {
        /// <summary><see cref="Object.Equals(Object)"/></summary>
        public override bool Equals(object? obj) => ThrowHelper.ThrowNotSupportedException<bool>();
        /// <summary><see cref="Object.GetHashCode"/></summary>
        public override int GetHashCode() => ThrowHelper.ThrowNotSupportedException<int>();
        /// <summary><see cref="Object.ToString"/></summary>
        public override string ToString() => nameof(FireAndForget);

        /// <summary>
        /// Raised when exceptions occur on fire-and-forget methods
        /// </summary>
        public static event Action<Exception> Exception;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void OnException(Exception exception)
        {
            if (exception != null) Exception?.Invoke(exception);
        }

        /// <summary>
        /// Gets the instance as a value-task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask AsValueTask() => default;

        /// <summary>
        /// Gets the instance as a task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task AsTask() => TaskUtils.CompletedTask;

        /// <summary>
        /// Gets the instance as a task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Task(FireAndForget _) => TaskUtils.CompletedTask;

        /// <summary>
        /// Gets the instance as a value-task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ValueTask(FireAndForget _) => default;

        /// <summary>
        /// Gets the awaiter for the instance
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTaskAwaiter GetAwaiter() => default(ValueTask).GetAwaiter();
    }
}
