using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PooledAwait.Internal
{
    internal static class AllocCounters
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
        }

        internal static Counter SetStateMachine,
            PooledStateAllocated, PooledStateRecycled,
            StateMachineBoxAllocated, StateMachineBoxRecycled;

    }
}
