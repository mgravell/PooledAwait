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
        private readonly Task _task;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PooledTask(Task task) => _task = task;

        /// <summary>
        /// Gets the instance as a task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task AsTask() => _task ?? Task.CompletedTask;

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
