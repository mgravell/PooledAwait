using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using PooledAwait;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Benchmark
{
    [MemoryDiagnoser, ShortRunJob]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    public class ComparisonBenchmarks
    {
        // note: all the benchmarks use Task/Task<T> for the public API, because BenchmarkDotNet
        // doesn't work reliably with more exotic task-types (even just ValueTask fails); instead,
        // we'll obscure the cost of the outer awaitable by doing a relatively large number of
        // iterations, so that we're only really measuring the inner loop
        private const int InnerOps = 1000;

        // [Params(false, true)]
        public bool ConfigureAwait { get; set; } = false;

        [Benchmark(OperationsPerInvoke = InnerOps, Description = ".NET/T")]
        [BenchmarkCategory(nameof(Task))]
        public async Task<int> ViaTaskT()
        {
            int sum = 0;
            for (int i = 0; i < InnerOps; i++)
                sum += await Inner(1, 2).ConfigureAwait(ConfigureAwait);
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

        [Benchmark(OperationsPerInvoke = InnerOps, Description = ".NET")]
        [BenchmarkCategory(nameof(Task))]
        public async Task ViaTask()
        {
            for (int i = 0; i < InnerOps; i++)
                await Inner().ConfigureAwait(ConfigureAwait);

            static async Task Inner()
            {
                await Task.Yield();
                await Task.Yield();
            }
        }

        [Benchmark(OperationsPerInvoke = InnerOps, Description = ".NET/T")]
        [BenchmarkCategory(nameof(ValueTask))]
        public async Task<int> ViaValueTaskT()
        {
            int sum = 0;
            for (int i = 0; i < InnerOps; i++)
                sum += await Inner(1, 2).ConfigureAwait(ConfigureAwait);
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

        [Benchmark(OperationsPerInvoke = InnerOps, Description = ".NET")]
        [BenchmarkCategory(nameof(ValueTask))]
        public async Task ViaValueTask()
        {
            for (int i = 0; i < InnerOps; i++)
                await Inner().ConfigureAwait(ConfigureAwait);

            static async ValueTask Inner()
            {
                await Task.Yield();
                await Task.Yield();
            }
        }

        [Benchmark(OperationsPerInvoke = InnerOps, Description = "Pooled/T")]
        [BenchmarkCategory(nameof(ValueTask))]
        public async Task<int> ViaPooledValueTaskT()
        {
            int sum = 0;
            for (int i = 0; i < InnerOps; i++)
                sum += await Inner(1, 2).ConfigureAwait(ConfigureAwait);
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

        [Benchmark(OperationsPerInvoke = InnerOps, Description = "Pooled")]
        [BenchmarkCategory(nameof(ValueTask))]
        public async Task ViaPooledValueTask()
        {
            for (int i = 0; i < InnerOps; i++)
                await Inner().ConfigureAwait(ConfigureAwait);

            static async PooledValueTask Inner()
            {
                await Task.Yield();
                await Task.Yield();
            }
        }

        [Benchmark(OperationsPerInvoke = InnerOps, Description = "Pooled/T")]
        [BenchmarkCategory(nameof(Task))]
        public async Task<int> ViaPooledTaskT()
        {
            int sum = 0;
            for (int i = 0; i < InnerOps; i++)
                sum += await Inner(1, 2).ConfigureAwait(ConfigureAwait);
            return sum;

            static async PooledTask<int> Inner(int x, int y)
            {
                int i = x;
                await Task.Yield();
                i *= y;
                await Task.Yield();
                return 5 * i;
            }
        }

        [Benchmark(OperationsPerInvoke = InnerOps, Description = "Pooled")]
        [BenchmarkCategory(nameof(Task))]
        public async Task ViaPooledTask()
        {
            for (int i = 0; i < InnerOps; i++)
                await Inner().ConfigureAwait(ConfigureAwait);

            static async PooledTask Inner()
            {
                await Task.Yield();
                await Task.Yield();
            }
        }
    }
}
