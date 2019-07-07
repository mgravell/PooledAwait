using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using PooledAwait;
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
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    public class Awaitable
    {
        private const int InnerOps = 1000;

        [Benchmark(OperationsPerInvoke = InnerOps, Description = "T")]
        [BenchmarkCategory(nameof(Task))]
        public async Task<int> ViaTaskT()
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

        [Benchmark(OperationsPerInvoke = InnerOps, Description = "void")]
        [BenchmarkCategory(nameof(Task))]
        public async Task ViaTask()
        {
            for (int i = 0; i < InnerOps; i++)
                await Inner().ConfigureAwait(false);

            static async Task Inner()
            {
                await Task.Yield();
                await Task.Yield();
            }
        }

        [Benchmark(OperationsPerInvoke = InnerOps, Description = "T")]
        [BenchmarkCategory(nameof(ValueTask))]
        public async ValueTask<int> ViaValueTaskT()
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

        [Benchmark(OperationsPerInvoke = InnerOps, Description = "void")]
        [BenchmarkCategory(nameof(ValueTask))]
        public async ValueTask ViaValueTask()
        {
            for (int i = 0; i < InnerOps; i++)
                await Inner().ConfigureAwait(false);
            static async ValueTask Inner()
            {
                await Task.Yield();
                await Task.Yield();
            }
        }

        // this should work, but it doesn't, because https://github.com/dotnet/BenchmarkDotNet/issues/1193
        // [Benchmark(OperationsPerInvoke = InnerOps, Description = nameof(TaskLike<int>))]
        public ValueTask<int> ViaPooledValueTaskT()
        {
            return Impl(); // thunks the type back to ValueTaskT

            async PooledValueTask<int> Impl()
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

        // workaround is to add an "await"
        [Benchmark(OperationsPerInvoke = InnerOps, Description = "T")]
        [BenchmarkCategory(nameof(PooledValueTask))]
        public async ValueTask<int> ViaPooledValueTaskTAwaited() => await ViaPooledValueTaskT();

        // this should work, but it doesn't, because https://github.com/dotnet/BenchmarkDotNet/issues/1193
        // [Benchmark(OperationsPerInvoke = InnerOps, Description = nameof(TaskLike))]
        public ValueTask ViaPooledValueTask()
        {
            return Impl(); // thunks the type back to ValueTaskT

            async PooledValueTask Impl()
            {
                for (int i = 0; i < InnerOps; i++)
                    await Inner().ConfigureAwait(false);
                static async PooledValueTask Inner()
                {
                    await Task.Yield();
                    await Task.Yield();
                }
            }
        }

        // workaround is to add an "await"
        [Benchmark(OperationsPerInvoke = InnerOps, Description = "void")]
        [BenchmarkCategory(nameof(PooledValueTask))]
        public async ValueTask ViaPooledValueTaskAwaited() => await ViaPooledValueTask();
    }
}
