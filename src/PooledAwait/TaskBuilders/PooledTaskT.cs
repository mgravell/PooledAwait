using PooledAwait.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PooledAwait.TaskBuilders
{
    public struct PooledTaskBuilder<T>
    {
        private TaskCompletionSource<T>? _state;
        private Exception _exception;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledTaskBuilder<T> Create() => default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine _) => AllocCounters.IncrSetStateMachine();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult(T result)
        {
            _state?.TrySetResult(result);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            EnsureState().TrySetException(exception);
            _exception = exception;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TaskCompletionSource<T> EnsureState() => _state ?? CreateState();
        [MethodImpl(MethodImplOptions.NoInlining)]
        private TaskCompletionSource<T> CreateState() => _state ?? (_state = new TaskCompletionSource<T>());

        public PooledTask<T> Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_state != null) return new PooledTask<T>(_state);
                if (_exception != null) return new PooledTask<T>(_exception);
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
