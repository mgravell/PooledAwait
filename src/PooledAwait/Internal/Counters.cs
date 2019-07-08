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
            public long Value => Interlocked.Read(ref _value);
            public override string ToString() => Value.ToString();
            public override bool Equals(object? obj)
                => throw new NotSupportedException();
            public override int GetHashCode()
                => throw new NotSupportedException();
            public void Reset() => Interlocked.Exchange(ref _value, 0);
        }

        internal static Counter
            SetStateMachine,
            PooledStateAllocated,
            PooledStateRecycled,
            StateMachineBoxAllocated,
            StateMachineBoxRecycled,
            ItemBoxAllocated;

        internal static void Reset()
        {
            SetStateMachine.Reset();
            PooledStateAllocated.Reset();
            PooledStateRecycled.Reset();
            StateMachineBoxAllocated.Reset();
            StateMachineBoxRecycled.Reset();
            ItemBoxAllocated.Reset();
        }


    }
}
