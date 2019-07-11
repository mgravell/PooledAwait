using PooledAwait.Internal;
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
        /// <summary><see cref="Object.Equals(Object)"/></summary>
        public override bool Equals(object obj) => obj is PooledTask<T> pt && _task == pt._task;
        /// <summary><see cref="Object.GetHashCode"/></summary>
        public override int GetHashCode() => _task == null ? 0 : _task.GetHashCode();
        /// <summary><see cref="Object.ToString"/></summary>
        public override string ToString() => nameof(PooledTask);

        private readonly Task<T>? _task;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PooledTask(Task<T> task) => _task = task;

        /// <summary>
        /// Gets the instance as a task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<T> AsTask() => _task ?? ThrowHelper.ThrowInvalidOperationException<Task<T>>();

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

        /// <summary>
        /// Indicates whether this is an invalid default instance
        /// </summary>
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _task == null;
        }
    }
}
