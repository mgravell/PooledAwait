using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PooledAwait.Internal
{
    internal sealed class StateMachineBox<TStateMachine>
#if PLAT_THREADPOOLWORKITEM
        : IThreadPoolWorkItem
#endif
            where TStateMachine : IAsyncStateMachine
    {
        private TStateMachine _stateMachine;
        private Action? _onCompleted;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private StateMachineBox()
        {
            _stateMachine = default!;
            Counters.StateMachineBoxAllocated.Increment();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static StateMachineBox<TStateMachine> Create(TStateMachine stateMachine)
        {
            var box = Pool<StateMachineBox<TStateMachine>>.TryGet() ?? new StateMachineBox<TStateMachine>();
            box._stateMachine = stateMachine;
            return box;
        }

        public Action OnCompleted => _onCompleted ?? CreateOnCompleted();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Action CreateOnCompleted() => _onCompleted = Execute;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AwaitOnCompleted<TAwaiter>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
        {
            var box = Create(stateMachine);
            if (typeof(TAwaiter) == typeof(YieldAwaitable.YieldAwaiter))
            {
                var syncContetx = SynchronizationContext.Current;
                if (syncContetx == null)
                {
                    // respect the fact that this isn't "unsafe"
                    ThreadPool.QueueUserWorkItem(s_WaitCallback, box);
                }
                else
                {
                    syncContetx.Post(s_SendOrPostCallback, box);
                }
            }
            else
            {
                awaiter.OnCompleted(box.OnCompleted);
            }
        }

        static readonly SendOrPostCallback s_SendOrPostCallback = state => ((StateMachineBox<TStateMachine>)state).Execute();
        static readonly WaitCallback s_WaitCallback = state => ((StateMachineBox<TStateMachine>)state).Execute();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AwaitUnsafeOnCompleted<TAwaiter>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
        {
            var box = Create(stateMachine);
            if (typeof(TAwaiter) == typeof(YieldAwaitable.YieldAwaiter))
            {
                var syncContetx = SynchronizationContext.Current;
                if (syncContetx == null)
                {
#if PLAT_THREADPOOLWORKITEM
                    ThreadPool.UnsafeQueueUserWorkItem(box, false);
#elif NETSTANDARD1_3
                    ThreadPool.QueueUserWorkItem(s_WaitCallback, box);
#else
                    ThreadPool.UnsafeQueueUserWorkItem(s_WaitCallback, box);
#endif
                }
                else
                {
                    syncContetx.Post(s_SendOrPostCallback, box);
                }
            }
            else
            {
                awaiter.UnsafeOnCompleted(box.OnCompleted);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute()
        {
            // extract the state
            var tmp = _stateMachine;

            // recycle the instance
            _stateMachine = default!;
            Pool<StateMachineBox<TStateMachine>>.TryPut(this);
            Counters.StateMachineBoxRecycled.Increment();

            // progress the state machine
            tmp.MoveNext();
        }
    }
}
