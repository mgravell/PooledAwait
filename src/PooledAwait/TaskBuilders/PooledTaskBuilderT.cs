using PooledAwait.Internal;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
        private object? _factoryState;
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
            if (_factoryState != null) PendingTaskFactory<T>.TrySetResult(_factoryState, result);
            _result = result;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            PendingTaskFactory<T>.TrySetException(EnsureState(), exception);
            _exception = exception;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object EnsureState() => _factoryState ?? CreateState();
        [MethodImpl(MethodImplOptions.NoInlining)]
        private object CreateState() => _factoryState ?? (_factoryState = PendingTaskFactory<T>.Create());

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public PooledTask<T> Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_factoryState != null) return new PooledTask<T>(_factoryState);
                if (_exception != null) return new PooledTask<T>(_exception);
                return new PooledTask<T>(_result);
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
            EnsureState();
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
            EnsureState();
            StateMachineBox<TStateMachine>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();
    }
}
