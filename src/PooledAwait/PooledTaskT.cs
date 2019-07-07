using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PooledAwait
{
    // this only exists to provide access to a custom builder
    [AsyncMethodBuilder(typeof(TaskBuilders.PooledTaskBuilder<>))]
    public readonly struct PooledTask<T>
    {
        private readonly object? _taskOrSource;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PooledTask(TaskCompletionSource<T> source) => _taskOrSource = source;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PooledTask(Exception exception) => _taskOrSource = Task.FromException(exception);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<T> AsTask()
        {
            if (_taskOrSource is TaskCompletionSource<T> source) return source.Task;
            return (Task<T>)_taskOrSource;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static implicit operator Task<T>(in PooledTask<T> task) => task.AsTask();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TaskAwaiter<T> GetAwaiter() => AsTask().GetAwaiter();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConfiguredTaskAwaitable<T> ConfigureAwait(bool continueOnCapturedContext)
            => AsTask().ConfigureAwait(continueOnCapturedContext);
    }
}
