using PooledAwait.Internal;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SystemTask = System.Threading.Tasks.Task;

#pragma warning disable CS1591

namespace PooledAwait.TaskBuilders
{
    /// <summary>
    /// This type is not intended for direct usage
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct PooledTaskBuilder
    {
        public override bool Equals(object obj) => ThrowHelper.ThrowNotSupportedException<bool>();
        public override int GetHashCode() => ThrowHelper.ThrowNotSupportedException<int>();
        public override string ToString() => nameof(PooledTaskBuilder);

        private ValueTaskCompletionSource<Nothing> _source;
        private Exception _exception;

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledTaskBuilder Create() => default;

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine _) => Counters.SetStateMachine.Increment();

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult()
        {
            if (_source.HasTask) _source.TrySetResult(default);
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            if (_source.HasTask) _source.TrySetException(exception);
            _exception = exception;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureHasTask()
        {
            if (!_source.HasTask) _source = ValueTaskCompletionSource<Nothing>.Create();
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public PooledTask Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                SystemTask task;
                if (_source.HasTask) task = _source.Task;
                else if (_exception is OperationCanceledException) task = TaskUtils.TaskFactory<Nothing>.Canceled;
                else if (_exception != null) task = TaskUtils.FromException(_exception);
                else task = TaskUtils.CompletedTask;
                return new PooledTask(task);
            }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            EnsureHasTask();
            StateMachineBox<TStateMachine>.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            EnsureHasTask();
            StateMachineBox<TStateMachine>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();
    }
}
