using PooledAwait.Internal;
using System;
using System.Runtime.CompilerServices;

namespace PooledAwait
{
    public struct PooledValueTaskBuilder<T>
    {
        private PooledState<T>? _state;
        private short _token;
        private T _result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledValueTaskBuilder<T> Create() => default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine _) => AllocCounters.IncrSetStateMachine();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult(T result)
        {
            _state?.TrySetResult(result, _token);
            _result = result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            EnsureState().TrySetException(exception, _token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PooledState<T> EnsureState() => _state ?? CreateState();
        [MethodImpl(MethodImplOptions.NoInlining)]
        private PooledState<T> CreateState() => _state ?? (_state = PooledState<T>.Create(out _token));

        public PooledValueTask<T> Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _state == null ? new PooledValueTask<T>(_result) : _state.PooledValueTask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            EnsureState();
            StateMachineBox<TStateMachine>.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            EnsureState();
            StateMachineBox<TStateMachine>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();
    }
}
