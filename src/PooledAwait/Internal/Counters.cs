using System;
using System.Diagnostics;
using System.Threading;

namespace PooledAwait.Internal
{
    internal static class Counters
    {
        internal struct Counter
        {
            private long _value;
            [Conditional("DEBUG")]
            public void Increment() => Interlocked.Increment(ref _value);
#if !DEBUG
            [Obsolete("Release only", false)]
#endif
            public long Value => Interlocked.Read(ref _value);
#pragma warning disable CS0618
            public override string ToString() => Value.ToString();
#pragma warning restore CS0618
            public override bool Equals(object? obj) => ThrowHelper.ThrowNotSupportedException<bool>();
            public override int GetHashCode() => ThrowHelper.ThrowNotSupportedException<int>();
            public void Reset() => Interlocked.Exchange(ref _value, 0);
        }

        internal static Counter
            SetStateMachine,
            PooledStateAllocated,
            PooledStateRecycled,
            StateMachineBoxAllocated,
            StateMachineBoxRecycled,
            ItemBoxAllocated,
            TaskAllocated,
            LazyStateAllocated;

#if !DEBUG
        [Obsolete("Release only", false)]
#endif
        public static long TotalAllocations =>
            PooledStateAllocated.Value + StateMachineBoxAllocated.Value
            + ItemBoxAllocated.Value + TaskAllocated.Value
            + SetStateMachine.Value // SetStateMachine usually means a boxed value
            + LazyStateAllocated.Value;

        internal static void Reset()
        {
            SetStateMachine.Reset();
            PooledStateAllocated.Reset();
            PooledStateRecycled.Reset();
            StateMachineBoxAllocated.Reset();
            StateMachineBoxRecycled.Reset();
            ItemBoxAllocated.Reset();
            TaskAllocated.Reset();
            LazyStateAllocated.Reset();
        }


    }
}
