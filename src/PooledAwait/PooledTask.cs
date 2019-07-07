using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PooledAwait
{
    // this only exists to provide access to a custom builder
    [AsyncMethodBuilder(typeof(TaskBuilders.PooledTaskBuilder))]
    public readonly struct PooledTask
    {
        private readonly object? _taskOrSource;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PooledTask(TaskCompletionSource<bool> source) => _taskOrSource = source;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PooledTask(Exception exception) => _taskOrSource = Task.FromException(exception);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task AsTask()
        {
            if (_taskOrSource == null) return Task.CompletedTask;
            if (_taskOrSource is TaskCompletionSource<bool> source) return source.Task;
            return (Task)_taskOrSource;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static implicit operator Task(in PooledTask task) => task.AsTask();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TaskAwaiter GetAwaiter() => AsTask().GetAwaiter();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConfiguredTaskAwaitable ConfigureAwait(bool continueOnCapturedContext)
            => AsTask().ConfigureAwait(continueOnCapturedContext);
    }
}
