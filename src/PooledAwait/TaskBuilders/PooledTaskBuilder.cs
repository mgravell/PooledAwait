using PooledAwait.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PooledAwait.TaskBuilders
{
    public struct PooledTaskBuilder
    {
        private TaskCompletionSource<bool>? _state;
        private Exception _exception;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledTaskBuilder Create() => default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine _) => AllocCounters.IncrSetStateMachine();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult()
        {
            _state?.TrySetResult(true);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            EnsureState().TrySetException(exception);
            _exception = exception;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TaskCompletionSource<bool> EnsureState() => _state ?? CreateState();
        [MethodImpl(MethodImplOptions.NoInlining)]
        private TaskCompletionSource<bool> CreateState() => _state ?? (_state = new TaskCompletionSource<bool>());

        public PooledTask Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_state != null) return new PooledTask(_state);
                if (_exception != null) return new PooledTask(_exception);
                return default;
            }
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
