using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace PooledAwait.Internal
{
    internal sealed class StateMachineBox<TStateMachine>
#if PLAT_THREADPOOLWORKITEM
        : IThreadPoolWorkItem
#endif
            where TStateMachine : IAsyncStateMachine
    {
        private readonly Action _execute;
        private TStateMachine _stateMachine;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private StateMachineBox()
        {
            _stateMachine = default!;
            _execute = Execute;
            Counters.StateMachineBoxAllocated.Increment();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static StateMachineBox<TStateMachine> Create(ref TStateMachine stateMachine)
        {
            var box = Pool<StateMachineBox<TStateMachine>>.TryGet() ?? new StateMachineBox<TStateMachine>();
            box._stateMachine = stateMachine;
            return box;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AwaitOnCompleted<TAwaiter>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
        {
            var box = Create(ref stateMachine);
            if (typeof(TAwaiter) == typeof(YieldAwaitable.YieldAwaiter))
            {
                Yield(box, true);
            }
            else
            {
                awaiter.OnCompleted(box._execute);
            }
        }
        private static void Yield(StateMachineBox<TStateMachine> box, bool flowContext)
        {
            // heavily inspired by YieldAwaitable.QueueContinuation

            var syncContext = SynchronizationContext.Current;
            if (syncContext != null && syncContext.GetType() != typeof(SynchronizationContext))
            {
                syncContext.Post(s_SendOrPostCallback, box);
            }
            else
            {
                var taskScheduler = TaskScheduler.Current;
                if (!ReferenceEquals(taskScheduler, TaskScheduler.Default))
                {
                    Task.Factory.StartNew(box._execute, default, TaskCreationOptions.PreferFairness, taskScheduler);
                }
                else if (flowContext)
                {
                    ThreadPool.QueueUserWorkItem(s_WaitCallback, box);
                }
                else
                {
#if PLAT_THREADPOOLWORKITEM
                    ThreadPool.UnsafeQueueUserWorkItem(box, false);
#elif NETSTANDARD1_3
                    ThreadPool.QueueUserWorkItem(s_WaitCallback, box);
#else
                    ThreadPool.UnsafeQueueUserWorkItem(s_WaitCallback, box);
#endif
                }
            }
        }

        static readonly SendOrPostCallback s_SendOrPostCallback = state => ((StateMachineBox<TStateMachine>)state).Execute();
        static readonly WaitCallback s_WaitCallback = state => ((StateMachineBox<TStateMachine>)state).Execute();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AwaitUnsafeOnCompleted<TAwaiter>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
        {
            var box = Create(ref stateMachine);
            if (typeof(TAwaiter) == typeof(YieldAwaitable.YieldAwaiter))
            {
                Yield(box, false);
            }
            else
            {
                awaiter.UnsafeOnCompleted(box._execute);
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
