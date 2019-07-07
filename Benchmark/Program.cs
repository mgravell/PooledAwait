using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using PooledAwait;
using System;
using System.Threading.Tasks;

namespace Benchmark
{
    public static class Program
    {
#if DEBUG
        static async Task Main()
        {
            var obj = new Awaitable();

            for (int i = 0; i < 100; i++)
                await obj.ViaTaskLike();
            //Console.WriteLine(AllocCounters.StateBox); // 2
            //Console.WriteLine(AllocCounters.TaskSource); // 2
            //Console.WriteLine(AllocCounters.SetStateMachine); // 0
        }
#else
        static void Main() => BenchmarkRunner.Run<Awaitable>();
#endif
    }

    [MemoryDiagnoser, ShortRunJob]
    public class Awaitable
    {
        private const int InnerOps = 1000;

        [Benchmark(OperationsPerInvoke = InnerOps, Description = nameof(Task<int>), Baseline = true)]
        public async Task<int> ViaTask()
        {
            int sum = 0;
            for (int i = 0; i < InnerOps; i++)
                sum += await Inner(1, 2).ConfigureAwait(false);
            return sum;
            static async Task<int> Inner(int x, int y)
            {
                int i = x;
                await Task.Yield();
                i *= y;
                await Task.Yield();
                return 5 * i;
            }
        }

        [Benchmark(OperationsPerInvoke = InnerOps, Description = nameof(ValueTask<int>))]
        public async ValueTask<int> ViaValueTask()
        {
            int sum = 0;
            for (int i = 0; i < InnerOps; i++)
                sum += await Inner(1, 2).ConfigureAwait(false);
            return sum;
            static async ValueTask<int> Inner(int x, int y)
            {
                int i = x;
                await Task.Yield();
                i *= y;
                await Task.Yield();
                return 5 * i;
            }
        }

        // this should work, but it doesn't, because https://github.com/dotnet/BenchmarkDotNet/issues/1193
        // [Benchmark(OperationsPerInvoke = InnerOps, Description = nameof(TaskLike<int>))]
        public ValueTask<int> ViaTaskLike() => ViaTaskLikeImpl();

        // workaround is to add an "await"
        [Benchmark(OperationsPerInvoke = InnerOps, Description = nameof(PooledValueTask<int>))]
        public async ValueTask<int> ViaTaskLikeAwaited() => await ViaTaskLikeImpl();

        private async PooledValueTask<int> ViaTaskLikeImpl()
        {
            int sum = 0;
            for (int i = 0; i < InnerOps; i++)
                sum += await Inner(1, 2).ConfigureAwait(false);
            return sum;
            static async PooledValueTask<int> Inner(int x, int y)
            {
                int i = x;
                await Task.Yield();
                i *= y;
                await Task.Yield();
                return 5 * i;
            }
        }
    }
}
