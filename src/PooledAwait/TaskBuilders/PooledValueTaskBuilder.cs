using PooledAwait.Internal;
using System;
using System.Runtime.CompilerServices;

namespace PooledAwait.TaskBuilders
{
    public struct PooledValueTaskBuilder
    {
        private PooledState? _state;
        private short _token;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledValueTaskBuilder Create() => default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine _) => AllocCounters.IncrSetStateMachine();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult()
        {
            _state?.TrySetResult(_token);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            EnsureState().TrySetException(exception, _token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PooledState EnsureState() => _state ?? CreateState();
        [MethodImpl(MethodImplOptions.NoInlining)]
        private PooledState CreateState() => _state ?? (_state = PooledState.Create(out _token));

        public PooledValueTask Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _state == null ? default : _state.PooledValueTask;
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
