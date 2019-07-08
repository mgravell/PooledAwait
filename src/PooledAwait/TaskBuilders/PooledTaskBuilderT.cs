using PooledAwait.Internal;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SystemTask = System.Threading.Tasks.Task;
#pragma warning disable CS1591

namespace PooledAwait.TaskBuilders
{
    /// <summary>
    /// This type is not intended for direct usage
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct PooledTaskBuilder<T>
    {
        private ValueTaskCompletionSource<T> _source;
        private Exception _exception;
        private T _result;

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledTaskBuilder<T> Create() => default;

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine _) => Counters.SetStateMachine.Increment();

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult(T result)
        {
            if (_source.HasTask) _source.TrySetResult(result);
            _result = result;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            if (_source.HasTask) _source.TrySetException(exception);
            _exception = exception;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public PooledTask<T> Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Task<T> task;
                if (_source.HasTask) task = _source.Task;
                else if (_exception != null) task = SystemTask.FromException<T>(_exception);
                else task = SystemTask.FromResult<T>(_result);
                return new PooledTask<T>(task);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureHasTask()
        {
            if (!_source.HasTask) _source = ValueTaskCompletionSource<T>.Create();
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
