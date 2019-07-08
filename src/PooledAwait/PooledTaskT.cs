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
        private readonly object? _taskOrSource;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PooledTask(object taskOrSource) => _taskOrSource = taskOrSource;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PooledTask(Exception exception) => _taskOrSource = Task.FromException(exception);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PooledTask(T result) => _taskOrSource = Task.FromResult(result);

        /// <summary>
        /// Gets the instance as a task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<T> AsTask()
        {
            if (_taskOrSource is TaskCompletionSource<T> source) return source.Task;
            if (_taskOrSource is Task<T> task) return task;
            Throw();
            return default!;

            static void Throw() => throw new InvalidOperationException();
        }

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
