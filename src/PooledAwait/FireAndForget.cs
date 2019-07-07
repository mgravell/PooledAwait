using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PooledAwait
{
    /// <summary>
    /// Represents an operation that completes at the first incomplete await,
    /// with the remainder continuing in the background
    /// </summary>
    [AsyncMethodBuilder(typeof(TaskBuilders.FireAndForgetBuilder))]
    public readonly struct FireAndForget
    {
        /// <summary>
        /// Gets the instance as a value-task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask AsValueTask() => default;

        /// <summary>
        /// Gets the instance as a task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task AsTask() => Task.CompletedTask;

        /// <summary>
        /// Gets the instance as a task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Task(FireAndForget _) => Task.CompletedTask;

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
