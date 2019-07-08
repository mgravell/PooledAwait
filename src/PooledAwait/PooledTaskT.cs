using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PooledAwait
{
    /// <summary>
    /// A Task<typeparamref name="T"/>, but with a custom builder
    /// </summary>
    [AsyncMethodBuilder(typeof(TaskBuilders.PooledTaskBuilder<>))]
    public readonly struct PooledTask<T>
    {
        private readonly Task<T>? _task;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PooledTask(Task<T> task) => _task = task;

        /// <summary>
        /// Gets the instance as a task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<T> AsTask() => _task ?? ThrowInvalid<Task<T>>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static TResult ThrowInvalid<TResult>() => throw new InvalidOperationException();

        /// <summary>
        /// Gets the instance as a task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static implicit operator Task<T>(in PooledTask<T> task) => task.AsTask();

        /// <summary>
        /// Gets the awaiter for the task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TaskAwaiter<T> GetAwaiter() => AsTask().GetAwaiter();

        /// <summary>
        /// Gets the configured awaiter for the task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConfiguredTaskAwaitable<T> ConfigureAwait(bool continueOnCapturedContext)
            => AsTask().ConfigureAwait(continueOnCapturedContext);
    }
}
