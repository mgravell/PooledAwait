using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PooledAwait
{
    /// <summary>Provides an awaitable context for switching into a target environment.</summary>
    public struct ConfiguredYieldAwaitable
    {
        private readonly bool _continueOnCapturedContext;
        internal ConfiguredYieldAwaitable(bool continueOnCapturedContext)
            => _continueOnCapturedContext = continueOnCapturedContext;

        /// <summary>Gets an awaiter for this <see cref="ConfiguredYieldAwaitable"/>.</summary>
        public ConfiguredYieldAwaiter GetAwaiter()
            => new ConfiguredYieldAwaiter(_continueOnCapturedContext);

        /// <summary>Provides an awaitable context for switching into a target environment.</summary>
        public struct ConfiguredYieldAwaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            private readonly bool _continueOnCapturedContext;
            internal ConfiguredYieldAwaiter(bool continueOnCapturedContext)
                => _continueOnCapturedContext = continueOnCapturedContext;

            /// <summary>Gets whether a yield is not required.</summary>
            public bool IsCompleted => false;

            /// <summary>Ends the await operation.</summary>
            public void GetResult() { }

            /// <summary>Posts the <paramref name="continuation"/> back to the current context.</summary>
            public void OnCompleted(Action continuation) => Yield(continuation, true);
            /// <summary>Posts the <paramref name="continuation"/> back to the current context.</summary>
            public void UnsafeOnCompleted(Action continuation) => Yield(continuation, false);

            private static readonly WaitCallback s_waitCallbackRunAction = state => ((Action)state)?.Invoke();

#if PLAT_THREADPOOLWORKITEM
            private sealed class ContinuationWorkItem : IThreadPoolWorkItem
            {
                private Action? _continuation;
                private ContinuationWorkItem() { }
                public static ContinuationWorkItem Create(Action continuation)
                {
                    var box = Pool<ContinuationWorkItem>.TryGet() ?? new ContinuationWorkItem();
                    box._continuation = continuation;
                    return box;
                }

                void IThreadPoolWorkItem.Execute()
                {
                    var callback = _continuation;
                    _continuation = null;
                    Pool<ContinuationWorkItem>.TryPut(this);
                    callback?.Invoke();
                }
            }
#endif

            private void Yield(Action continuation, bool flowContext)
            {
                if (_continueOnCapturedContext)
                {
                    if (flowContext)
                    {
                        default(YieldAwaitable.YieldAwaiter).OnCompleted(continuation);
                    }
                    else
                    {
                        default(YieldAwaitable.YieldAwaiter).UnsafeOnCompleted(continuation);
                    }
                }
                else
                {
                    if (flowContext)
                    {
                        ThreadPool.QueueUserWorkItem(s_waitCallbackRunAction, continuation);
                    }
                    else
                    {
#if PLAT_THREADPOOLWORKITEM
                        ThreadPool.UnsafeQueueUserWorkItem(ContinuationWorkItem.Create(continuation), false);
#elif NETSTANDARD1_3
                        ThreadPool.QueueUserWorkItem(s_waitCallbackRunAction, continuation);
#else
                        ThreadPool.UnsafeQueueUserWorkItem(s_waitCallbackRunAction, continuation);
#endif
                    }
                }
            }
        }
    }
}
