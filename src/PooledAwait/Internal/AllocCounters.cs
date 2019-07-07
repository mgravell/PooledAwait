using System.Diagnostics;
using System.Threading;

namespace PooledAwait.Internal
{
    internal static class AllocCounters
    {
        private static long s_TaskSource, s_StateBox, s_SetStateMachine;
        public static long TaskSource => Interlocked.Read(ref s_TaskSource);
        public static long StateBox => Interlocked.Read(ref s_StateBox);
        public static long SetStateMachine => Interlocked.Read(ref s_SetStateMachine);
        [Conditional("DEBUG")]
        internal static void IncrTaskSource() => Interlocked.Increment(ref s_TaskSource);
        [Conditional("DEBUG")]
        internal static void IncrStateBox() => Interlocked.Increment(ref s_StateBox);
        [Conditional("DEBUG")]
        internal static void IncrSetStateMachine() => Interlocked.Increment(ref s_SetStateMachine);
    }
}
