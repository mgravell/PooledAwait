using PooledAwait.Internal;
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
        /// <summary><see cref="Object.Equals(Object)"/></summary>
        public override bool Equals(object obj) => obj is PooledTask pt && _task ==  pt._task;
        /// <summary><see cref="Object.GetHashCode"/></summary>
        public override int GetHashCode() => _task == null ? 0 : _task.GetHashCode();
        /// <summary><see cref="Object.ToString"/></summary>
        public override string ToString() => nameof(PooledTask);

        private readonly Task _task;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PooledTask(Task task) => _task = task;

        /// <summary>
        /// Gets the instance as a task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task AsTask() => _task ?? TaskUtils.CompletedTask;

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
