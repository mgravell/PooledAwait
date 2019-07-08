

#if DEBUG
using System;
using System.Threading.Tasks;
using PooledAwait.Internal;
#else
using BenchmarkDotNet.Running;
#endif

namespace Benchmark
{
    public static class Program
    {
#if DEBUG
        static async Task Main()
        {
            var obj = new ComparisonBenchmarks();

            for (int i = 0; i < 100; i++)
                await obj.ViaPooledValueTask();

            Console.WriteLine(nameof(Counters.PooledStateAllocated) + ": " + Counters.PooledStateAllocated); // 2
            Console.WriteLine(nameof(Counters.PooledStateRecycled) + ": " + Counters.PooledStateRecycled); // 100100
            Console.WriteLine(nameof(Counters.StateMachineBoxAllocated) + ": " + Counters.StateMachineBoxAllocated); // 2
            Console.WriteLine(nameof(Counters.StateMachineBoxRecycled) + ": " + Counters.StateMachineBoxRecycled); // 299979
            Console.WriteLine(nameof(Counters.SetStateMachine) + ": " + Counters.SetStateMachine); // 0
        }
#else
        static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
#endif
    }
}
