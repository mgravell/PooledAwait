using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PooledAwait
{
    /// <summary>
    /// A Task, but with a custom builder
    /// </summary>
    [AsyncMethodBuilder(typeof(TaskBuilders.PooledTaskBuilder))]
    public readonly struct PooledTask
    {
        private readonly object? _taskOrSource;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PooledTask(object taskOrSource) => _taskOrSource = taskOrSource;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PooledTask(Exception exception) => _taskOrSource = Task.FromException(exception);

        /// <summary>
        /// Gets the instance as a task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task AsTask()
        {
            if (_taskOrSource == null) return Task.CompletedTask;
            if (_taskOrSource is TaskCompletionSource<bool> source) return source.Task;
            return (Task)_taskOrSource;
        }

        /// <summary>
        /// Gets the instance as a task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static implicit operator Task(in PooledTask task) => task.AsTask();

        /// <summary>
        /// Gets the awaiter for the task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TaskAwaiter GetAwaiter() => AsTask().GetAwaiter();

        /// <summary>
        /// Gets the configured awaiter for the task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConfiguredTaskAwaitable ConfigureAwait(bool continueOnCapturedContext)
            => AsTask().ConfigureAwait(continueOnCapturedContext);
    }
}
