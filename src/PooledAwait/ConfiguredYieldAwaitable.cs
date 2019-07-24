using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PooledAwait
{
    /// <summary>Provides an awaitable context for switching into a target environment.</summary>
    public readonly struct ConfiguredYieldAwaitable
    {
        /// <summary><see cref="Object.Equals(Object)"/></summary>
        public override bool Equals(object? obj) => obj is ConfiguredYieldAwaitable other && other._continueOnCapturedContext == _continueOnCapturedContext;
        /// <summary><see cref="Object.GetHashCode"/></summary>
        public override int GetHashCode() => _continueOnCapturedContext ? 1 : 0;
        /// <summary><see cref="Object.ToString"/></summary>
        public override string ToString() => nameof(ConfiguredYieldAwaitable);

        private readonly bool _continueOnCapturedContext;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ConfiguredYieldAwaitable(bool continueOnCapturedContext)
            => _continueOnCapturedContext = continueOnCapturedContext;

        /// <summary>Gets an awaiter for this <see cref="ConfiguredYieldAwaitable"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConfiguredYieldAwaiter GetAwaiter()
            => new ConfiguredYieldAwaiter(_continueOnCapturedContext);

        /// <summary>Provides an awaitable context for switching into a target environment.</summary>
        public readonly struct ConfiguredYieldAwaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            /// <summary><see cref="Object.Equals(Object)"/></summary>
            public override bool Equals(object? obj) => obj is ConfiguredYieldAwaiter other && other._continueOnCapturedContext == _continueOnCapturedContext;
            /// <summary><see cref="Object.GetHashCode"/></summary>
            public override int GetHashCode() => _continueOnCapturedContext ? 1 : 0;
            /// <summary><see cref="Object.ToString"/></summary>
            public override string ToString() => nameof(ConfiguredYieldAwaiter);

            private readonly bool _continueOnCapturedContext;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ConfiguredYieldAwaiter(bool continueOnCapturedContext)
                => _continueOnCapturedContext = continueOnCapturedContext;

            /// <summary>Gets whether a yield is not required.</summary>
            public bool IsCompleted
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => false;
            }

            /// <summary>Ends the await operation.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void GetResult() { }

            /// <summary>Posts the <paramref name="continuation"/> back to the current context.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action continuation)
            {
                if (_continueOnCapturedContext) YieldFlowContext(continuation, true);
                else YieldNoContext(continuation, true);
            }

            /// <summary>Posts the <paramref name="continuation"/> back to the current context.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UnsafeOnCompleted(Action continuation)
            {
                if (_continueOnCapturedContext) YieldFlowContext(continuation, false);
                else YieldNoContext(continuation, false);
            }

            private static readonly WaitCallback s_waitCallbackRunAction = state => ((Action?)state)?.Invoke();

#if PLAT_THREADPOOLWORKITEM
            private sealed class ContinuationWorkItem : IThreadPoolWorkItem
            {
                private Action? _continuation;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private ContinuationWorkItem() => Internal.Counters.ItemBoxAllocated.Increment();

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static ContinuationWorkItem Create(Action continuation)
                {
                    var box = Pool<ContinuationWorkItem>.TryGet() ?? new ContinuationWorkItem();
                    box._continuation = continuation;
                    return box;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                void IThreadPoolWorkItem.Execute()
                {
                    var callback = _continuation;
                    _continuation = null;
                    Pool<ContinuationWorkItem>.TryPut(this);
                    callback?.Invoke();
                }
            }
#endif
            [MethodImpl(MethodImplOptions.NoInlining)] // no-one ever calls ConfigureAwait(true)!
            private static void YieldFlowContext(Action continuation, bool flowContext)
            {
                var awaiter = default(YieldAwaitable.YieldAwaiter);
                if (flowContext) awaiter.OnCompleted(continuation);
                else awaiter.UnsafeOnCompleted(continuation);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void YieldNoContext(Action continuation, bool flowContext)
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
